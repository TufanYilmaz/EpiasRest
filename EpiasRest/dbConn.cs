using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telemeter.Extensions;
//using Telemeter.Helpers;

namespace EpiasRest
{
    public class dbConn
    {
        public string ParamFile = string.Format("{0}\\conn.db", ArrayHelper.MainFolder);
        public string Server = "";
        public string Database = "";
        public string Userid = "";
        public string Password = "";
        public static dbConn AbysisDb = new dbConn();

        public dbConn()
        {

        }
        public dbConn(dbConn Source)
        {
            Assign(Source);
        }

        public string String
        {
            get
            {
                return getConnectionStr();
            }
            set
            {
                setConnectionStr(value);
            }
        }
        private string getConnectionStr()
        {
            string tmp = "";
            if ((Server != "") && (Database != "") && (Userid != "") && (Password != "")) tmp = string.Format("Server={0};Database={1};User Id={2};Password={3}", Server, Database, Userid, Password);
            return tmp;
        }

        private void setConnectionStr(string str)
        {
            //"Server={0};Database={1};User Id={2};Password={3}" 
            string[] list = str.Split(';');
            foreach (string s in list)
            {
                var t = s.Trim();
                if (t.StartsWith("Server=")) Server = t.Replace("Server=", "");
                else
                if (t.StartsWith("Database=")) Database = t.Replace("Database=", "");
                else
                if (t.StartsWith("User Id=")) Userid = t.Replace("User Id=", "");
                else
                if (t.StartsWith("Password=")) Password = t.Replace("Password=", "");
            }
        }

        public void Assign(dbConn Source)
        {
            ParamFile = Source.ParamFile;
            Server = Source.Server;
            Database = Source.Database;
            Userid = Source.Userid;
            Password = Source.Password;
        }

        public override string ToString()
        {
            return String;
        }

        public string Encripted
        {
            get
            {
                return String.toBase64();
            }
            set
            {
                String = value.fromBase64();
            }
        }

        public bool Load()
        {
            bool result = false;
            if (File.Exists(ParamFile))
            {
                Encripted = File.ReadAllText(ParamFile);
                result = true;
            }
            return result;
        }

        public void Save()
        {
            File.WriteAllText(ParamFile, Encripted);
        }

        public string exMessage = "";

        public bool ServerAvailable()
        {
            bool result = false;
            SqlConnection test = new SqlConnection(String);
            try
            {
                test.Open();
                test.Close();
                result = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                exMessage = e.Message;
            }
            return result;
        }
    }
}