using ClosedXML.Excel;
using Epias.Send;
using RegisterModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using TcpModels;

namespace EpiasRest
{
    class EpiasDataManager
    {
        private SqlConnection Conn = null;
        //DataTable epiasData = null;
        private string connectionString;
        List<int> SentDataIDs = new List<int>();
        public static List<ProfileIndexPair> SentIndexPairs = new List<ProfileIndexPair>();
        public static DataPack pack = new DataPack();
        public EpiasDataManager()
        {
            if (dbConn.AbysisDb.Load())
                connectionString = dbConn.AbysisDb.ToString();
            this.Conn = new SqlConnection(connectionString);
            try
            {
                if (Conn.State != ConnectionState.Open)
                    Conn.Open();
            }
            catch (Exception)
            {
                Helper.log.WriteLogLine("Epias (Veri Tabanı) Erişimi Sağlanamadı!", false);
            }
        }
        public enum EpiasSubcriptionCallType { NONE = -1, INSTANT = 0, DAYEND = 1 };

        //public IEnumerable<ProfileIndexPair> GetEpiasData(DateTime date,SubscriptionCallType callType)
        //{
        //    var res=RegisterDbHelper.GetClient().FindEpiasProfiles(date, callType);
        //    return res.Profiles;
        //}

        public Attachment GetDayEndReportAttachment()
        {
            Helper.log.WriteLogLine("Mail için attechment oluşturuluyor");
            Attachment result = null;
            try
            {
                XLWorkbook wb = new XLWorkbook();
                DataTable dt = MailBodyData();
                wb.Worksheets.Add(dt, "EpiasSentData");
                //using (Stream xs=new MemoryStream())
                //{
                //    wb.SaveAs(xs);
                //    xs.Position = 0;
                //    result = new Attachment(xs, "EpiasSentData");
                //}
                using (Stream epiasStream = GetDayEndEpiasXMLStream(wb))
                {
                    result = new Attachment(epiasStream, "EpiasSentData.xlsx");
                }
            }
            catch (Exception ex)
            {
                Helper.log.WriteLogLine("Attachment oluşturulamadı" + ex.Message, false);
            }
            return result;
        }
        public Stream GetDayEndEpiasXMLStream(XLWorkbook Workbook)
        {
            Stream fs = new MemoryStream();
            Workbook.SaveAs(fs);
            fs.Position = 0;
            return fs;
        }

