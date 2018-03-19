using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApiTokenAuth.Models
{
    public class RmsUser
    {
        public string UFName { get; set; }
        public string ULName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int AccessLevel { get; set; }
    }
}