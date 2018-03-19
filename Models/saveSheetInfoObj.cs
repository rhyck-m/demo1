using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApiTokenAuth.Models
{
    public class saveSheetInfoObj
    {
        public string FileName { get; set; }
        public string SheetType { get; set; }
        public string SheetStreet { get; set; }
        public string SheetNumber { get; set; }
        public string HasSanitary { get; set; }
        public string HasStorm { get; set; }
        public string HasWater { get; set; }
        public string HasSWMP { get; set; }
        public string HasBridge { get; set; }
    }
}