using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft;
using System.Data;
using System.Data.SqlClient;
using Telemeter.Extensions;
using System.Diagnostics;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace EpiasRest
{
    static class Program 
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        static void SetRecoveryOptions(string serviceName)
        {
            try
            {
                int exitCode;
                using (var process = new Process())
                {
                    var startInfo = process.StartInfo;
                    startInfo.FileName = "sc";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    startInfo.Arguments = string.Format("failure \"{0}\" reset= 0 actions= restart/30000", serviceName);

                    process.Start();
                    process.WaitForExit();

                    exitCode = process.ExitCode;
                }

                if (exitCode != 0)
                    throw new InvalidOperationException();
            }
            catch (Exception)
            {
                Console.WriteLine("Hata oluştuğunda servisi yeniden başlat ayarı yapılamadı, Hizmetler bölümünden elle yapabilirsiniz.");
            }
        }
        static void Main(string[] args)
        {
            try
            {
                if (System.Environment.UserInteractive)
                {
                    if (args.Length > 0)
                    {
                        string p = args[0];
                        if (("-/").Contains(p[0]))
                        {
                            p = p.Remove(0, 1);
                            switch (p.ToUpper())
                            {
                                case "İ":
                                case "I":
                                case "İNSTALL":
                                case "INSTALL":
                                    {
                                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                                        SetRecoveryOptions(EpiasService.serviceName);
                                        break;
                                    }
                                case "U":
                                case "UNİNSTALL":
                                case "UNINSTALL":
                                    {
                                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                                        break;
                                    }
                                case "RUN": MainService.Start(args); break;
                            }
                        }
                    }
                }
                else
                {
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                        new EpiasService()
                    };
                    ServiceBase.Run(ServicesToRun);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
