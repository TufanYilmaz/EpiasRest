using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using Newtonsoft.Json;
using System.Data;
using System.ComponentModel;

namespace EpiasRest
{
    public static class Helper
    {
        public static LogManager log = new LogManager();

        public static void sendErrorMail(string error)
        {

        }
        public static DataTable ToDataTable<T>(this IList<T> DataList)
        {

            PropertyDescriptorCollection Properties =

                TypeDescriptor.GetProperties(typeof(T));

            DataTable Table = new DataTable();

            foreach (PropertyDescriptor AProperty in Properties)

                Table.Columns.Add(AProperty.Name, Nullable.GetUnderlyingType(AProperty.PropertyType) ?? AProperty.PropertyType);

            foreach (T item in DataList)
            {

                DataRow Row = Table.NewRow();

                foreach (PropertyDescriptor AProperty in Properties)

                    Row[AProperty.Name] = AProperty.GetValue(item) ?? DBNull.Value;

                Table.Rows.Add(Row);

            }

            return Table;

        }
    }
}