        public EpiasSendableData EpiasJsonData(SubscriptionCallType callType, SentState sentState,int dayStart=-3)
         {
            List<ProfileIndexPair> res = new List<ProfileIndexPair>();

            EpiasSendableData result = new EpiasSendableData();
            List<OsosDataTypeList> ososData = new List<OsosDataTypeList>();
            SentIndexPairs.Clear();
            var client = RegisterDbHelper.GetClient();
            //pack = client.FindEpiasProfiles(DateTime.Today.AddDays(-2), callType, SentState.Sent);
            DateTime today = DateTime.Today.AddDays(-1);
            for (DateTime d = today; d > today.AddDays(dayStart); d = d.AddDays(-1))
            {
                pack = client.FindEpiasProfiles(d, callType, sentState);
                if (pack.Profiles != null)
                    ososData.AddRange(GetOsosDataWithPackProfiles(pack.Profiles.ToList()));
            }

            client.Disconnect();

            Header[] header = new Header[1];
            header[0] = new Header
            {
                Key = "Service-Key",
                Value = string.Empty
            };
            //SentDataIDs.Clear();
            result.Header = header;
            //int i = 0;
            if (ososData.Count <= 0)
                return null;
            Helper.log.WriteLogLine("Pack length = " + ososData.Count);
            //foreach (var item in pack.Profiles)
            //{
            //    var eICData = pack.EICData.Where(p => p.MeterId == item.MeterId).FirstOrDefault();
            //    ososData.Add(new OsosDataTypeList
            //    {
            //        Eic = eICData.Code,//item.EIC,
            //        MeteringTime = item.Time.ToString("yyyy-MM-ddTHH:mmzz") + "00",//(items[0].CALCDATETIME_).ToString("yyyy-MM-ddTHH:mmzz") + "00",
            //        Period = eICData.Period.ToString(),
            //        MeteringType = eICData.DepartmentId,
            //        ConsumptionAmount = (double)item.ConsumptionValue,
            //        GenerationAmount = (double)item.GenerationValue
            //    });
            //    SentIndexPairs.Add(item);
            //}
            Epias.Send.Body body = new Epias.Send.Body
            {
                OsosDataTypeList = ososData.ToArray()
            };
            result.Body = body;
            result.Header = header;
            return result;
        }
        private List<OsosDataTypeList> GetOsosDataWithPackProfiles(List<ProfileIndexPair> profiles)
        {
            var res = new List<OsosDataTypeList>();
            if (profiles is null || profiles.Count <= 0)
                return res;
            foreach (var item in profiles)
            {
                var eICData = pack.EICData.Where(p => p.MeterId == item.MeterId).FirstOrDefault();
                res.Add(new OsosDataTypeList
                {
                    Eic = eICData.Code,//item.EIC,
                    MeteringTime = item.Time.ToString("yyyy-MM-ddTHH:mmzz") + "00",//(items[0].CALCDATETIME_).ToString("yyyy-MM-ddTHH:mmzz") + "00",
                    Period = eICData.Period.ToString(),
                    MeteringType = eICData.DepartmentId,
                    ConsumptionAmount = (double)item.ConsumptionValue,
                    GenerationAmount = (double)item.GenerationValue
                });
                SentIndexPairs.Add(item);
            }
            return res;
        }
        public void OsosConfig(Epias.Recive.Config.EpiasOsosConfig config)
        {
            // Osos Config Veritabanına yazım Başlatıldı");
            #region OldCode
            //if (Conn.State != ConnectionState.Open)
            //{
            //    Conn = new SqlConnection(connectionString);
            //    Conn.Open();
            //}
            //SqlCommand cmd = new SqlCommand("TRUNCATE TABLE tb_EPIAS_OSOSCONFIG", Conn);
            //try
            //{
            //    //SqlDataAdapter adap = new SqlDataAdapter(cmd);
            //    //adap.Fill(EICData);
            //    cmd.ExecuteNonQuery();
            //    DataTable target = (from c in config.Body.OsosDataTypeList.AsEnumerable()
            //                        select new
            //                        {
            //                            EIC_CODE = c.Eic,
            //                            PERIOD = c.Period,
            //                            METERTYPE = c.MeterType,
            //                            BALANCETYPE = c.BalanceType
            //                        }).ToList().ToDataTable();
            //    SqlBulkCopy ososConfigtoDatabase = new SqlBulkCopy(Conn, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.UseInternalTransaction, null)
            //    {
            //        DestinationTableName = "tb_EPIAS_OSOSCONFIG"
            //    };
            //    ososConfigtoDatabase.WriteToServer(target);

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(DateTime.Now + "GetEicData " + ex.ToString());
            //}
            //finally
            //{
            //    if (Conn.State == ConnectionState.Open)
            //        Conn.Close();
            //}
            // Osos Config Veritabanına yazım bitti");
            #endregion



            var client = RegisterDbHelper.GetClient();
            var EICList = new List<EICData>();
            var EICdevices = client.FindEICDevices();
            foreach (var item in config.Body.OsosDataTypeList)
            {
                bool active = EICdevices.Devices?.FirstOrDefault(d => d.EICData.Code == item.Eic)?.EICData.Active ?? true ;
                EICList.Add(new EICData
                {
                    Code = item.Eic,
                    BalanceType = item.BalanceType,
                    Active = active,
                    DepartmentId = 1,
                    MeterType = item.MeterType,
                    Period = item.Period,
                    Source = "EPIAS",
                });
            }
            //todo: GetEICDevices ile active durumu kontrol et
            try
            {
                client.AddOrUpdateEIC(EICList.ToArray());
                Helper.log.WriteLogLine("OsosConfig güncellendi");
            }
            catch (Exception ex)
            {
                Helper.log.WriteLogLine(ex.Message, false);
            }
            finally
            {
                client.Disconnect();
            }
        }


