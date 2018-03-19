using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApiTokenAuth.Models
{
    public class rmsPlanUploadObj
    {
        public string planTitle { get; set; }
        public string planFolder { get; set; }
        public string planYear { get; set; }
        public string setUID { get; set; }
        public string planCustomName { get; set; }
        public string planName { get; set; }
        public string sheetType { get; set; }
        public string sheetNumber { get; set; }
        public string sheetStreets { get; set; }
        public string hasSanitary { get; set; }
        public string hasStorm { get; set; }
        public string hasWater { get; set; }
        public string hasSTR { get; set; }
        public string hasSITE { get; set; }
    }
}