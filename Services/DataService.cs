using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace WebApiTokenAuth.Services
{
    public class DataService
    {
        private string src_ConnectionString = ConfigurationManager.ConnectionStrings["conSRC"].ConnectionString;
        private string rms_ConnectionString = ConfigurationManager.ConnectionStrings["conRMS"].ConnectionString;

        public DataSet ExecuteSQL(bool isRMS, string strCmdType, string strSQL, Dictionary<string, string> param)
        {
            string m_ConnectionString = isRMS ? rms_ConnectionString : src_ConnectionString;
            DataSet ds = new DataSet();
            using (SqlConnection sqlcon = new SqlConnection(m_ConnectionString))
            {
                sqlcon.Open();
                SqlCommand cmd = new SqlCommand(strSQL, sqlcon);
                if (strCmdType == "sp")
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                if (param != null)
                {
                    int count = param.Count;
                    if (count > 0)
                    {
                        foreach (KeyValuePair<string, string> par in param)  // par for parameter
                        {
                            cmd.Parameters.Add(new SqlParameter(par.Key, par.Value));
                        }
                    }
                }
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds, "RS");
            }
            return ds;
        }
    }
}