        public void UpdateSentData()
        {

            SentIndexPairs.ForEach(p => p.Sent = true);
            var client = RegisterDbHelper.GetClient();
            client.UpdateProfiles(SentIndexPairs);
            client.Disconnect();
        }

        public void DisableMeteringPoints(string Eic)
        {
            #region OldCode
            //// Sayaç  Veritabanında pasif hale getiriliyor ");
            //if (Conn.State != ConnectionState.Open)
            //{
            //    Conn = new SqlConnection(connectionString);
            //    Conn.Open();
            //}
            //bool status = true;
            //SqlCommand cmd = new SqlCommand(@"SELECT ES.*, P.INFO AS EIC FROM tb_EPIAS_SUBSCRIPTIONS  ES WITH(NOLOCK)
            //                        LEFT OUTER JOIN tb_PMUM_MEASURING_POINTS P WITH(NOLOCK)
            //                        ON ES.SUBSCRIPTION_ID = P.SUBSCRIPTION_ID
            //                        WHERE P.INFO = @eic AND ES.ACTIVE=0");
            //DataTable existingData = new DataTable();
            //try
            //{
            //    cmd.Connection = Conn;
            //    cmd.Parameters.Clear();
            //    cmd.Parameters.AddWithValue("@eic", Eic);
            //    SqlDataAdapter adap = new SqlDataAdapter(cmd);
            //    adap.Fill(existingData);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
            //finally
            //{
            //    if (Conn.State == ConnectionState.Open)
            //        Conn.Close();
            //}
            //if (existingData.Rows.Count > 0)
            //    status = false;
            //if (status)
            //{
            //    if (Conn.State != ConnectionState.Open)
            //    {
            //        Conn = new SqlConnection(connectionString);
            //        Conn.Open();
            //    }

            //    cmd = new SqlCommand(@"UPDATE ES
            //                         SET ACTIVE = 0
            //                         FROM tb_EPIAS_SUBSCRIPTIONS ES 
            //                         LEFT OUTER JOIN tb_PMUM_MEASURING_POINTS P
            //                         ON ES.SUBSCRIPTION_ID = P.SUBSCRIPTION_ID
            //                         WHERE P.INFO LIKE @eic");
            //    try
            //    {
            //        cmd.Connection = Conn;
            //        cmd.Parameters.Clear();
            //        cmd.Parameters.AddWithValue("@eic", Eic);
            //        cmd.ExecuteNonQuery();
            //        Helper.log.WriteLogLine(Eic + " Ölçüm noktası pasif hale getirildi.", false);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.ToString());
            //    }
            //    finally
            //    {
            //        if (Conn.State == ConnectionState.Open)
            //            Conn.Close();
            //    }
            //} 
            // Sayaç  Veritabanında pasif hale getirildi ");
            #endregion

            var client = RegisterDbHelper.GetClient();
            var devices = client.FindEICDevices();
            var failedEice = devices.Devices.First(d => d.EICData.Code == Eic);
            failedEice.EICData.Active = false;
            failedEice.EICData.Source = "EPIAS";
            client.AddOrUpdateEIC(failedEice.EICData);
            //Mailer.Send(Parameters.AdminMails, "Epiaş Hata ");
            client.Disconnect();
        }

