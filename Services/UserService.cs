using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using WebApiTokenAuth.Models;

namespace WebApiTokenAuth.Services
{
    public class UserService
    {
        DataService ds = new DataService();

        public bool checkAuthorization(string email, string pw)
        {
            DataTable dt = checkUser(email, pw);
            if (dt.Rows.Count > 0)
                return true;
            else
                return false;
        }

        public RmsUser GetUserByCredentials(string email, string pw)
        {
            DataTable dt = checkUser(email, pw);
            if (dt.Rows.Count > 0)
            {
                RmsUser _user = new RmsUser()
                {
                    UFName = dt.Rows[0]["UFName"].ToString(),
                    ULName = dt.Rows[0]["ULName"].ToString(),
                    Email = dt.Rows[0]["Email"].ToString(),
                    AccessLevel = Convert.ToInt32(dt.Rows[0]["AccessLevel"])
                };
                return _user;
            }
            else
                return null;
        }

        private static DataTable checkUser(string email, string pw)
        {
            DataService DS = new DataService();
            string strSQL = "select UFName, ULName, Email, AccessLevel from Users where Email=@email and Password=@pw";
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("@email", email);
            param.Add("@pw", GenerateSHA256String(pw));
            DataTable dt = DS.ExecuteSQL(false, "text", strSQL, param).Tables["RS"];

            return dt;
        }
        private static string GenerateSHA256String(string inputString)
        {
            SHA256 sha256 = SHA256Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            byte[] hash = sha256.ComputeHash(bytes);
            return GetStringFromHash(hash);
        }

        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString().ToLower();
        }


    }
}