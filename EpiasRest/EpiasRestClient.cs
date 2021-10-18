using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Data;
using System.Net;
using System.IO;
using Epias;
using Epias.Send;
using Newtonsoft.Json;
using RegisterModels;

namespace EpiasRest
{
    class EpiasRestClient
    {
        HttpResponseMessage response { get; set; }
        public DataTable data { get; set; }
        private string TGTWithUri { get; set; }
        private string TGT { get; set; }
        private string ST { get; set; }
        public EpiasDataManager DataManager;

        private string CasBaseUri = "https://cas.epias.com.tr";
        private string CasRequest = "/cas/v1/tickets";
        private string CasOsosService = "https://osos.epias.com.tr";
        private string OsosDataUrl;
        //private string OsosRestUrlT = "https://osos-test.epias.com.tr/osos-web/rest";
        private string OsosRestUrl = "https://osos-test.epias.com.tr/osos-web/rest";
        //private string ososWeb = "osos-web";
        //private string rest = "rest";
        private string ososData = "ososData";
        private string getOsosData = "getOSOSData";
        private string ososConfig = "ososConfig";
        //private string getOsosDataUrl = "https://osos-test.epias.com.tr/osos-web/rest/ososData/getOsosData";

        IEnumerable<KeyValuePair<string, string>> LoginKeyValue;
        public EpiasRestClient(string username, string password, string ososDataURL)
        {
            //Helper.log.WriteLogLine(username +"   "+password);
            DataManager = new EpiasDataManager();
            LoginKeyValue = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("format","text"),
                new KeyValuePair<string,string>("username",username),
                new KeyValuePair<string,string>("password",password)
            };
            OsosDataUrl = ososDataURL;
            if(!ososDataURL.Contains("test"))
                OsosRestUrl = "https://osos.epias.com.tr/osos-web/rest";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        public EpiasRestClient()
        {
            DataManager = new EpiasDataManager();
            LoginKeyValue = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("format","text"),
                new KeyValuePair<string,string>("username","Username"),
                new KeyValuePair<string,string>("password","Password")
            };
            OsosDataUrl = "https://osos-test.epias.com.tr/osos-web/rest/ososData/";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        public void GetOsosData(string eic, string date)
        {
            GetServiceTicket();
            Thread.Sleep(200);
            string requestUrl = OsosRestUrl + "/" + ososData + "/" + getOsosData;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl + "?eic=" + eic + "&startTime=" + date);
            request.Method ="GET";
            //request.Headers.Clear();
            request.ContentType = "application/json";
            request.Headers.Add("Service-Key", ST);
            //Console.WriteLine(request.Headers);
            try
            {

                using (WebResponse response = request.GetResponse())
                {

                    using (Stream dataStream = response.GetResponseStream())
                    {
                        Console.WriteLine(DateTime.Now + "   " + (int)((HttpWebResponse)response).StatusCode);
                        using (StreamReader reader = new StreamReader(dataStream))
                        {
                            string responseFromServer = reader.ReadToEnd();
                            Console.WriteLine(responseFromServer);
                        }
                    }
                }

            }
            catch (WebException we)
            {
                using (WebResponse wr = we.Response)
                {
                    using (Stream responseStream = wr.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                        string errorText = reader.ReadToEnd();
                        Console.WriteLine(errorText);
                    }
                    if ((int)((HttpWebResponse)wr).StatusCode >= 400 && (int)((HttpWebResponse)wr).StatusCode <= 499)
                        Helper.log.WriteLogLine((int)((HttpWebResponse)wr).StatusCode+" Hatası", false);
                    else
                        Helper.log.WriteLogLine("Sunucu ile ilgili bir problem var", false);

                    Thread.Sleep(500);
                }
            }
            Thread.Sleep(300);
        }
        

