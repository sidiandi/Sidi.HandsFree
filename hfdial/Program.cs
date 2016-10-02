using Sidi.HandsFree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hfdial
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.BasicConfigurator.Configure();
            var d = new SimpleDialer();
            d.Dial(args[0], args[1]).Wait();
        }
    }
}
