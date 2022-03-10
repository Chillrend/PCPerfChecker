using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PCPerfChecker
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            SendPulseService service = new SendPulseService();
            if (args.Contains("-c", StringComparer.InvariantCultureIgnoreCase) || args.Contains("-console", StringComparer.InvariantCultureIgnoreCase))
            {
                service.StartService(args);
                Console.ReadLine();
                service.StopService();
                return;
            }
            ServiceBase[] ServicesToRun = new ServiceBase[] { 
                service
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
