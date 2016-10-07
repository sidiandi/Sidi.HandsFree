// Copyright (c) 2016, Andreas Grimme

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand.Net.Sockets;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;

namespace Sidi.HandsFree
{
    public class SimpleDialer
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SimpleDialer()
        {
        }
        public async Task Dial(string number, string deviceName = null)
        {
            var slc = await ServiceLevelConnection.Connect(deviceName);
            await slc.Establish();
            await slc.Dial(number);
        }
    }
}
