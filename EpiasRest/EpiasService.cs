using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace EpiasRest
{
    partial class EpiasService : ServiceBase
    {
        public static string serviceName = "Abysis.EpiasService";
        public EpiasService()
        {
            //InitializeComponent();
            ServiceName = serviceName;
        }
        protected override void OnStart(string[] args)
        {
            MainService.Start(args);
        }

        protected override void OnStop()
        {
            MainService.Stop();
        } 
    }
}
