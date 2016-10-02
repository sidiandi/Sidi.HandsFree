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

namespace Sidi.HandsFree
{
    /// <summary>
    /// HFP Service Level Connection
    /// </summary>
    class ServiceLevelConnection
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly AtCommandConnection at;

        public AtCommandConnection At { get { return at; } } 

        public ServiceLevelConnection(Stream stream)
        {
            this.at = new AtCommandConnection(stream);
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
            return devices
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
        }

        public async Task Establish()
        {
            await at.Command("+BRSF=0");
            await at.Command("+CIND=?");
            await at.Command("+CIND?");
        }

        public Task<string> GetManufacturerIdentification()
        {
            return at.Get("+CGMI");
        }

        public async Task Dial(string number)
        {
            await at.Command("D" + number + ";");
        }
    }
}
