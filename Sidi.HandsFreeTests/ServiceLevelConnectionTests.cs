using NUnit.Framework;
using Sidi.HandsFree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidi.HandsFree.Tests
{
    [TestFixture()]
    public class ServiceLevelConnectionTests
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [TestFixtureSetUp]
        public void Setup()
        {
            log4net.Config.BasicConfigurator.Configure();
        }

        [Test()]
        public void GetDeviceInformation()
        {
            GetDeviceInformationAsync().Wait();
        }

        async Task<ServiceLevelConnection> GetTestServiceLevelConnection()
        {
            var slc = await ServiceLevelConnection.Connect("flamingo");
            return slc;
        }

        async Task GetDeviceInformationAsync()
        {
            using (var slc = await GetTestServiceLevelConnection())
            {
                Console.WriteLine(await slc.GetManufacturerIdentification());
                Console.WriteLine(await slc.GetModelIdentification());
                Console.WriteLine(await slc.GetSerialNumberIdentification());
                Console.WriteLine(await slc.GetNetworkOperator());
                Console.WriteLine(await slc.GetSubscriberNumber());
                Console.WriteLine(slc.IsService);
                Console.WriteLine(slc.IsCall);
                Console.WriteLine(slc.CallSetup);
                Console.WriteLine(slc.CallHeld);
                Console.WriteLine(slc.Signal);
                Console.WriteLine(slc.Battery);

                await slc.Dial("09131847814");

                await slc.CallHangUp();
            }
        }
    }
}