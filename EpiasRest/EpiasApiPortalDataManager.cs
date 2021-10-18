using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epias.Transparency.DayAheadMCP;
using System.Threading;

namespace EpiasRest
{
    class EpiasApiPortalDataManager
    {
        private SqlConnection Conn = null;
        private string connectionString;

        public EpiasApiPortalDataManager()
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
                Helper.log.WriteLogLine("Veri (Tabanı) Erişimi Sağlanamadı! (PTF Data)", false);
            }
        }

        public bool CheckMCPbyDate(DateTime day,bool dailyCheck=false)
        {
            bool result = false;
            if (Conn.State != ConnectionState.Open)
            {
                Conn = new SqlConnection(connectionString);
                Conn.Open();
            }

            string sql = @"SELECT * FROM tb_EPIAS_PTF_DETAILS WITH(NOLOCK) WHERE DATETIME_ = @d ORDER BY ID DESC";
            SqlCommand cmd = new SqlCommand(sql);
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@d", day);
            DataTable existingData = new DataTable();
            try
            {
                cmd.Connection = Conn;
                SqlDataAdapter adap = new SqlDataAdapter(cmd);
                adap.Fill(existingData);
                if (existingData.Rows.Count > 0)
                {
                    if(!dailyCheck)
                        Helper.log.WriteLogLine(day.ToString("dd-MM-yyyy")+" günün EPIAS Şeffaflık PTF verileri zaten veri tabanında var, tekrar işlem yapılmayacak.", false);
                    result = false;
                }
                else
                {
                    Helper.log.WriteLogLine(day.ToString("dd-MM-yyyy") + " günün EPİAS Şeffaflık PTF verileri yok");
                    result = true;
                }
            }
            catch (Exception)
            {
                Helper.log.WriteLogLine("EPİAS Şeffaflık PTF verilerine (veritabanından) ulaşırken bir sorun yaşandı", false);
            }
            finally
            {
                Conn.Close();
            }
            return result;
        }

        public bool SaveDayAheadMCP(DayAheadMCP input)
        {
            if (Conn.State != ConnectionState.Open)
            {
                Conn = new SqlConnection(connectionString);
                Conn.Open();
            }
            bool result = false;
            bool status = false;
            //string[] dates = new string[input.Body.DayAheadMCPList.Length];
            //for (int i = 0; i < input.Body.DayAheadMCPList.Length; i++)
            //    dates[i] = "\'" + Convert.ToDateTime(input.Body.DayAheadMCPList[i].Date).ToString("yyyy-MM-dd hh:mm:ss") + "\'";
            DateTime check = Convert.ToDateTime(input.Body.DayAheadMCPList[0].Date);
            if (input.Body.DayAheadMCPList.Length > 0)
            {
                //string sql = string.Format("SELECT * FROM tb_EPIAS_PTF_DETAILS WITH(NOLOCK) WHERE DATETIME_ IN ({0}) ORDER BY ID DESC", string.Join(",", dates));
                //SqlCommand cmd = new SqlCommand(sql);
                //DataTable existingData = new DataTable();
                try
                {
                    //cmd.Connection = Conn;
                    //SqlDataAdapter adap = new SqlDataAdapter(cmd);
                    //adap.Fill(existingData);
                    if (!CheckMCPbyDate(check))
                    {
                        status = false;
                        Helper.log.WriteLogLine("EPIAS Şeffaflık PTF verileri zaten veri tabanında var, tekrar işlem yapılmayacak.", false);
                        result = true;
                        //Console.WriteLine(sql+"\n"+existingData.Rows.Count);
                    }
                    else
                    {
                        status = true;
                        Helper.log.WriteLogLine("EPİAS Şeffaflık PTF verileri alındı ve veri tabanına kaydediliyor");
                        //Console.WriteLine("true " + sql + "\n" + existingData.Rows.Count);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Helper.log.WriteLogLine("EPİAS Şeffaflık PTF verilerine (veritabanından) ulaşırken bir sorun yaşandı", false);
                }
                finally
                {
                    Conn.Close();
                }
            }
            else
            {
                Helper.log.WriteLogLine("Sunucuda yarına ait (PTF fiyatları) veri hala yok ", false);
                status = false;
            }
            if (status)
            {
                if (Conn.State != ConnectionState.Open)
                {
                    Conn = new SqlConnection(connectionString);
                    Conn.Open();
                }
                using (SqlConnection oConnection = Conn)
                {
                    using (SqlTransaction oTransaction = oConnection.BeginTransaction())
                    {
                        using (SqlCommand oCommand = oConnection.CreateCommand())
                        {
                            oCommand.Transaction = oTransaction;
                            oCommand.CommandType = CommandType.Text;
                            oCommand.CommandText = "INSERT INTO tb_EPIAS_PTF_DETAILS (DEPARTMENT_ID,DATETIME_,PRICE,CREATED_BY) VALUES (1,@date, @price,@created_by)";
                            oCommand.Parameters.Add(new SqlParameter("@date", SqlDbType.DateTime2));
                            oCommand.Parameters.Add(new SqlParameter("@price", SqlDbType.Decimal));
                            oCommand.Parameters.Add(new SqlParameter("@created_by", SqlDbType.VarChar));
                            try
                            {
                                foreach (var oSetting in input.Body.DayAheadMCPList)
                                {
                                    oCommand.Parameters[0].Value = oSetting.Date;
                                    oCommand.Parameters[1].Value = oSetting.Price;
                                    oCommand.Parameters[2].Value = "AbysisEpiasService";
                                    if (oCommand.ExecuteNonQuery() != 1)
                                    {
                                        Helper.log.WriteLogLine("EPİAS Şeffaflık PTF verileri veritabanına kaydedilemedi", false);
                                        //throw new InvalidProgramException();
                                    }
                                }
                                Helper.log.WriteLogLine("EPİAS Şeffaflık PTF verileri veri tabanına kaydedildi");
                                result = true;
                                oTransaction.Commit();
                            }
                            catch (Exception)
                            {
                                oTransaction.Rollback();
                            }
                            finally
                            {
                                Conn.Close();
                            }
                        }
                    }
                }
            }
            Thread.Sleep(300);
            if (input.Body.DayAheadMCPList.Length > 0)
                return result;
            return false;
        }

        public DataTable GetDayAheadMCP(DateTime day)
        {
            if (Conn.State != ConnectionState.Open)
            {
                Conn = new SqlConnection(connectionString);
                Conn.Open();
            }
            DataTable result = new DataTable();
            DateTime begin = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0);
            DateTime end = new DateTime(day.Year, day.Month, day.Day, 23, 0, 0);

            SqlCommand cmd = new SqlCommand(@"SELECT D.CODE AS [DEPARTMAN],PTF.DATETIME_ AS [TARİH],PTF.PRICE AS[FİYAT] FROM tb_EPIAS_PTF_DETAILS PTF
                                            LEFT OUTER JOIN tb_DEPARTMENTS D
                                            ON PTF.DEPARTMENT_ID = D.ID
                                            WHERE DATETIME_ BETWEEN @begindate AND @enddate");
            try
            {
                cmd.Connection = Conn;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@begindate", begin);
                cmd.Parameters.AddWithValue("@enddate", end);
                SqlDataAdapter adap = new SqlDataAdapter(cmd);
                adap.Fill(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Conn.Close();
            }

            return result;
        }

    }
}
