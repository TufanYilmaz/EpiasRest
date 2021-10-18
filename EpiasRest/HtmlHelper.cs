using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpiasRest
{
    public static class HtmlHelper
    {
        static int cellPadding;
        public static string ConvertDataTableToHTML(this DataTable dt, string GroupBy="")
        { 
            string html = "";
            string ActiveGroup = "";
            cellPadding = 10;
            if (!string.IsNullOrEmpty(GroupBy))
                ActiveGroup = dt.Rows[0][GroupBy].ToString();
            else
            {
                ActiveGroup = dt.Rows[0][0].ToString();
                GroupBy = dt.Columns[0].ColumnName;
            }
            html = AddHeader(html, dt, GroupBy, ActiveGroup, true);
            
            //add rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (ActiveGroup != dt.Rows[i][GroupBy].ToString())
                {
                    ActiveGroup = dt.Rows[i][GroupBy].ToString();
                    html = AddHeader(html, dt, GroupBy, ActiveGroup, false);
                }
                html = AddLine(dt, html, GroupBy, i);
            }
            html += "</table><br>";
            return html;
        }

        private static string AddLine(DataTable dt, string html, string GroupBy, int i)
        {
            html += "<tr>";
            for (int j = 0; j < dt.Columns.Count; j++)
                if (dt.Columns[j].ColumnName != GroupBy)
                    html += "<td>" + dt.Rows[i][j].ToString() + "</td>";
            html += "</tr>";
            return html;
        }
 
 
        private static string AddHeader(string html, DataTable dt, string GroupBy, string ActiveGroup, bool first)
        {
            if (!first) html += "</table><br>";
            html += string.Format("<table Border=1 cellpadding={0}>",cellPadding);
            if (!string.IsNullOrEmpty(ActiveGroup))
                html += string.Format("<tr><th colspan={0}>{1}</th></tr>",dt.Columns.Count-1 , ActiveGroup);
            //add header row
            html += "<tr>";
            for (int i = 0; i < dt.Columns.Count; i++)
                if (dt.Columns[i].ColumnName != GroupBy)
                    html += "<th>" + dt.Columns[i].ColumnName + "</th>";
            html += "</tr>";
            return html;
        }
    }
}
