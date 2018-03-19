using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApiTokenAuth.Models
{
    public class rmsPlanObj
    {
        public int SetUID { get; set; }
        public string PlanName { get; set; }
        public string PlanTitle { get; set; }
        public string PlanYear { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string FileSource { get; set; }
        public string SheetType { get; set; }
        public int SheetTypeUID { get; set; }
        public string SheetNumber { get; set; }
        public string SheetStreets { get; set; }
        public int HasSanitary { get; set; }
        public int HasStorm { get; set; }
        public int HasWater { get; set; }
        public int HasSWMP { get; set; }
        public int HasBridge { get; set; }
        public string PlanLink { get; set; }
        public string PlanThumbnail { get; set; }
    }
}