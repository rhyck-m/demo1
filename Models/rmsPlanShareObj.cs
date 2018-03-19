using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApiTokenAuth.Models
{
    public class rmsPlanShareObj
    {
        public string emailSubject { get; set; }
        public string emailFrom { get; set; }
        public string emailTo { get; set; }
        public string message { get; set; }
        public List<rmsPlanObj> plansArray { get; set; }
    }
}