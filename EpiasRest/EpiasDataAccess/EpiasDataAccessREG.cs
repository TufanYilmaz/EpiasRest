using ClosedXML.Excel;
using Epias.Recive.Config;
using Epias.Send;
using RegisterModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using TcpModels;

namespace EpiasRest.EpiasDataAccess
{
    class EpiasDataAccessREG:IEpiasDataManager
    {

        private SqlConnection Conn = null;
        //DataTable epiasData = null;
        private string connectionString;
        List<int> SentDataIDs = new List<int>();
        public static List<ProfileIndexPair> SentIndexPairs = new List<ProfileIndexPair>();
        public static DataPack pack = new DataPack();
        public EpiasDataAccessREG()
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
        public void DisableMeteringPoints(string Eic)
        {
            throw new NotImplementedException();
        }

        public EpiasSendableData EpiasJsonData(SubscriptionCallType callType, SentState sentState)
        {
            throw new NotImplementedException();
        }

        public Stream GetDayEndEpiasXMLStream(XLWorkbook Workbook)
        {
            throw new NotImplementedException();
        }

        public Attachment GetDayEndReportAttachment()
        {
            throw new NotImplementedException();
        }

        public List<Epias.Send.OsosDataTypeList> GetOsosDataWithPackProfiles(List<ProfileIndexPair> profiles)
        {
            throw new NotImplementedException();
        }

        public DataTable MailBodyData()
        {
            throw new NotImplementedException();
        }

        public void OsosConfig(EpiasOsosConfig config)
        {
            throw new NotImplementedException();
        }

        public void UpdateSentData()
        {
            throw new NotImplementedException();
        }

        public void WriteErrors(string errorText)
        {
            throw new NotImplementedException();
        }
    }
}
