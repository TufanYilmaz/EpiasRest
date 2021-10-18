//Parameters
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Telemeter.Extensions;

namespace EpiasRest
{
    [Serializable]
    public static class Parameters
    {
        private static bool isptfrunning;
        private static bool isososrunning;
        public  static string OSB { get; set; }
        public static int ososDataQuantity { get; set; }
        public static bool isServiceRunning = false;
        public static string MailRecipents { get; set; }
        public static string MailRecipentsDataSet { get; set; }
        public static bool isMailRunning { get; set; }
        public static string AdminMails { get; set; }
        public static string ErrorMailRecivers { get; set; }
        public static string LastSentPTFTime { get; set; }
        public static bool isSmsRunning { get; set; }
        public static string smsServiceURL { get; set; }
        public static string ServiceUser { get; set; }
        public static string ServicePassword { get; set; }
        public static string YekdemPriceCode { get; set; }

        public static string RegisterDbHost { get; set; }
        public static int RegisterDbPort { get; set; }

        public static bool isPTFRunning
        {
            get
            {
                return isptfrunning;
            }
            set
            {
                isptfrunning = value; 
                Services.StartPTF(value && isServiceRunning);
            }
        }
        public static bool isOSOSRunning { get { return isososrunning; } set { isososrunning = value; Services.StartOSOS(value&&isServiceRunning); } }
        

        private static string ParametersfileName = "parameters.db";

        public static string DefaultChannel = "Default";
        static Parameters()
        {
            YekdemPriceCode = "YKDMO";
            smsServiceURL = "http://10.10.10.155:90";
            ErrorMailRecivers = "";
            isSmsRunning = false;
            isMailRunning = false;
            ServiceUser = "";
            ServicePassword = "";
            LastSentPTFTime = DateTime.Today.AddDays(-1).ToString();
            AdminMails = "";
            MailRecipentsDataSet = "";
            MailRecipents = "";
            isPTFRunning = false;
            isOSOSRunning = false;
            ososDataQuantity = 20;
            OSB = "OSB";
            RegisterDbHost = "127.0.0.1";
            RegisterDbPort = 11100;
            Load(ParametersfileName);
        }

       internal static string GetPropertyValue(string param)
        {
            string result = param;
            try
            {
                var p = Array.Find(ParamList, x => x.Name.ToUpper() == param.ToUpper());
                if (p != null)
                    result = p.GetValue(null).ToString();
            }
            catch
            {
                // Logger.Writeln(0, tmp[0] + " property not found in Parameters");
            }
            return result;
        }

        internal static string GetPropertyName(string param)
        {
            string result = param;
            try
            {
                var p = Array.Find(ParamList, x => x.Name.ToUpper() == param.ToUpper());
                if (p != null)
                    result = p.Name;
            }
            catch
            {
                // Logger.Writeln(0, tmp[0] + " property not found in Parameters");
            }
            return result;
        }
        internal static bool SetPropertyValue(string param, string value)
        {
            bool result = false;
            try
            {
                var p = Array.Find(ParamList, x => x.Name.ToUpper() == param.ToUpper());
                if (p != null)
                {
                    var dtype = p.GetValue(null, null).GetType();
                    var typeConverter = TypeDescriptor.GetConverter(dtype);
                    var propValue = typeConverter.ConvertFromString(value);
                    p.SetValue(null, propValue);
                    result = true;
                }
            }
            catch
            {
                // Logger.Writeln(0, tmp[0] + " property not found in Parameters");
            }
            return result;
        }

        public static void Create()
        {
            // Bu method parametrelerin yüklenmesini sağlar. Çağrıldığında nesne oluşturulsun diye boş olarak kullanılmıştır. 
        }
        public static PropertyInfo[] ParamList = typeof(Parameters).GetProperties();

        
        private static void Load(string filename)
        {
            filename = ArrayHelper.MainFolder + "\\" + filename;
            bool paramExist = System.IO.File.Exists(filename);
            if (paramExist)
            {
                string[] slist = System.IO.File.ReadAllText(filename).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
               
                paramExist = (slist.Count() > 0);
                if (paramExist)
                    foreach (string s in slist)
                    {
                        var tmp = s.Trim().Split(new string[] { "=" },2, StringSplitOptions.RemoveEmptyEntries);
                        if (tmp.Count() > 1) 
                            Parameters.SetPropertyValue(tmp[0].Trim(), tmp[1].Trim()); 
                    }
            }
            if (!paramExist)
                Save();
        }

        public static void Save()
        {
            Save(ParametersfileName);
            Console.WriteLine("Parameters saved to file...");
        }

        private static void Save(string filename)
        {
            filename = ArrayHelper.MainFolder + "\\" + filename;
            string Data = "";
            Type type = typeof(Parameters);
            foreach (var p in type.GetProperties())
            {
                if (p.Name.Substring(0, 2) != "do")
                {
                    var v = p.GetValue(null, null);
                    string s = p.Name + "=" + v.ToString();
                    Data += s + "\r\n";
                }
            } 
            System.IO.File.WriteAllText(filename, Data); 
        }

       
    }
}
