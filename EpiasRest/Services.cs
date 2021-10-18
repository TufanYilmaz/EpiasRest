using EpiasRest.Auth;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TcpModels;
using Telemeter.Extensions;

namespace EpiasRest
{
    public static class Services
    {
        static EpiasRestClient restClient;
        //static EpiasRestClient restClientForCheck;
        static EpiasApiPortalClient apiPortalClient;
        static string pmumUser;
        static string pmumPassword;
        static string pmumURL;
        static string PTFtime;
        static int SMS_ID;
        //Helper.log.WriteLogLine(" DEBUG

        static DateTime nextTimeForOSOS;
        static DateTime nextTimeForOsosConfig;
        static object ososLock = new object();
        public static void OsosThread()
        {
            #region DebugAndDemoOnly
           
            #endregion
            restClient = new EpiasRestClient(pmumUser, pmumPassword, pmumURL);//General
            while (Parameters.isOSOSRunning)
            {
                try
                {
                    lock (ososLock)
                    {
                        restClient.SendEpiasData(RegisterModels.SubscriptionCallType.Instant, RegisterModels.SentState.NotSend);
                        if (DateTime.Now > nextTimeForOsosConfig)
                        {
                            restClient.OsosConfig();
                            LoadNextTimeForOsosConfig(1);
                            //GC.Collect();
                        }
                        if (DateTime.Now > nextTimeForOSOS)
                        {
                            Helper.log.WriteLogLine("Günsonu Verileri Gönderiliyor");
                            restClient.SendEpiasData(RegisterModels.SubscriptionCallType.DayEnd, RegisterModels.SentState.NotSend);
                            LoadNextTimeForOsos(1);

                            #region mail
                            //OSOS Mailer
                            if (!string.IsNullOrEmpty(Parameters.AdminMails) && Parameters.isMailRunning)
                            {
                                Helper.log.WriteLogLine("Osos Rapor Mailer Girildi");
                                Thread.Sleep(1000);
                                try
                                {
                                    var yesterday = DateTime.Now.AddDays(-1);
                                    string info = Parameters.OSB + "<br>" + new DateTime(yesterday.Year, yesterday.Month, yesterday.Day).ToString() +
                                                                               " Gününe ait OSOS verileri aşağıdadır ve EPİAŞ sistemine gönderilmiştir. <br><br>";
                                    info += restClient.DataManager.MailBodyData().ConvertDataTableToHTML("ABONE ADI");
                                    //DataTable xmlInfo = restClient.DataManager.MailBodyData();
                                    Mailer.Instance.Send(
                                        Parameters.AdminMails,
                                        "Osos Günlük Raporu",
                                        info,
                                         restClient.DataManager.MailBodyData()
                                         ,BCc:"tufan@merkez.com.tr");
                                    Helper.log.WriteLogLine("Osos Rapor Mailer Çıkıldı");
                                }
                                catch (Exception ex)
                                {
                                    Helper.log.WriteLogLine(ex.Message);
                                }
                            }
                            //OSOS Mailer End
                            #endregion
                        }
                    }
                    Thread.Sleep(60000);
                }
                catch (Exception ex)
                {
                    try
                    {
                        Mailer.Instance.Send("tufan@merkez.com.tr",Parameters.OSB+ " Epiaş Main Thread Hatası", ex.ToString());
                    }
                    catch (Exception ext)
                    {
                        Helper.log.WriteLogLine("! Main Epias Thread ERROR Mailer Error" + ext.Message, false);
                    }
                    Helper.log.WriteLogLine("! Main Epias Thread ERROR" + ex.Message, false);
                    //Parameters.isOSOSRunning = false;
                    Helper.log.WriteLogLine("Parametre False yapılmadı test amaçlı", false);
                }
            }
        }
        static DateTime nextTimeForPTF;
        static object ptfLock = new object();
        private static void loggerWriteLine(object Sender, object Line)
        {
            Helper.log.WriteLogLine(Line.ToString());
        }
        public static void PTFThread()
        {
            try
            {
                apiPortalClient = new EpiasApiPortalClient();
                while (Parameters.isPTFRunning)
                {
                    if (DateTime.Now > nextTimeForPTF)
                    {
                        //Console.WriteLine(nextTimeForPTF);
                        lock (ptfLock)
                        {
                            //apiPortalClient.GetAndSaveDayAheadMCP(DateTime.Now, DateTime.Now);
                            if (apiPortalClient.GetAndSaveDayAheadMCP(DateTime.Now.AddDays(1), DateTime.Now.AddDays(1)))
                            {
                                LoadNextTimeForPTF(1);
                               
                                if (Convert.ToDateTime(Parameters.LastSentPTFTime) < nextTimeForPTF)
                                {
                                    if (Parameters.isMailRunning)
                                        SendPTFMailRecipents();
                                    if (Parameters.isSmsRunning)
                                        SendSMSRecipents();
                                    Parameters.LastSentPTFTime = nextTimeForPTF.ToString();
                                    Parameters.Save();
                                }
                            }
                            else
                            {
                                nextTimeForPTF = DateTime.Now.AddMinutes(1);
                            }
                            apiPortalClient.SavePTFMonth(DateTime.Now.Month, true);
                        }
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (Exception ex)
            {
                Helper.log.WriteLogLine(ex.Message.ToString(),false);
                Console.WriteLine(ex.ToString());
            }
        }

        private static decimal PTFAverage()
        {
            //decimal avg = apiPortalClient.DataManager.GetDayAheadMCP(nextTimeForPTF).Select()
            //    .Where(p => p[2] != DBNull.Value)
            //    .Select(c => Convert.ToDecimal(c[2])).Average();
            return Math.Round(Convert.ToDecimal(apiPortalClient.DataManager.GetDayAheadMCP(nextTimeForPTF).Compute("AVG([FİYAT])", "")), 2);
        }
        #region SMS and Mail area
        private static bool SendSMSRecipents()
        {
            bool result = false;
            string info = "PTF " + nextTimeForPTF.ToString("dd-MMM-yyyy") + " (TL/MWh)\r\n";
            decimal avg = PTFAverage();
            info += "Art.Ort: " + avg + "\r\n";
            DataTable TomorrowsPTF = apiPortalClient.DataManager.GetDayAheadMCP(nextTimeForPTF);
            foreach (DataRow item in TomorrowsPTF.Rows)
            {
                info += Convert.ToDateTime(item[1]).ToString("HH") + "-" + Convert.ToDateTime(item[1]).AddHours(1).ToString("HH") + " " + Convert.ToDecimal(item[2]).ToString("n2") + "\r\n";
            }
            DataTable ykdm = GetYekdemPrice();
            if (ykdm.Rows.Count > 0)
            {
                info += Convert.ToDateTime(ykdm.Rows[0]["BEGINDATE"]).ToString("MMMM") + "-" + ykdm.Rows[0]["INFO"].ToString() + " = " + (Convert.ToDecimal(ykdm.Rows[0]["PRICE"]) * 1000).ToString("n2");
            }
            #region sending Sms

            //recipents += ";" + GetSubscriberPhones();
            var client = new AuthenticationSoapClient("AuthenticationSoap", Parameters.smsServiceURL + "/services/authentication.asmx");
            var resultLogin = client.Login(Parameters.ServiceUser, Parameters.ServicePassword, "ELEKTRİK");
            if (resultLogin.Result.Id == 0)
            {
                //var sms = new Messager.MessagerSoapClient("MessagerSoap", Parameters.smsServiceURL + "/services/messager.asmx");
                EndpointAddress smsEndpoint = new System.ServiceModel.EndpointAddress(Parameters.smsServiceURL + "/services/messager.asmx");
                BasicHttpBinding binding = MakeBinding("MessagerSoap", 30, 30, 64000000, 64000000, 64000000);
                var sms = new Messager.MessagerSoapClient(binding, smsEndpoint);
                try
                {
                    Console.WriteLine("Mesajlar Gönderiliyor...");
                    var smsRes = sms.SendSmsById(resultLogin.Value.ToString(), SMS_ID, info);
                    if (smsRes.Value != null)
                    {
                        var failed = smsRes.Value.ToList().FindAll(p => Convert.ToInt32(p.Status) > 1);
                        if (failed.Count > 0)
                        {
                            DataTable errors = failed.ToDataTable();
                            SendErrorMail(errors);
                        }

                    }
                    else
                        Console.WriteLine("Sms Gönderme işlemi başarısız oldu");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                Console.WriteLine("Sms Gönderme işlemi tamamlandı.");
            }

            #endregion

            return result;
        }
        public static System.ServiceModel.BasicHttpBinding MakeBinding(string bindingName, int sentTimeOut, int reciveTimeOut, long maxReceivedMessageSize, int maxBufferSize, long maxBufferPoolSize)
        {
            System.ServiceModel.BasicHttpBinding binding = new System.ServiceModel.BasicHttpBinding();
            TimeSpan SentTimeOut = new TimeSpan(0, 0, 0);
            TimeSpan ReciveTimeOut = new TimeSpan(0, 0, 0);
            binding.SendTimeout = SentTimeOut.Add(new TimeSpan(0, sentTimeOut, 0));
            binding.ReceiveTimeout = ReciveTimeOut.Add(new TimeSpan(0, sentTimeOut, 0));
            binding.MaxReceivedMessageSize = maxReceivedMessageSize;
            binding.MaxBufferPoolSize = maxBufferPoolSize;
            binding.MaxBufferSize = maxBufferSize;
            binding.Name = bindingName;

            return binding;
        }
        private static bool ValidateNumber(string item)
        {
            bool result = false;
            Regex validator = new Regex(@"^[0-9]+$");
            result = validator.Match(item).Success;
            if (result)
            {
                if (item.StartsWith("0"))
                {
                    item = item.Substring(1);
                }
                result = item.Length == 10;
            }
            return result;
        }
        private static bool IsBase64(string input)
        {
            Regex validator = new Regex("^([A-Za-z0-9+/]{4})*([A-Za-z0-9+/]{4}|[A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{2}==)$");
            return validator.Match(input).Success;
        }

        public static DataTable GetYekdemPrice()
        {
            DataTable result = new DataTable();
            SqlConnection Conn;
            if (dbConn.AbysisDb.Load())
            {
                Conn = new SqlConnection(dbConn.AbysisDb.ToString());
                try
                {
                    Conn.Open();
                    SqlCommand cmd = new SqlCommand(@"SELECT P.ID, PD.CODE, PD.INFO, P.BEGINDATE, P.PRICE FROM tb_PRICE_DEFINES PD
                                                     LEFT OUTER JOIN tb_PRICES P ON PD.ID=P.PRICE_DEFINE_ID
                                                     WHERE MONTH(P.BEGINDATE)=MONTH(@date) AND YEAR(P.BEGINDATE)=YEAR(@date) AND PD.CODE=@code", Conn);

                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@date", nextTimeForPTF);
                    cmd.Parameters.AddWithValue("@code", Parameters.YekdemPriceCode);
                    SqlDataAdapter adap = new SqlDataAdapter(cmd);
                    adap.Fill(result);
                    Conn.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            return result;
        }

        private static void SendPTFMailRecipents()
        {
            if (!string.IsNullOrEmpty(Parameters.MailRecipents))
            {
                string info = nextTimeForPTF.Day + " " + nextTimeForPTF.ToString("MMMM", CultureInfo.CreateSpecificCulture("tr")) + " " + nextTimeForPTF.Year +
                                        " Gününe Ait Epiaş'ın yayınladığı Piyasa Takas Fiyatları (PTF)<br>";
                info += apiPortalClient.DataManager.GetDayAheadMCP(nextTimeForPTF).ConvertDataTableToHTML("DEPARTMAN");
                info += "Aritmetik Ortalama = " + PTFAverage() + "<br>";
                string recipents = Parameters.MailRecipents;
                if (recipents != "")
                {
                    string Bcc = Parameters.AdminMails;
                    if (Parameters.MailRecipentsDataSet != "")
                    {
                        //Console.WriteLine("Add Bcc Start");
                        Bcc += ";" + GetSubscriberMails();
                        //Console.WriteLine("Add Bcc Fin");
                    }
                    Console.WriteLine(DateTime.Now + " Mail Gönderiliyor.");
                    int tryMail = -1;
                    while (++tryMail < 3)
                    {
                        if (Mailer.Instance.Send(recipents, "Yarına Ait PTF Fiyatları", info,BCc: Bcc))
                            break;
                    }
                }
            }
        }

        private static void SendErrorMail(DataTable errors)
        {
            errors.Columns.Remove("Content");
            if (!string.IsNullOrEmpty(Parameters.ErrorMailRecivers))
            {
                string info = @"<h3>Telefon Numarası Hatası</h3><br>
                               Aşağıdaki Numaralara veri gönderilirken bir hata oluştu.<br>
                               (Lütfen numaraların eksik,fazla veya hatalı olmamalarına özen gösteriniz)<br>";
                info += errors.ConvertDataTableToHTML(errors.Columns[0].ColumnName);
                string sender = Parameters.MailRecipents;
                if (sender != "")
                {
                    string Bcc = Parameters.ErrorMailRecivers;
                    Mailer.Instance.Send(sender, "Epiaş Servis Hata Mesajı", info,BCc: Bcc);
                }

            }
            foreach (DataRow r in errors.Rows)
            {
                Helper.log.WriteLogLine(r[1] + " | " + r[2] + " | " + r[3] + " | " + r[4], false);
            }
            Helper.log.WriteLogLine(errors.Rows.Count + " Numaraya Mesaj Gönderilemedi!", false);
        }

        private static string GetSubscriberMails()
        {
            string result = "";
            //Console.WriteLine("Start get Sub Mails");
            SqlConnection Conn;

            if (dbConn.AbysisDb.Load())
            {
                Conn = new SqlConnection(dbConn.AbysisDb.ToString());
                try
                {
                    Conn.Open();
                    SqlCommand cmd = new SqlCommand(Parameters.MailRecipentsDataSet, Conn);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        //Console.WriteLine("Sub mail"+r[0].ToString());
                        result += r[0].ToString() + ';';
                    }
                    r.Close();
                    r.Dispose();
                    Conn.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return result;
        }

        private static object SetMailParam(string value, int isEncode)
        {
            object result;
            if (isEncode != 0)
            {
                result = value.fromBase64();
            }
            else
                result = value;

            return result;

        }

        //Interval, DelayForNext,RecepientType;
        private static void LoadSMSProperties()
        {
            DataTable res = new DataTable();
            SqlCommand cmd = new SqlCommand(@"SELECT ID FROM tb_SMS WHERE DEPARTMENT_ID=1 AND CODE='PTF' AND ACTIVE=1");
            SqlConnection Conn;
            if (dbConn.AbysisDb.Load())
            {
                Conn = new SqlConnection(dbConn.AbysisDb.ToString());
                try
                {
                    if (Conn.State != ConnectionState.Open)
                        Conn.Open();
                    cmd.Connection = Conn;
                    SqlDataAdapter adap = new SqlDataAdapter(cmd);
                    adap.Fill(res);
                    Conn.Close();
                    Conn.Dispose();
                    if (res.Rows.Count > 0)
                    {
                        DataRow r = res.Rows[0];
                        SMS_ID = Convert.ToInt32(r["ID"]);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        #endregion

        public static void LoadProperties()
        {
            LoadSMSProperties();
            SqlConnection Conn;
            LoadNextTimeForOsosConfig(0, true);
            //nextTimeForOsosConfig 
            if (dbConn.AbysisDb.Load())
            {
                Conn = new SqlConnection(dbConn.AbysisDb.ToString());

                try
                {
                    if (Conn.State != ConnectionState.Open)
                        Conn.Open();

                    string[] props = new string[] { "PMUM_OsosService", "PMUM_Password", "PMUM_UserName", "PMUM_PTF_QUERYTIME" };
                    string sql = string.Format(@"SELECT PROPERTYNAME,PROPERTYVALUE,ISENCODE FROM def_PROPERTIES WHERE DEPARTMENT_ID=1 AND USERID IN (0,1) AND PROPERTYNAME IN({0})", "\'" + string.Join("','", props) + "\'");
                    DataTable result = new DataTable();
                    SqlCommand cmd = new SqlCommand(sql);
                    try
                    {
                        cmd.Connection = Conn;
                        SqlDataAdapter adap = new SqlDataAdapter(cmd);
                        adap.Fill(result);
                        foreach (DataRow R in result.Rows)
                        {
                            switch (R["PROPERTYNAME"].ToString())
                            {
                                case "PMUM_OsosService":
                                    pmumURL = R["PROPERTYVALUE"].ToString();
                                    break;
                                case "PMUM_Password":
                                    pmumPassword = R["PROPERTYVALUE"].ToString();
                                    if (IsBase64(pmumPassword))
                                        pmumPassword = Encoding.UTF8.GetString(Convert.FromBase64String(pmumPassword));
                                    break;
                                case "PMUM_UserName":
                                    pmumUser = R["PROPERTYVALUE"].ToString();
                                    if (IsBase64(pmumUser))
                                        pmumUser = Encoding.UTF8.GetString(Convert.FromBase64String(pmumUser));
                                    break;
                                case "PMUM_PTF_QUERYTIME":
                                    PTFtime = R["PROPERTYVALUE"].ToString();
                                    break;
                                default:
                                    break;
                            }
                        }
                        Parameters.isServiceRunning = true;
                    }
                    catch (Exception ex)
                    {
                        Helper.log.WriteLogLine(ex.ToString(), false);
                    }
                }
                catch (Exception)
                {
                    Helper.log.WriteLogLine("Veri (Tabanı) Erişimi Sağlanamadı(Properties)!", false);
                }


            }
        }



        public static void LoadNextTimeForPTF(int addDay = 0)
        {
            DateTime tmw = DateTime.Now.AddDays(addDay);
            nextTimeForPTF = new DateTime(tmw.Year, tmw.Month, tmw.Day, 14, 0, 0);

            //if (!string.IsNullOrEmpty(PTFtime))
            //{
            //    string[] time = PTFtime.Split(':');
            //    DateTime tmw = DateTime.Now.AddDays(addDay);
            //    try
            //    {
            //        nextTimeForPTF = new DateTime(tmw.Year, tmw.Month, tmw.Day, Convert.ToInt32(time[0]), Convert.ToInt32(time[1]), 0);
            //    }
            //    catch (Exception)
            //    {
            //        nextTimeForPTF = new DateTime(tmw.Year, tmw.Month, tmw.Day, 14, 0, 0);
            //    }

            //    //nextTimeForPTF = StartPTFTime.AddDays(addDay);
            //}

        }
        public static void LoadNextTimeForOsos(int addDay = 0)
        {
            // LoadNextTimeForOsos start");
            DateTime tmw = DateTime.Now.AddDays(addDay);
            nextTimeForOSOS = new DateTime(tmw.Year, tmw.Month, tmw.Day, 1, 50, 0);
            // LoadNextTimeForOsos end");
        }
        public static void LoadNextTimeForOsosConfig(int addHour = 0, bool initiliaze = false)
        {
            // LoadNextTimeForOsosConfig başladı");
            DateTime td = DateTime.Now;
            if (initiliaze)
                nextTimeForOsosConfig = new DateTime(td.Year, td.Month, td.Day, td.Hour, 0, 0);
            else
                nextTimeForOsosConfig = nextTimeForOsosConfig.AddHours(addHour);
            // LoadNextTimeForOsosConfig bitti");
        }

        public static void StartOSOS(bool run)
        {
            if (run)
            {
                Task.Run(() => OsosThread());
            }

        }

        public static void StartPTF(bool run)
        {
            if (run)
            {
                Task.Run(() => PTFThread());
            }
        }

        public static bool DoCommand(string command)
        {
            bool result = true;
            var cmd = StringEx.getWordtoUpper(ref command);
            switch (cmd)
            {

                case "": break;
                case "CLS":
                case "CLEAR": Console.Clear(); break;
                case "DIS":
                case "DISPLAY":
                    {
                        var param = StringEx.getWordtoUpper(ref command);
                        switch (param)
                        {
                            case "CONN": WriteDatabase(command); break;
                            case "MAIL":
                            case "MAİL": WriteMailInfo(); break;
                            case "PROPS": DisplayProperties(); break;
                            default: Console.WriteLine("\t DISPLAY CONN"); break;
                        }
                    }
                    break;
                case "QUİT":
                case "QUIT":
                case "EXİT":
                case "EXIT": result = false; break;
                case "SAVE":
                    {
                        var param = StringEx.getWordtoUpper(ref command);
                        switch (param)
                        {
                            case "PTF":

                                apiPortalClient = new EpiasApiPortalClient();
                                apiPortalClient.GetAndSaveDayAheadMCP(DateTime.Now, DateTime.Now);
                                Thread.Sleep(2000);
                                apiPortalClient.GetAndSaveDayAheadMCP(DateTime.Now.AddDays(1), DateTime.Now.AddDays(1));
                                Thread.Sleep(2000);
                                apiPortalClient.GetAndSaveDayAheadMCP(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(-1));

                                break;
                            case "PTFMONTH":
                                apiPortalClient.SavePTFMonth(DateTime.Now.Month);
                                break;
                            case "PTFMONTH-1":
                                apiPortalClient.SavePTFMonth(DateTime.Now.Month - 1);
                                break;
                            case "EPIASDAYEND":
                                restClient.SendEpiasData(RegisterModels.SubscriptionCallType.DayEnd, RegisterModels.SentState.NotSend);
                                break;
                            case "EPIASINSTANT":
                                restClient.SendEpiasData(RegisterModels.SubscriptionCallType.Instant, RegisterModels.SentState.NotSend);
                                break;
                            case "CONN":
                                {
                                    dbConn.AbysisDb.Save();
                                    Console.WriteLine("SQL Connection parameters saved!");
                                }
                                break;
                            case "PARAMS": Parameters.Save(); break;

                            default: Console.WriteLine("\t SAVE [CONN or PARAMS]"); break;
                        }
                    }
                    break;
                case "LOAD":
                    {
                        var param = StringEx.getWordtoUpper(ref command);
                        switch (param)
                        {
                            case "CONN":
                                {
                                    dbConn.AbysisDb.Load();
                                    Console.WriteLine("SQL Connection parameters loaded.");
                                }
                                break;
                            default: Console.WriteLine("\t LOAD CONN"); break;
                        }
                    }
                    break;
                case "TESTDB":
                    {
                        if (dbConn.AbysisDb.ServerAvailable())
                            Console.WriteLine("SQL connection success!");
                    }
                    break;
                case "PARAMS": WriteParamList(); break;
                case "HELP": WriteHelp(); break;
                case "SET":
                    {
                        var param = StringEx.getWord(ref command);
                        SetValue(param, command);
                    }
                    break;
                case "OSOSCONFIG":
                    restClient.OsosConfig();
                    break;
                case "SENDOSOS":
                    if (restClient == null)
                    {
                        restClient = new EpiasRestClient(pmumUser, pmumPassword, pmumURL);
                    }
                    for (int i = -2; i > -32; i=i-2)
                    {
                        restClient.SendEpiasData(RegisterModels.SubscriptionCallType.DayEnd, RegisterModels.SentState.NotSend,i);
                    }
                    break;
                default: Console.WriteLine("Invalid command"); break;
            }
            return result;
        }

        private static void DisplayProperties()
        {
            Console.WriteLine("NextTime For PTF     " + nextTimeForPTF);
            Console.WriteLine("");
        }

        //private static void SavePTFMonth(int month)
        //{
        //    apiPortalClient = new EpiasApiPortalClient();
        //    DateTime begin = new DateTime(DateTime.Now.Year,month,1, 0, 0, 0);
        //    for (DateTime b = begin; b <= DateTime.Now.AddDays(1); b= b.AddDays(1))
        //    {
        //        if(apiPortalClient.DataManager.CheckMCPbyDate(b))
        //            apiPortalClient.GetAndSaveDayAheadMCP(b, b);
        //        Thread.Sleep(2500);
        //    }
        //    Console.WriteLine(DateTime.Now + " Aylık PTF kaydedildi");
        //}



        private static void WriteParamList()
        {
            foreach (var p in Parameters.ParamList)
            {
                Console.WriteLine(string.Format("\t{0} = {1}", p.Name, p.GetValue(null, null)));
            }
        }
        public static void WriteHelp()
        {
            Console.WriteLine();
            Console.WriteLine("  Set Server [SQLInstance]\t> Set the SQL Server instance name");
            Console.WriteLine("  Set Database [DBName]   \t> Set the SQL Database name");
            Console.WriteLine("  Set User [UserId]       \t> Set the SQL Server User name");
            Console.WriteLine("  Set Pass [Password]     \t> Set the SQL Server Password");
            Console.WriteLine();

            Console.WriteLine("  Set [property] [value]\t> Set the property value for any object");
            Console.WriteLine();
            Console.WriteLine("  Params                \t> List the parameters");
            Console.WriteLine("  List Drivers          \t> List the all driver names");
            Console.WriteLine("  List FrameTypes       \t> List the all frametypes (modbus)");
            Console.WriteLine();
            Console.WriteLine("  TestDb \t> Check the SQL connection");
            Console.WriteLine("  Load   \t> Load any object(s)");
            Console.WriteLine("  Save   \t> Save any object(s)");
            Console.WriteLine("  Display\t> Display the any object information");
            Console.WriteLine();
            Console.WriteLine("  Ping   \t> Ping active device, or ping IP address");

            Console.WriteLine("  Quit   \t> Exit");
            Console.WriteLine();
        }
        private static void WriteDatabase(string command)
        {
            if (dbConn.AbysisDb.String != "")
            {
                Console.WriteLine();
                Console.WriteLine(" DB CONNECTION");
                Console.WriteLine("\tSERVER      \t" + dbConn.AbysisDb.Server);
                Console.WriteLine("\tDATABASE    \t" + dbConn.AbysisDb.Database);
                Console.WriteLine("\tUSER ID     \t" + dbConn.AbysisDb.Userid);
                Console.WriteLine("\tPASSWORD    \t" + new string('*', dbConn.AbysisDb.Password.Length));
            }
            else
                Console.WriteLine("No database definition!");
            Console.WriteLine();
        }
        private static void WriteMailInfo()
        {
            if (dbConn.AbysisDb.String != "")
            {
                Console.WriteLine();
                Console.WriteLine(" Mail Config");
                Console.WriteLine("\tHOST     \t" +Mailer.Instance.Host);
                Console.WriteLine("\tUSER    \t" + Mailer.Instance.UserName);
                Console.WriteLine("\tPORT    \t" + Mailer.Instance.Port);
                Console.WriteLine("\tUSE SSL \t" + Mailer.Instance.UseSSL.ToString());
                Console.WriteLine("\tPASSWORD    \t" + new string('*', Mailer.Instance.Password.Length));
            }
            else
                Console.WriteLine("No database definition!");
            Console.WriteLine();
        }
        public static void SetValue(string param, string value)
        {
            bool setted = true;
            string settedObject = "";
            switch (param.ToUpper())
            {
                case "SERVER": { dbConn.AbysisDb.Server = value; settedObject = "Conn"; param = "Server"; } break;
                case "DATABASE": { dbConn.AbysisDb.Database = value; settedObject = "Conn"; param = "Database"; } break;
                case "USERID":
                case "USER_ID":
                case "USER": { dbConn.AbysisDb.Userid = value; settedObject = "Conn"; param = "User"; } break;
                case "PASSWORD": { dbConn.AbysisDb.Password = value; settedObject = "Conn"; param = "Password"; } break;
                default:
                    {
                        try
                        {
                            if (value.StartsWith("+"))
                            {
                                value = value.Substring(1);
                                string oldValue = Parameters.GetPropertyValue(param);
                                value = oldValue + ";" + value;
                            }
                            setted = Parameters.SetPropertyValue(param, value);
                            if (setted)
                            {
                                settedObject = "Params";
                                param = Parameters.GetPropertyName(param);
                                value = Parameters.GetPropertyValue(param);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    break;
            }
            if (setted)
                Console.WriteLine(string.Format("{0}.{1} setted value {2}", settedObject, param, value));
        }

    }
}
