using Sidi.HandsFree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hfdial
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static int Main(string[] args)
        {
            log4net.Config.BasicConfigurator.Configure();
            try
            {
                var d = new SimpleDialer();
                d.Dial(args[0], args[1]).Wait();
                Thread.Sleep(TimeSpan.FromSeconds(30));
                return 0;
            }
            catch (Exception e)
            {
                log.Error(e);
                return -1;
            }
        }
    }
}
