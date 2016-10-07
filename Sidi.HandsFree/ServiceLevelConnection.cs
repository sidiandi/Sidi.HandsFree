// Copyright (c) 2016, Andreas Grimme

using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sprache;
using System.Reflection;

namespace Sidi.HandsFree
{
    public enum CallSetupStatus
    {
        NotCurrentlyInCallSetUp = 0,
        IncomingCallProcessOngoing = 1,
        OutgoingCallSetUpIsOngoing = 2,
        RemotePartyBeingAlertedInAnOutgoingCall = 3,
    };

    public enum CallHeldStatus
    {
        NoCallsHeld = 0,
        CallIsPlacedOnHold = 1,
        CallOnHoldNoActiveCall = 2
    }

    /// <summary>
    /// HFP Service Level Connection
    /// </summary>
    public class ServiceLevelConnection : IDisposable
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly AtCommandConnection at;

        public AtCommandConnection At { get { return at; } } 

        public ServiceLevelConnection(Stream stream)
        {
            this.at = new AtCommandConnection(stream);
            at.Response += At_Response;
        }

        private void At_Response(object sender, string e)
        {
            var response = SupportedIndicatorsParser.AtResponse.TryParse(e);
            if (response.WasSuccessful)
            {
                if (response.Value.Command.Equals("CIEV"))
                {
                    var update = SupportedIndicatorsParser.IndicatorUpdate.Parse(response.Value.Value);
                    var indicator = indicators.First(_ => _.Index == update.Index);
                    indicator.CurrentValue = update.CurrentValue;
                    OnIndicatorUpdate(indicator);
                }
            }
        }

        event EventHandler<Indicator> IndicatorUpdate;

        void OnIndicatorUpdate(Indicator i)
        {
            log.DebugFormat("Indicator update: {0}", i);
            if (IndicatorUpdate != null)
            {
                IndicatorUpdate(this, i);
            }
        }

        /// <summary>
        /// Tries to establish a service level connection to the registered Bluetooth device deviceName.
        /// </summary>
        /// <param name="deviceName">If null, connect to the first registered device that supports HFP</param>
        /// <returns></returns>
        public async static Task<ServiceLevelConnection> Connect(string deviceName)
        {
            var client = new BluetoothClient();
            var devices = client.DiscoverDevices(10, true, true, false);
            var slc = devices
                .Where(x => deviceName == null || string.Equals(x.DeviceName, deviceName))
                .Select(device =>
                {
                    try
                    {
                        var ep = new BluetoothEndPoint(device.DeviceAddress, BluetoothService.Handsfree);
                        var cli = new BluetoothClient();
                        cli.Connect(ep);
                        var s = cli.GetStream();
                        return new ServiceLevelConnection(s);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .First(_ => _ != null);

            await slc.Establish();
            return slc;
        }

        List<Indicator> indicators;

        // Indicators explicitly specified in HFP
        Indicator serviceIndicator = null;
        Indicator callIndicator = null;
        Indicator callsetupIndicator = null;
        Indicator callheldIndicator = null;
        Indicator signalIndicator = null;
        Indicator battchgIndicator = null;

        public async Task Establish()
        {
            await at.Command("+BRSF=0");

            indicators = SupportedIndicatorsParser.Parse(await at.Get("+CIND=?")).ToList();
            foreach (var i in indicators)
            {
                var p = GetType().GetField(i.Name + "Indicator", BindingFlags.Instance | BindingFlags.NonPublic);
                if (p == null)
                {
                    log.DebugFormat("Unknown indicator: {0}", i);
                }
                else
                {
                    p.SetValue(this, i);
                }
            }

            await GetIndicatorValues();
            await at.Command("+CMER=3,0,0,1");
            //await at.Command("+CLIP");
        }

        public void Release()
        {
            At.Close();
        }

        public async Task GetIndicatorValues()
        {
            var currentIndicatorValues = SupportedIndicatorsParser.IndicatorValues.Parse(await at.Get("+CIND?"));
            foreach (var i in indicators.Zip(currentIndicatorValues, (i, v) => { i.CurrentValue = v; return i; }))
            {
                OnIndicatorUpdate(i);
            }
        }

        public Task<string> GetManufacturerIdentification()
        {
            return at.Get("+CGMI");
        }

        public Task<string> GetModelIdentification()
        {
            return At.Get("+CGMM");
        }

        public Task<string> GetSerialNumberIdentification()
        {
            return At.Get("+CGSN");
        }

        public Task AnswerCall()
        {
            return at.Command("A");
        }

        public Task<string> GetSubscriberNumber()
        {
            return at.Get("+CNUM");
        }

        /// <summary>
        /// Terminate the currently active call
        /// </summary>
        /// Execution command causes the AG to terminate the currently active call.This command shall have no impact on the state of a held call except in 
        /// the use of rejecting a call placed on hold by the Respond and Hold feature
        /// <returns></returns>
        public Task CallHangUp()
        {
            return at.Command("+CHUP");
        }

        public async Task<string> GetNetworkOperator()
        {
            await at.Command("+COPS=3,0");
            return SupportedIndicatorsParser.CommaSeparatedStrings.Parse(await at.Get("+COPS?"))[2];
        }
        
        public async Task Dial(string number)
        {
            await at.Command("D" + number + ";");
        }

        /// <summary>
        /// Service availability indication
        /// </summary>
        /// <value>=false implies no service.No Home/Roam network available.
        /// <value>=true implies presence of service.Home/Roam network available.
        public bool IsService
        {
            get
            {
                return serviceIndicator.CurrentValue == 1;
            }
        }

        /// <summary>
        /// Standard call status indicator
        /// </summary>
        public bool IsCall
        {
            get
            {
                return callIndicator.CurrentValue == 1;
            }
        }

        /// <summary>
        /// Bluetooth proprietary call set up status indicator
        /// </summary>
        public CallSetupStatus CallSetup 
        {
            get
            {
                return (CallSetupStatus)callsetupIndicator.CurrentValue;
            }
        }

        /// <summary>
        /// Bluetooth proprietary call hold status indicator.
        /// </summary>
        public CallHeldStatus CallHeld
        {
            get
            {
                return (CallHeldStatus) callheldIndicator.CurrentValue;
            }
        }

        /// <summary>
        /// Signal strength, 0-5
        /// </summary>
        public int Signal
        {
            get
            {
                return signalIndicator.CurrentValue;
            }
        }

        /// <summary>
        /// Battery level, 0-5
        /// </summary>
        public int Battery
        {
            get
            {
                return battchgIndicator.CurrentValue;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.At.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ServiceLevelConnection() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