        public void WriteErrors(string errorText)
        {
            // Gönderim hatası yazılımı başladı");
            ErrorProcess.SentDataResponseError(errorText);
            SqlCommand cmd = new SqlCommand(@"INSERT INTO tb_EPIAS_ERRORS VALUES(
                                                 @eic,
                                                 (SELECT SUBSCRIPTION_ID FROM tb_PMUM_MEASURING_POINTS WITH(NOLOCK) WHERE INFO=@eic),
                                                 @date,
                                                 @errorcode,
                                                 @message,
                                                 @createddate)");
            var recive = Newtonsoft.Json.JsonConvert.DeserializeObject<Epias.Recive.EpiasReciveAnswer>(errorText);
            if (recive.Body == null)
            {
                ErrorProcess.SentDataResponseError(errorText);
                try 
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@eic", "null");
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@errorcode", recive.ResultCode);
                    cmd.Parameters.AddWithValue("@message", recive.ResultDescription);
                    cmd.Parameters.AddWithValue("@createddate", DateTime.Now);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                return;
            }

            if (recive.Body.Failed.Length > 0)
            {
                try
                {
                    if (Conn.State != ConnectionState.Open)
                        Conn.Open();
                    cmd.Connection = Conn;

                    foreach (var error in recive.Body.Failed)
                    {
                        try
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@eic", error.Eic);
                            cmd.Parameters.AddWithValue("@date", Convert.ToDateTime(error.MeteringTime));
                            cmd.Parameters.AddWithValue("@errorcode", recive.ResultCode);
                            cmd.Parameters.AddWithValue("@message", error.Message);
                            cmd.Parameters.AddWithValue("@createddate", DateTime.Now);

                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helper.log.WriteLogLine("DB hatası " + ex.Message, false);
                    throw;
                }
                finally
                {
                    if (Conn.State == ConnectionState.Open)
                        Conn.Close();
                }
                var errorSummary = recive.Body.Failed.ToLookup(p => new { p.Eic, p.Code }, p=>p);
                string errorsSummary = string.Empty;
                foreach (var item in errorSummary)
                {
                    errorsSummary += item.Key.Eic + " " + item.Key.Code + " :" + item.First().Message;
                    if (item.Key.Code == "data.001" || item.Key.Code == "data.002")
                    {
                        DisableMeteringPoints(item.Key.Eic);
                        errorsSummary += " (Bu ölçüm noktasın gönderimi pasif hale getirildi)";
                    }
                    errorsSummary += "<br>";
                }
                Mailer.Instance.Send(Parameters.AdminMails, 
                    "Epiaş Servis Hatası",
                    "Veri Gönderim Hatası" + "<br>" + Parameters.OSB + "<br>" + errorsSummary,
                    BCc: "tufan@merkez.com.tr");
            }
            // Gönderim hatası yazılımı Bitti");
        }

        public DataTable GetMailTable(string UserName)
        {
            if (Conn.State != ConnectionState.Open)
                Conn.Open();
            DataTable result = new DataTable();
            var parts = UserName.Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
            string abysisUser = parts[0].Trim();
            try
            {

                SqlCommand cmd = new SqlCommand(@"
                        DECLARE @abysisUserID AS INT
                        SET @abysisUserID=(SELECT TOP 1 ID FROM def_USERS where CODE=@abysisUserName ORDER BY ID)
                        SELECT * FROM (
                        SELECT * FROM def_PROPERTIES WITH (NOLOCK) WHERE USERID=@abysisUserID AND 
                        DEPARTMENT_ID=1 UNION SELECT * FROM def_PROPERTIES WITH(NOLOCK)
                        WHERE PROPERTYNAME NOT IN(SELECT PROPERTYNAME FROM def_PROPERTIES WITH(NOLOCK) WHERE USERID=@abysisUserID AND 
                        DEPARTMENT_ID=(SELECT TOP 1 DEPARTMENT_ID FROM def_PROPERTIES WHERE USERID=@abysisUserID AND 
                        PROPERTYNAME LIKE 'Mail%')) AND USERID=0 AND DEPARTMENT_ID=(SELECT TOP 1 DEPARTMENT_ID FROM def_PROPERTIES WHERE USERID=@abysisUserID 
                        AND PROPERTYNAME LIKE 'Mail%') UNION SELECT * FROM def_PROPERTIES WITH (NOLOCK)WHERE DEPARTMENT_ID=0 AND USERID=0) P 
                        WHERE PROPERTYNAME LIKE 'Mail%' ORDER BY DEPARTMENT_ID, USERID DESC", Conn);
                cmd.Parameters.AddWithValue("@abysisUserName", abysisUser);
                SqlDataAdapter adap = new SqlDataAdapter(cmd);
                adap.Fill(result);
            }
            catch(Exception ex)
            {
                Helper.log.WriteLogLine(ex.Message, false);
            }
            finally
            {
                Conn.Close();
            }

            return result;
        }


        public DataTable MailBodyData()
        {
            DataTable result = new DataTable();

            result.Columns.Add("ABONE ADI", typeof(string));
            result.Columns.Add("ZAMAN", typeof(DateTime));
            result.Columns.Add("TÜKETİM", typeof(double));
            result.Columns.Add("ÜRETİM", typeof(double));
            result.Columns.Add("GÖNDERİLME ZAMANI", typeof(DateTime));

            #region OldCode

            //try
            //{
            //    if (Conn.State != ConnectionState.Open)
            //    {
            //        Conn = new SqlConnection(connectionString);
            //        Conn.Open();
            //    }
            //    SqlCommand cmd = new SqlCommand(@"SELECT S.CODE+' - '+S.INFO AS [ABONE ADI],EI.DATETIME_ AS [ZAMAN],EI.CONSUMPTION_VALUE AS [TÜKETİM],EI.GENERATION_VALUE AS [ÜRETİM],EI.SENT_DATETIME AS [GÖNDERİLME ZAMANI] FROM tb_EPIAS_INDEXES EI WITH(NOLOCK)
            //                                    LEFT OUTER JOIN tb_PMUM_MEASURING_POINTS P WITH(NOLOCK)
            //                                    ON EI.SUBSCRIPTION_ID=P.SUBSCRIPTION_ID
            //								    LEFT OUTER JOIN tb_METERS M WITH(NOLOCK)
            //								    ON M.ID = EI.METER_ID
            //                                    LEFT OUTER JOIN tb_EPIAS_SUBSCRIPTIONS ES WITH(NOLOCK)
            //									ON EI.SUBSCRIPTION_ID = ES.SUBSCRIPTION_ID 
            //									LEFT OUTER JOIN tb_SUBSCRIPTION S WITH(NOLOCK)
            //									ON EI.SUBSCRIPTION_ID = S.ID
            //                                    WHERE EI.SENT=1 AND ( DATETIME_ > @dayBegin AND DATETIME_ <=@dayEnd) AND ES.ACTIVE=1
            //                                    ORDER BY S.CODE,EI.DATETIME_");
            //    //epiasData = null;
            //    //todo: rapor için findepiasprofiles da sen olanlar çekilecek
            //    try
            //    {
            //        //int selected= rnd.Next(5, 10);
            //        //selected = 1;
            //        cmd.Connection = Conn;
            //        cmd.Parameters.Clear();
            //        cmd.Parameters.AddWithValue("@dayBegin", new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, (DateTime.Now.AddDays(-1)).Day, 0, 0, 0));
            //        cmd.Parameters.AddWithValue("@dayEnd", DateTime.Now);
            //        SqlDataAdapter adap = new SqlDataAdapter(cmd);
            //        adap.Fill(result);
            //    }
            //    catch (Exception)
            //    {
            //        Helper.log.WriteLogLine("Veriler alınırken bir hata oluştu.(Mail Sender)", false);
            //    }
            //    finally
            //    {
            //        cmd.Dispose();
            //    }
            //}
            //catch (Exception ex) { Helper.log.WriteLogLine(ex.Message); }
            //finally
            //{
            //    if (Conn.State == ConnectionState.Open)
            //        Conn.Close();
            //}
            #endregion

            var client = RegisterDbHelper.GetClient();
            var data = client.FindEpiasProfiles(DateTime.Today.AddDays(-1), SubscriptionCallType.DayEnd, SentState.Sent);
            var groups = data.Profiles.ToLookup(p => p.MeterId, p => p);

            foreach (var group in groups)
            {
                var subscriber = data.Devices.First(p => p.MeterId == group.Key).History.First().Value;
                string subInfo = subscriber.Code + " " + subscriber.Info;
                foreach (var profile in group.ToList())
                {
                    DataRow row  =result.NewRow();
                    row["ABONE ADI"] = subInfo;
                    row["ZAMAN"] = profile.Time;
                    row["TÜKETİM"] = profile.ConsumptionValue;
                    row["ÜRETİM"] = profile.GenerationValue;
                    row["GÖNDERİLME ZAMANI"] = profile.SentTime;
                    result.Rows.Add(row);
                }
            }
            client.Disconnect();
            return result;
        }
    }
}
