using ClosedXML.Excel;
using Epias.Send;
using RegisterModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace EpiasRest.EpiasDataAccess
{
    enum EpiasSubcriptionCallType { NONE = -1, INSTANT = 0, DAYEND = 1 };
    interface IEpiasDataManager
    {
        Attachment GetDayEndReportAttachment();
        Stream GetDayEndEpiasXMLStream(XLWorkbook Workbook);
        EpiasSendableData EpiasJsonData(SubscriptionCallType callType, SentState sentState);
        List<OsosDataTypeList> GetOsosDataWithPackProfiles(List<ProfileIndexPair> profiles);
        void OsosConfig(Epias.Recive.Config.EpiasOsosConfig config);
        void UpdateSentData();
        void DisableMeteringPoints(string Eic);
        void WriteErrors(string errorText);
        DataTable MailBodyData();
    }
}
