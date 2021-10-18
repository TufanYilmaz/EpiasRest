using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;

namespace EpiasRest
{
    public class LogManager
    {
        //string LogDocument = @"C:\Users\Tufan\Documents\visual studio 2012\Projects\Epias_Rest\Epias_Rest\Helper.log.txt";
        string LogDocument = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+ "\\OsosLog\\log.txt";
        //string LogDocument = Directory.GetCurrentDirectory() + "\\OsosLog\\log.txt";
        public LogManager()
        {
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\OsosLog"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\OsosLog");
                File.Create(Directory.GetCurrentDirectory() + "\\OsosLog\\log.txt");
            }
            //if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\OsosLog\\" + DateTime.Now.Year + DateTime.Now.ToString("MMMM", CultureInfo.CreateSpecificCulture("tr"))))
            //{
            //    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\OsosLog\\" + DateTime.Now.Year + DateTime.Now.ToString("MMMM", CultureInfo.CreateSpecificCulture("tr")));
            //    File.Create(Directory.GetCurrentDirectory() + "\\OsosLog\\" + DateTime.Now.Year + DateTime.Now.ToString("MMMM", CultureInfo.CreateSpecificCulture("tr")) + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt");
            //}
            //else
            //{
            //    if (!File.Exists(Directory.GetCurrentDirectory() + "\\OsosLog\\" + DateTime.Now.Year + DateTime.Now.ToString("MMMM", CultureInfo.CreateSpecificCulture("tr")) + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt"))
            //    {
            //        File.Create(Directory.GetCurrentDirectory() + "\\OsosLog\\" + DateTime.Now.Year + DateTime.Now.ToString("MMMM", CultureInfo.CreateSpecificCulture("tr")) + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt");
            //    }
            //}
            //LogDocument = Directory.GetCurrentDirectory() + "\\OsosLog\\" + DateTime.Now.Year + DateTime.Now.ToString("MMMM", CultureInfo.CreateSpecificCulture("tr")) + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt";
        }
        public void WriteLogLine(string log, bool status = true)
        {
            try
            {
                lock (this)
                {
                    using (StreamWriter sw = File.AppendText(LogDocument))
                    {
                        sw.WriteLine("\n" + DateTime.Now.ToString() + " -- " + log);
                    }
                }
                if (status)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "  " + log);
                }
                else
                {
                    
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(DateTime.Now.ToString() + "  ! " + log);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
