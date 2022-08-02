using System.ServiceProcess;

namespace Shutdowner
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ShutdownerServce()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
