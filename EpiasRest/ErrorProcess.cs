using EpiasRest;
using System;
using System.Linq;

public static class ErrorProcess
{
    public static void SentDataResponseError(string error)
    {
        var recive = Newtonsoft.Json.JsonConvert.DeserializeObject<Epias.Recive.EpiasReciveAnswer>(error);
        if (recive.Body == null)
        {
            switch (recive.ResultCode)
            {
                case "user.001":
                    Helper.log.WriteLogLine(recive.ResultCode + "Kimlik Doğrulama Hatası", false);
                    break;
                case "user.002":
                    Helper.log.WriteLogLine(recive.ResultCode + "Geçersiz organizasyon girişi. Sayaç okuyan kurum (ED) kullanıcısı ile giriş yapınız.", false);
                    break;
                case "data.001":
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;
                case "data.002":
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;
                case "data.003":
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;
                case "data.004":
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;
                case "data.005":
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;
                case "data.006":
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;
                case "data.007":
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;
                case "data.008":
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;

                default:
                    Helper.log.WriteLogLine(recive.ResultCode + recive.ResultDescription, false);
                    break;

            }
        }
        else
        {
            foreach (var failed in recive.Body.Failed)
            {
                try
                {

                    int failedMeterId = EpiasDataManager.pack.EICData.Where(p => p.Code == failed.Eic).FirstOrDefault().MeterId;
                    var item = EpiasDataManager.SentIndexPairs.Where(p =>
                      p.MeterId == failedMeterId &&
                      (p.Time.ToString("yyyy-MM-ddTHH:mmzz") + "00")
                      .Equals(failed.MeteringTime))
                    .FirstOrDefault();
                    EpiasDataManager.SentIndexPairs.Remove(item);
                }
                catch (Exception ex)
                {
                    Helper.log.WriteLogLine("Error Process" + ex.Message);
                }
                switch (failed.Code)
                {

                    case "data.001":
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                    case "data.002":
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                    case "data.003":
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                    case "data.004":
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                    case "data.005":
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                    case "data.006":
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                    case "data.007":
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                    case "data.008":
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                    default:
                        Helper.log.WriteLogLine(failed.Code + "\t" + failed.Message + "\t" + failed.Eic + "\t" + failed.MeteringTime, false);
                        break;
                }
            }
        }
    }
}