        public void OsosConfig()
        {
            GetServiceTicket();
            string requestUrl= OsosRestUrl + "/" + ososConfig + "/";

            WebRequest request = WebRequest.Create(requestUrl);
            request.Method = "GET";
            request.Headers.Clear();
            request.ContentType = "application/json";
            request.Headers.Add("Service-Key", ST);
            try
            {

                using (WebResponse response = request.GetResponse())
                {

                    using (Stream dataStream = response.GetResponseStream())
                    {
                        Console.WriteLine(DateTime.Now + "   " + (int)((HttpWebResponse)response).StatusCode);
                        using (StreamReader reader = new StreamReader(dataStream))
                        {
                            string responseFromServer = reader.ReadToEnd();
                            var recive = JsonConvert.DeserializeObject<Epias.Recive.Config.EpiasOsosConfig>(responseFromServer);
                            DataManager.OsosConfig(recive);
                            Console.WriteLine(responseFromServer);
                        }
                    }
                }

            }
            catch (WebException we)
            {
                using (WebResponse wr = we.Response)
                {
                    using (Stream responseStream = wr.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                        string errorText = reader.ReadToEnd();
                        Console.WriteLine(errorText);
                    }
                    if ((int)((HttpWebResponse)wr).StatusCode >= 400 && (int)((HttpWebResponse)wr).StatusCode <= 499)
                    {
                        Helper.log.WriteLogLine((int)((HttpWebResponse)wr).StatusCode+" Hatası", false);
                    }
                    else
                        Helper.log.WriteLogLine("Sunucu ile ilgili bir problem var", false);

                    Thread.Sleep(500);
                }
            }
            Thread.Sleep(300);
        }
        static int tgtTryCount = 0;
        public async void GetTicketGrandingTicket()
        {
            string responseContent;
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(CasBaseUri);
                    using (var req = new HttpRequestMessage(HttpMethod.Post, CasRequest) { Content = new FormUrlEncodedContent(LoginKeyValue) })
                    {
                        response = await client.SendAsync(req);
                        responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseContent);
                    }
                }
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    TGT = responseContent;
                    Helper.log.WriteLogLine("Tgt Oluşturuldu ( " + TGT + " ) " + response.StatusCode);
                    tgtTryCount = 0;
                }
                else
                {
                    Helper.log.WriteLogLine("TGT oluşturulamadı!  " + response.StatusCode, false);
                    if(tgtTryCount++>10)
                    {
                        Parameters.isOSOSRunning = false;
                    }
                    string res = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(res);
                    if (response.StatusCode==HttpStatusCode.NotAcceptable)
                    {
                        try
                        {
                            Mailer.Instance.Send(Parameters.AdminMails,
                                "Epiaş Kullanıcı Adı Parola",Parameters.OSB+
                                @"Epiaş kullanıcı adı ve parolanın süresi dolmuştur
lütfen EKYS portalından parolanızı yineleyerek servisi yeniden başlatınız",
                                BCc:"tufan@merkez.com.tr");
                        }
                        catch (Exception ex)
                        {
                            if(!string.IsNullOrEmpty(Parameters.AdminMails))
                            {
                                Helper.log.WriteLogLine("Mail Tanımlanmamış! " + ex.Message, false);
                            }
                            Helper.log.WriteLogLine("Hata Maili Gönderilemedi! " + ex.Message, false);
                        }
                    }
                }

            }
            catch (WebException we)
            {
                WebResponse errorResponse = we.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();
                    //Console.WriteLine(errorText);
                }
                Helper.log.WriteLogLine("TGT alınırken bir hata oluştu!", false);
            }
            Thread.Sleep(300);
        }

        public void GetServiceTicket()
        {
            string requestUrl = CasBaseUri + CasRequest + "/" + TGT;
            WebRequest request = WebRequest.Create(requestUrl + "?service=" + CasOsosService);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            try
            {

                using (WebResponse response = request.GetResponse())
                {

                    using (Stream dataStream = response.GetResponseStream())
                    {
                        //Console.WriteLine(DateTime.Now + "   " + (int)((HttpWebResponse)response).StatusCode);
                        using (StreamReader reader = new StreamReader(dataStream))
                        {
                            string responseFromServer = reader.ReadToEnd();
                            ST = responseFromServer;
                            Console.WriteLine(ST);
                        }
                    }
                }

            }
            catch (WebException we)
            {
                using (WebResponse wr = we.Response)
                {
                    if ((int)((HttpWebResponse)wr).StatusCode >= 400 && (int)((HttpWebResponse)wr).StatusCode <= 499)
                    {
                        Helper.log.WriteLogLine("TGT session süresi dolmus tekrar TGT Alınacak (Service Ticket Oluşturulamadı)", false);
                        GetTicketGrandingTicket();
                    }
                    else
                        Helper.log.WriteLogLine("Sunucu ile ilgili bir problem var", false);

                    Thread.Sleep(500);
                    if(Parameters.isOSOSRunning)
                        GetServiceTicket();
                }
            }
            catch (Exception ex)
            {
                Helper.log.WriteLogLine("!EXCEPTION " + ex.ToString(), false);
            }
            Thread.Sleep(300);
        }

        public bool SendEpiasData(SubscriptionCallType callType,SentState sentState,int dayStart=-3)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            EpiasSendableData Data = DataManager.EpiasJsonData(callType,sentState,dayStart);
            if (Data == null || Data.Body.OsosDataTypeList.Length == 0)
            {
                return false;
            }
            //string jsonDemo = JsonConvert.SerializeObject(Data);
            //DataManager.UpdateSentData();
            Epias.Recive.EpiasReciveAnswer reciveAnswer = new Epias.Recive.EpiasReciveAnswer();
            GetServiceTicket();
            Data.Header[0].Value = ST;
            string jsonContent = JsonConvert.SerializeObject(Data);
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(OsosDataUrl);
            var request = WebRequest.Create(OsosDataUrl);
            request.Method = "POST";
            request.ContentType = @"application/json";

            System.Text.UTF8Encoding encoding = new UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(jsonContent);
            request.ContentLength = byteArray.Length;


            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            bool result = false;
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream dataStream = response.GetResponseStream())
                    {
                        //Console.WriteLine(response.Headers.ToString());
                        using (StreamReader reader = new StreamReader(dataStream))
                        {
                            string responseFromServer = reader.ReadToEnd();
                            var recive = JsonConvert.DeserializeObject<Epias.Recive.EpiasReciveAnswer>(responseFromServer);
                            Helper.log.WriteLogLine("Cevap Alındı  ResultCode=" + recive.ResultCode
                                + "\tResultDescription=" + recive.ResultDescription
                                + "\tSuccesCount=" + recive.Body.SuccessCount
                                + "\tFailedCount=" + recive.Body.Failed.Length);
                            //Console.WriteLine(responseFromServer);
                            result = (Data.Body.OsosDataTypeList.Length == recive.Body.SuccessCount);
                            if (result)
                            {
                                DataManager.UpdateSentData();
                                Helper.log.WriteLogLine("Sent Data UPDATED");
                            }
                            else
                            {
                                //ErrorProcess.SentDataResponseError(responseFromServer);
                                //DataManager.WriteErrors(responseFromServer);
                                //Helper.log.WriteLogLine(recive.Body.Failed.First().Message,false);
                                //foreach (var item in recive.Body.Failed)
                                //{
                                //    Helper.log.WriteLogLine(item.Message, false);

                                //}
                                DataManager.UpdateSentData();
                            }
                            result = true;
                        }
                    }
                }
                return result;
            }
            catch (WebException we)
            {
                Helper.log.WriteLogLine(we.Message, false);
                using (WebResponse errorResponse = we.Response)
                {
                    using (Stream responseStream = errorResponse.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                        string errorText = reader.ReadToEnd();
                        var recive = JsonConvert.DeserializeObject<Epias.Recive.EpiasReciveAnswer>(errorText);
                        DataManager.WriteErrors(errorText);
                        //ErrorProcess.SentDataResponseError(errorText);
                        Helper.log.WriteLogLine((int)((HttpWebResponse)errorResponse).StatusCode + " Hatası döndü!" + " ResultCode=" + recive.ResultCode + "  resultDesciption=" + recive.ResultDescription, false);
                        
                    }
                }
            }
            Thread.Sleep(300);
            return result;
        }
    }
}
