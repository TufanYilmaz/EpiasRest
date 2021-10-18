using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpiasRest
{
    public static class MainService
    {
        public static void Start(string[] args)
        {
            Services.LoadProperties();
            Services.LoadNextTimeForPTF();
            Services.LoadNextTimeForOsos(1);
            bool mainRunning = true;
            string cmd = string.Empty;
            Services.StartPTF(Parameters.isPTFRunning);
            Services.StartOSOS(Parameters.isOSOSRunning);
            if (System.Environment.UserInteractive)
                while (mainRunning)
                {
                    try
                    {


                        Console.Write("cmd>");
                        cmd = Console.ReadLine();
                        mainRunning = Services.DoCommand(cmd);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
        }
        public static void Stop()
        {
            Parameters.isPTFRunning = false;
            Parameters.isOSOSRunning = false;
        }
    }
}
