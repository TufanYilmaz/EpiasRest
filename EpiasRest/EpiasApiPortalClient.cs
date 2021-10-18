using Epias.Transparency.DayAheadMCP;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;


namespace EpiasRest
{
    class EpiasApiPortalClient
    {
        public EpiasApiPortalDataManager DataManager;
        //private string EpiasTransparencyBaseUri = "https://seffaflik.epias.com.tr"; //Old
        private string EpiasTransparencyBaseUri = "https://api.epias.com.tr"; // New
        //private string EpiasTransparencyDayAheadMCP = "/transparency/service/market/day-ahead-mcp";//Old
        private string EpiasTransparencyDayAheadMCP = "/epias/exchange/transparency/market/day-ahead-mcp";//New
        public EpiasApiPortalClient()
        {
            DataManager = new EpiasApiPortalDataManager();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;

        }
        public bool GetAndSaveDayAheadMCP(DateTime startDate, DateTime endDate)
        {
            bool result = false;
            DayAheadMCP data = null;
            string requestUrl = EpiasTransparencyBaseUri + EpiasTransparencyDayAheadMCP;
            WebRequest request = WebRequest.Create(requestUrl + "?startDate=" + startDate.ToString("yyyy-MM-dd") + "&endDate=" + endDate.ToString("yyyy-MM-dd"));
            request.Method = "GET";
            request.Headers.Clear();
            //request.Timeout = 3000;
            request.Headers.Add("X-IBM-Client-Id", "f487a472-c75c-4f7e-911c-bdc4db88113b");
            try
            {

                using (WebResponse response = request.GetResponse())
                {

                    using (Stream dataStream = response.GetResponseStream())
                    {
                        //Console.WriteLine(DateTime.Now + "   " + (int)((HttpWebResponse)response).StatusCode + ((HttpWebResponse)response).StatusCode);
                        using (StreamReader reader = new StreamReader(dataStream))
                        {
                            string responseFromServer = reader.ReadToEnd();
                            data = JsonConvert.DeserializeObject<Epias.Transparency.DayAheadMCP.DayAheadMCP>(responseFromServer);
                            if (DataManager.SaveDayAheadMCP(data))
                            {
                                result = true;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (ex is IndexOutOfRangeException)
                {
                    Helper.log.WriteLogLine("Sunucuda yarına ait veri yok", false);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }

                if (ex is TimeoutException)
                {
                    GetAndSaveDayAheadMCP(startDate, endDate);
                }
                else
                if (ex is WebException && ((WebException)ex).Response != null)
                {
                    try
                    {
                        using (WebResponse wr = ((WebException)ex).Response)
                        {
                            try
                            {
                                for (int i = 0; i < wr.Headers.Count; i++)
                                {
                                    var values = wr.Headers.GetValues(i);
                                    for (int j = 0; j < values.Length; j++)
                                    {
                                        Console.WriteLine(values[j]);
                                    }
                                }

                                if ((int)((HttpWebResponse)wr).StatusCode >= 400 && (int)((HttpWebResponse)wr).StatusCode <= 499)
                                {
                                    Console.WriteLine("hata");
                                }
                                else
                                {

                                    Helper.log.WriteLogLine("Sunucu ile ilgili bir problem var gibi", false);
                                    using (StreamReader rd = new StreamReader(((WebException)ex).Response.GetResponseStream()))
                                    {

                                        Helper.log.WriteLogLine(rd.ReadToEnd(), false);
                                    }
                                    //for (int i = 0; i < ((WebException)ex).Response.Headers.Count; i++)
                                    //{
                                    //    Console.WriteLine(((WebException)ex).Response.Headers[i]);

                                    //}
                                    //for (int i = 0; i < ((WebException)ex).Response.Headers.AllKeys.Length; i++)
                                    //{
                                    //    Console.WriteLine(((WebException)ex).Response.Headers.AllKeys[i]);
                                    //}
                                }
                            }
                            catch (Exception ex2)
                            {
                                Console.WriteLine(ex2.ToString());
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine(ex2.ToString());
                    }
                }
                //GetAndSaveDayAheadMCP(startDate, endDate);
            }
            finally
            {

            }

            return result;
        }
        public void SavePTFMonth(int month, bool dailyCheck = false)
        {
            DateTime begin;
            if (month == 0)
            {
                begin = new DateTime(DateTime.Now.Year - 1, 12, 1, 0, 0, 0);
            }
            else
            {
                begin = new DateTime(DateTime.Now.Year, month, 1, 0, 0, 0);
            }

            for (DateTime b = begin; b <= DateTime.Now.AddDays(1); b = b.AddDays(1))
            {
                if (DataManager.CheckMCPbyDate(b, dailyCheck))
                {
                    GetAndSaveDayAheadMCP(b, b);
                    System.Threading.Thread.Sleep(2000);
                }
            }
            if (!dailyCheck)
            {
                Console.WriteLine(DateTime.Now + " Aylık PTF kaydedildi");
            }
            //else
            //    Console.WriteLine(DateTime.Now+ " Aylık Eksik Olan Veri Yok" );
        }
    }
}
