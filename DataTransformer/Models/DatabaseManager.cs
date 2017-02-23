using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace DataTransformer.Models
{
    public class DatabaseManager
    {
        public string dbConnStr = "";

        public DatabaseManager(string dbConnStr)
        {
            this.dbConnStr = dbConnStr;
        }

        public DataTable GetRecords(string query)
        {
            return GetRecords(query, this.dbConnStr);
        }
        public DataTable GetRecords(string query, string dbConnStr)
        {
            DataTable tbl = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(query, dbConnStr))
            {
                da.Fill(tbl);
            }
            return tbl;
        }

        public int UpdateRecords(string query)
        {
            return UpdateRecords(query, this.dbConnStr);
        }
        public int UpdateRecords(string query, string dbConnStr)
        {
            int recordsAffected = 0;

            using (SqlConnection conn = new SqlConnection(dbConnStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                recordsAffected = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return recordsAffected;
        }
    }
}