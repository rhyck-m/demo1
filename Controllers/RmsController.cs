using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WebApiTokenAuth.Models;
using WebApiTokenAuth.Services;


namespace WebApiTokenAuth.Controllers
{
    //[Authorize]
    [RoutePrefix("api/rms")]
    public class RmsController : ApiController
    {
        #region Declarations
        private DataTable g_dtblSearchCriteriaTrail;
        private DataTable g_dtblSheet;
        private const string kEMHT_SVAR_TABLE_SEARCHTRAIL = "Table_SearchCritieriaTrail";
        private const string kEMHT_SVAR_TABLE_SHEET = "Table_Sheet";

        private string rms_ConnectionString = ConfigurationManager.ConnectionStrings["conRMS"].ConnectionString;
        #endregion

        DataService ds = new DataService();

        [HttpGet]
        [Route("initsearchcriteriatrail")]
        public async Task<HttpResponseMessage> InitSearchCriteriaTrail()
        {
            SearchCriteriaTrail_Initialize();
            return Request.CreateResponse(HttpStatusCode.OK, "OK");
        }
        public async Task<HttpResponseMessage> ArchivePlans()
        {
            HttpRequest httpRequest = HttpContext.Current.Request;
            string rootPath = ConfigurationManager.AppSettings["RMS_Plans"];
            string archive_info = httpRequest.Form["archiveInfo"];
            var folder = httpRequest.Form["folder"];
            try
            {
                string _sub_folder = $"{rootPath}/{folder}";
                if (!Directory.Exists(_sub_folder))
                {
                    Directory.CreateDirectory(_sub_folder);
                }
                if (httpRequest.Files.Count > 0)
                {
                    //[1] save construction files
                    for (int i = 0; i < httpRequest.Files.Count; i++)
                    {
                        HttpPostedFile postedFile = httpRequest.Files[i];
                        var tempFile = $"{_sub_folder}/{Path.GetFileName(postedFile.FileName)}";
                        postedFile.SaveAs(tempFile); 
                    }
                    
                    //[2] update the rms database
                    List<rmsPlanObj> plansArray = JsonConvert.DeserializeObject<List<rmsPlanObj>>(archive_info);

                    if (plansArray.Count > 0)
                    {
                        foreach (rmsPlanObj plan in plansArray)
                        {
                            string fileExt = plan.FileName.Split('.').Last();
                            if (fileExt == "pdf") // || fileExt == "jpg" || fileExt == "tif")
                            {
                                //[1] Write the plan detail in DB

                                string sqlStr = "SET IDENTITY_INSERT PlanSets ON " +
                                    "insert into PlanSets(SetUID, PlanName, PlanTitle, PlanYear) " +
                                    "values(@setUID, @planName, @planTitle, @planYear) " +
                                    "SET IDENTITY_INSERT PlanSets OFF " +
                                    "insert into PlanSheets([FileName], FileSource, FileExt, SetUID, SheetType, SheetNumber, SheetStreets, " +
                                    "HasSanitary, HasStorm, HasWater, HasSWMP, HasBridge)" +
                                    "values(@fileName, @fileSource, @fileExt, @setUID, @SheetType, @sheetNumber, @sheetStreets, " +
                                    "@hasSanitary, @hasStorm, @hasWater, @hasSwmp, @hasBridge)";

                                Dictionary<string, string> param = new Dictionary<string, string>();

                                // table PlanSets
                                param.Add("@setUID", plan.SetUID.ToString());
                                param.Add("@planName", plan.PlanName);
                                param.Add("@planTitle", plan.PlanTitle);
                                param.Add("@planYear", plan.PlanYear);

                                // table PlanSheets
                                param.Add("@fileName", plan.FileName.Split('.').First());
                                param.Add("@fileSource", plan.FileSource.Replace(@"\\", @"\"));
                                param.Add("@fileExt", plan.FileName.Split('.').Last());
                                param.Add("@SheetType", plan.SheetType);
                                param.Add("@sheetNumber", plan.SheetNumber);
                                param.Add("@sheetStreets", plan.SheetStreets);
                                param.Add("@hasSanitary", plan.HasSanitary.ToString());
                                param.Add("@hasStorm", plan.HasStorm.ToString());
                                param.Add("@hasWater", plan.HasWater.ToString());
                                param.Add("@hasSwmp", plan.HasSWMP.ToString());
                                param.Add("@hasBridge", plan.HasBridge.ToString());

                                DataTable dt = ds.ExecuteSQL(true, "text", sqlStr, param).Tables["RS"];

                            }
                        }
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, "OK");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, $":( Ooops, something went wrong: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("shareplans")]
        public async Task<HttpResponseMessage> SharePlans()
        {
            HttpRequest httpRequest = HttpContext.Current.Request;
            var data = httpRequest.Form["emailDetailsObj"];

            rmsPlanShareObj _sharedPlansObj = JsonConvert.DeserializeObject<rmsPlanShareObj>(data);

            EmailService email = new EmailService(_sharedPlansObj);

            string email_message = email.sendPlans();

            return Request.CreateResponse(HttpStatusCode.OK, email_message);
        }

        [HttpGet]
        [Route("getDefaultCustomInfo")]
        public async Task<HttpResponseMessage> GetDefaultCustomInfo()
        {
            List<string> listFolder = new List<string>();
            List<sheetTypeObj> listPlanType = new List<sheetTypeObj>();
            string maxSetUID = string.Empty;
            try
            {
                string sqlStr_maxSetUID = "select max(SetUID) +1 as MaxSetUID from PlanSets";
                string sqlStr_listFolder = "select distinct FileSource from PlanSheets order by FileSource asc";
                string sqlStr_listPlanType = "select distinct SheetType, [UID] from _SheetTypes  where SheetType not in('-9') order by SheetType asc";

                DataTable dt_maxSetUID = ds.ExecuteSQL(true, "text", sqlStr_maxSetUID, null).Tables["RS"];
                DataTable dt_listFolder = ds.ExecuteSQL(true, "text", sqlStr_listFolder, null).Tables["RS"];
                DataTable dt_listPlanType = ds.ExecuteSQL(true, "text", sqlStr_listPlanType, null).Tables["RS"];

                maxSetUID = dt_maxSetUID.Rows[0]["MaxSetUID"].ToString();

                for (int i = 0; i < dt_listFolder.Rows.Count; i++)
                {
                    listFolder.Add(dt_listFolder.Rows[i]["FileSource"].ToString());
                }

                for (int i = 0; i < dt_listPlanType.Rows.Count; i++)
                {
                    listPlanType.Add(new sheetTypeObj()
                    {
                        UID = Convert.ToInt32(dt_listPlanType.Rows[i]["UID"].ToString()),
                        SheetType = dt_listPlanType.Rows[i]["SheetType"].ToString()
                    });
                }
                // Provide the add new folder option
                listFolder.Add("Create New Folder");

                var customInfoObj = new
                {
                    maxSetUID = maxSetUID,
                    listFolders = listFolder,
                    listPlansType = listPlanType
                };
                return Request.CreateResponse(HttpStatusCode.OK, customInfoObj);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, ex.Message);
            }
        }

        [HttpGet]
        [Route("loadplansBySet/{fileName}")]
        public async Task<HttpResponseMessage> LoadplansBySet(string fileName)
        {
            string strSQL = "select PD.SetUID, PD.PlanName, PD.PlanTitle, PD.PlanYear, PS.[FileName], PS.[FileExt], PS.FileSource, PS.SheetType as SheetTypeUID," +
                    " (select SheetType from _SheetTypes where [UID] = PS.SheetType) as SheetType, PS.SheetNumber, PS.SheetStreets," +
                    " PS.HasSanitary, PS.HasStorm, PS.HasWater, PS.HasSWMP, PS.HasBridge from PlanSets PD" +
                    " inner join PlanSheets PS on PD.SetUID = PS.SetUID" +
                    $" where PS.SetUID = (select top 1 SetUID from PlanSheets where [FileName] = '{fileName}')" +
                    " order by PD.SetUID asc";

            DataTable dt = ds.ExecuteSQL(true, "text", strSQL, null).Tables["RS"];
            if (dt.Rows.Count > 0)
                return Request.CreateResponse(HttpStatusCode.OK, resultSet(dt));
            else
                return Request.CreateResponse(HttpStatusCode.OK, $"No records found");
        }

        [Route("getsearch/{term}")] // used by Search Plans Components
        public async Task<HttpResponseMessage> GetPlansBySearch(string term)
        {

            string strSQL2 = "select PD.*, PS.* from PlanSets PD inner join PlanSheets PS" +
                               " on PD.SETID = PS.SETID where PS.SETID = (Select top 1 PS.SETID from PlanSets where";

            if (term.IndexOf(' ') > -1)
            {
                string[] terms = term.Split(' ');
                for (int i = 0; i < terms.Length; i++)
                {
                    strSQL2 = strSQL2 + $" PD.DESCRIPTION LIKE '%{terms[i]}%' OR PD.YEAR LIKE '%{terms[i]}%'" +
                              $" OR PS.FILENAME LIKE '%{terms[i]}%' OR PS.SHEETSTREETS LIKE '%{terms[i]}%'";

                    if (i < terms.Length - 1)
                        strSQL2 = strSQL2 + " AND ";
                    else
                        strSQL2 = strSQL2 + ")";
                }

                strSQL2 = strSQL2 + " order by PS.SETID asc";
            }
            else
            {
                strSQL2 = strSQL2 + $" PD.DESCRIPTION LIKE '% {term} %' OR PD.YEAR LIKE '%{term}%'" +
                               $" OR PS.FILENAME LIKE '%{term}%' OR PS.SHEETSTREETS LIKE '%{term}%')" +
                               " order by PS.SETID asc";
            }

            string strSQL = "spGetPlanSearch";
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("@searchTerm", term);
            DataTable dt = ds.ExecuteSQL(true, "text", strSQL2, null).Tables["RS"];
            // DataTable dt = D.ExecuteSQL(true, "sp", strSQL, param).Tables["RS"];
            if (dt.Rows.Count > 0)
                return Request.CreateResponse(HttpStatusCode.OK, resultSet(dt));
            else
                return Request.CreateResponse(HttpStatusCode.OK, $"No records found");
        }

        [HttpPost]
        [Route("getplansByQueryBuilder")]
        public async Task<HttpResponseMessage> GetplansByQueryBuilder()
        {
            HttpRequest httpRequest = HttpContext.Current.Request;
            var data = httpRequest.Form["queryBuilderObj"];

            IEnumerable<queryBuilderObj> advSearchObj = JsonConvert.DeserializeObject<IEnumerable<queryBuilderObj>>(data);
            if (advSearchObj.ToList().Count > 0)
            {
                foreach (queryBuilderObj searchObj in advSearchObj)
                {
                    SearchCriteriaTrail_Append(searchObj.fieldName, searchObj.fieldValue);
                }
                SearchCriteriaTrail_Reload();
                GetSheets();
            }

            var result = new
            {
                g_dtblSheet = resultSet(g_dtblSheet),
                g_dtblSearchCriteriaTrail = HttpContext.Current.Session[kEMHT_SVAR_TABLE_SEARCHTRAIL]
            };
            HttpContext.Current.Session.Remove(kEMHT_SVAR_TABLE_SEARCHTRAIL);
            return Request.CreateResponse(HttpStatusCode.OK, result);
        }

        #region RMS2 Plans Edit

        [HttpPost]
        [Route("saveSheetEdits")]
        public async Task<HttpResponseMessage> SaveSheetInfo()
        {
            HttpRequest httpRequest = HttpContext.Current.Request;
            string sheet_edit_info = httpRequest.Form["sheetEditInfo"];

            saveSheetInfoObj sheetInfo = JsonConvert.DeserializeObject<saveSheetInfoObj>(sheet_edit_info);

            string strSQL = "update PlanSheets set SheetType = @sheetType, SheetStreets = @sheetStreets," +
                " SheetNumber = @sheetNumber, HasSanitary = @hasSanitary, HasStorm = @hasStorm, HasWater = @hasWater, HasSWMP = @hasSWMP, HasBridge = @hasBridge where [FileName] = @fileName";
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("@sheetType", sheetInfo.SheetType);
            param.Add("@sheetStreets", sheetInfo.SheetStreet);
            param.Add("@sheetNumber", sheetInfo.SheetNumber);
            param.Add("@fileName", sheetInfo.FileName);
            param.Add("@hasSanitary", sheetInfo.HasSanitary);
            param.Add("@hasStorm", sheetInfo.HasStorm);
            param.Add("@hasWater", sheetInfo.HasWater);
            param.Add("@hasSWMP", sheetInfo.HasSWMP);
            param.Add("@hasBridge", sheetInfo.HasBridge);


            DataTable dt = ds.ExecuteSQL(true, "text", strSQL, param).Tables["RS"];
            return Request.CreateResponse(HttpStatusCode.OK, true);
        }

        [HttpGet]
        [Route("saveSetTitle/{fileName}/{title}")]
        public async Task<HttpResponseMessage> SaveSetTitle(string fileName, string title)
        {
            string strSQL = "update PlanSets set PlanTitle = @title where SetUID = (select SetUID from PlanSheets where [FileName] = @fileName)";
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("@fileName", fileName);
            param.Add("@title", title);
            DataTable dt = ds.ExecuteSQL(true, "text", strSQL, param).Tables["RS"];
            return Request.CreateResponse(HttpStatusCode.OK, true);
        }

        [HttpGet]
        [Route("deleteSheetEdits/{fileName}")]
        public async Task<HttpResponseMessage> DeleteSheetInfo(string fileName)
        {
            string strSQL = " delete PlanSheets where [FileName] = @fileName";
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("@fileName", fileName);
            DataTable dt = ds.ExecuteSQL(true, "text", strSQL, param).Tables["RS"];
            return Request.CreateResponse(HttpStatusCode.OK, true);
        }

        #endregion

        private static string BuildWhereClause(string sField, string sValue)
        {
            string sRetVal = "";
            string[] aPhrases = sValue.Replace("'", "''").Split(',');
            for (int i = 0; i < aPhrases.Length; i++)
            {
                string[] aWords = aPhrases[i].Split(' ');
                string sWherePart = "";
                for (int j = 0; j < aWords.Length; j++)
                {
                    if (!string.IsNullOrEmpty(aWords[j]))
                    {
                        if (sWherePart != "")
                            sWherePart += " AND ";

                        switch (sField)
                        {

                            default:
                                sWherePart += $"{sField} LIKE '%{aWords[j]}%'";
                                break;
                        }
                    }
                }
                if (sWherePart != "")
                {
                    if (sRetVal != "")
                        sRetVal += " OR ";

                    sRetVal += $"{sWherePart}";
                }
            }

            return sRetVal;
        }
        private static List<rmsPlanObj> resultSet(DataTable dt)
        {
            List<rmsPlanObj> plansList = new List<rmsPlanObj>();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow R in dt.Rows)
                {
                    string RMS_Plan_Link = ConfigurationManager.AppSettings["RMS_Plan_Link"]; //HostingEnvironment.MapPath("~/Plans");
                    string _PlanThumbnails = ConfigurationManager.AppSettings["RMS_Thumbnails"];

                    rmsPlanObj P = new rmsPlanObj()
                    {
                        SetUID = Convert.ToInt32(R["SetUID"]),
                        PlanName = R["PlanName"].ToString(),
                        PlanTitle = R["PlanTitle"].ToString(),
                        PlanYear = R["PlanYear"].ToString(),
                        FileName = R["FileName"].ToString(),
                        FileExt = R["FileExt"].ToString(),
                        FileSource = R["FileSource"].ToString(),
                        SheetTypeUID = (!(R["SheetTypeUID"].Equals(System.DBNull.Value))) ? Convert.ToInt32(R["SheetTypeUID"]) : 0,
                        SheetType = R["SheetType"].ToString(),
                        SheetNumber = R["SheetNumber"].ToString(),
                        SheetStreets = R["SheetStreets"].ToString(),
                        HasSanitary = Convert.ToInt32(R["HasSanitary"]),
                        HasStorm = Convert.ToInt32(R["HasStorm"]),
                        HasWater = Convert.ToInt32(R["HasWater"]),
                        HasSWMP = Convert.ToInt32(R["HasSWMP"]),
                        HasBridge = Convert.ToInt32(R["HasBridge"]),
                        PlanLink = $"{RMS_Plan_Link}{R["FileSource"].ToString().Replace('\\', '/')}/{R["FileName"].ToString()}.{R["FileExt"].ToString()}",
                        PlanThumbnail = $"{_PlanThumbnails}{R["FileSource"].ToString().Replace('\\', '/')}/{R["FileName"].ToString()}.png"
                    };
                    plansList.Add(P);
                }

            }
            return plansList;
        }

        #region SearchCriteria_Trail_Helper
        private void SearchCriteriaTrail_Initialize()
        {
            g_dtblSearchCriteriaTrail = new DataTable();
            g_dtblSearchCriteriaTrail.Columns.Add("ID", Type.GetType("System.Int16"));
            g_dtblSearchCriteriaTrail.Columns.Add("Field", Type.GetType("System.String"));
            g_dtblSearchCriteriaTrail.Columns.Add("Value", Type.GetType("System.String"));
            g_dtblSearchCriteriaTrail.Columns.Add("Deleted", Type.GetType("System.Boolean"));
            g_dtblSearchCriteriaTrail.Columns.Add("Records", Type.GetType("System.Int16"));

            SearchCriteriaTrail_Reload();
        }

        private void SearchCriteriaTrail_Reload()
        { 
            HttpContext.Current.Session[kEMHT_SVAR_TABLE_SEARCHTRAIL] = g_dtblSearchCriteriaTrail;
            DataView dvwSearchCriteria = new DataView(g_dtblSearchCriteriaTrail, "Deleted = false", "ID", DataViewRowState.CurrentRows);
            //DataTable dt = dvwSearchCriteria.ToTable();
           // return dt;
           // gvwSearchCriteriaTrail.DataSource = dvwSearchCriteria;
           // gvwSearchCriteriaTrail.DataBind();
        }

        private void SearchCriteriaTrail_Append(string sField, string sValue)
        {
            if (g_dtblSearchCriteriaTrail == null) g_dtblSearchCriteriaTrail = (DataTable)(HttpContext.Current.Session[kEMHT_SVAR_TABLE_SEARCHTRAIL]);
            if (g_dtblSearchCriteriaTrail == null) SearchCriteriaTrail_Initialize();

            switch (sField)
            {
                case "SheetUID":
                    DataRow[] rowsDelete = g_dtblSearchCriteriaTrail.Select("Field = 'SheetUID' AND Deleted = false");
                    foreach (DataRow rowDelete in rowsDelete)
                    {
                        sValue += "," + rowDelete["Value"];
                        rowDelete["Delete"] = true;
                    }

                    break;
                case "Utility":
                    DataRow[] rowsDeleteT = g_dtblSearchCriteriaTrail.Select("Field = 'Utility'");
                    foreach (DataRow rowDelete in rowsDeleteT)
                    {
                        rowDelete["Deleted"] = true;
                    }
                    break;
                default:
                    break;
            }

            DataRow drow = g_dtblSearchCriteriaTrail.NewRow();
            drow["ID"] = g_dtblSearchCriteriaTrail.Rows.Count;
            drow["Field"] = sField;
            drow["Value"] = sValue;
            drow["Deleted"] = false;
            g_dtblSearchCriteriaTrail.Rows.Add(drow);
        }

        private void SearchCriteriaTrail_Remove(int ID)
        {
            var session = HttpContext.Current.Session;
            if (g_dtblSearchCriteriaTrail == null) g_dtblSearchCriteriaTrail = (DataTable)(session[kEMHT_SVAR_TABLE_SEARCHTRAIL]);

            DataRow[] rowsDelete = g_dtblSearchCriteriaTrail.Select("ID = " + ID);

            if(rowsDelete[0]["Field"].ToString() == "Utility")
            {
                //for (int i = 0; i < cblSearchUtilityType.Items.Count; i++)
                //{

                //}
            }

            rowsDelete[0]["Deleted"] = true;
            SearchCriteriaTrail_Reload();
        }

        private static string DataTableToJSON(DataTable dt)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(dt);
            return JSONString;
        }

        #endregion

        #region Search Records Helpers
        private void GetSheets()
        {
            using (SqlConnection sqlcon = new SqlConnection(rms_ConnectionString))
            {
                g_dtblSheet = new DataTable();
                SqlDataAdapter oSqlDataAdapter = new SqlDataAdapter();
                sqlcon.Open();

                string strSQL = "SELECT PlanSets.SetUID, PlanSets.PlanName, PlanSets.PlanTitle, PlanSets.PlanYear" +
               ", _SheetTypes.UID AS SheetTypeUID, _SheetTypes.SheetType" +
               ", PlanSheets.SheetUID, PlanSheets.FileName, PlanSheets.FileSource, PlanSheets.FileExt" +
               ", PlanSheets.SheetNumber, PlanSheets.SheetStreets" +
               ", PlanSheets.HasSanitary, PlanSheets.HasStorm, PlanSheets.HasWater, PlanSheets.HasSWMP" +
               ", PlanSheets.HasBridge FROM PlanSets INNER JOIN PlanSheets ON PlanSets.SetUID = PlanSheets.SetUID" +
               " LEFT OUTER JOIN _SheetTypes ON PlanSheets.SheetType = _SheetTypes.UID";

                DataView dvwSheet;

                if (g_dtblSearchCriteriaTrail == null) g_dtblSearchCriteriaTrail = (DataTable)(HttpContext.Current.Session[kEMHT_SVAR_TABLE_SEARCHTRAIL]);

                DataRow[] rowsSearchCriteriaTrail = g_dtblSearchCriteriaTrail.Select("Deleted = false", "ID");
                if (rowsSearchCriteriaTrail.Length == 0)
                {
                    //if (_isAll)
                    //{
                    //    strSQL += " WHERE (PlanSheets.SheetUID IS NOT NULL)";
                    //    dvwSheet = new DataView(g_dtblSheet, "", "", DataViewRowState.CurrentRows);
                    //    oSqlDataAdapter.SelectCommand = new SqlCommand(strSQL, sqlcon);
                    //    oSqlDataAdapter.Fill(g_dtblSheet);
                    //    dvwSheet = new DataView(g_dtblSheet, "", "SheetUID", DataViewRowState.CurrentRows);
                    //}
                    //else
                    //{
                    //    strSQL += " WHERE (PlanSheets.SheetUID IS NULL)";
                    //    dvwSheet = new DataView(g_dtblSheet, "", "", DataViewRowState.CurrentRows);
                    //}

                }
                else
                {
                    DataRow drowSearchCriteriaTrail = rowsSearchCriteriaTrail[0];
                    strSQL += " WHERE (" + BuildWhereClause02(drowSearchCriteriaTrail["Field"].ToString().Trim(), drowSearchCriteriaTrail["Value"].ToString().Trim(), false) + ")";
                    oSqlDataAdapter.SelectCommand = new SqlCommand(strSQL, sqlcon);
                    oSqlDataAdapter.Fill(g_dtblSheet);
                    drowSearchCriteriaTrail["Records"] = g_dtblSheet.Rows.Count;

                    for (int i = 0; i < rowsSearchCriteriaTrail.Length; i++)
                    {
                        drowSearchCriteriaTrail = rowsSearchCriteriaTrail[i];
                        DataRow[] rowsRefinedSheets = g_dtblSheet.Select(BuildWhereClause02(drowSearchCriteriaTrail["Field"].ToString().Trim(), drowSearchCriteriaTrail["Value"].ToString().Trim(), true));

                        if (rowsRefinedSheets.Length == 0)
                            g_dtblSheet.Rows.Clear();
                        else
                            g_dtblSheet = rowsRefinedSheets.CopyToDataTable();

                        drowSearchCriteriaTrail["Records"] = g_dtblSheet.Rows.Count;
                    }
                    SearchCriteriaTrail_Reload();
                    dvwSheet = new DataView(g_dtblSheet, "", "SheetUID", DataViewRowState.CurrentRows);
                }

                HttpContext.Current.Session[kEMHT_SVAR_TABLE_SHEET] = g_dtblSheet;

            }
        }

        private string BuildWhereClause02(string sField, string sValue, bool bConvertToString)
        {
            string sRetVal = "";
            if (!string.IsNullOrEmpty(sValue))
            {
                string sWherePart = "";
                switch (sField)
                {
                    case "Utility":
                        string[] aUtility = sValue.Split(',');
                        
                        for (int i = 0; i < aUtility.Length; i++)
                        {
                            if (sWherePart != "") sWherePart += " OR";
                            if (bConvertToString) sWherePart += "((CONVERT(" + aUtility[i] + ", 'System.String')) = '1')";
                            else
                                sWherePart += "(" + aUtility[i] + " = '1')";
                        }
                        sRetVal = "(" + sWherePart + ")";
                        break;
                    default:
                        string[] aPhrases = sValue.Replace("'", "''").Split(',');
                        for (int i = 0; i < aPhrases.Length; i++)
                        {
                            string[] aWords = aPhrases[i].Split(' ');
                            sWherePart = "";

                            for (int j = 0; j < aWords.Length; j++)
                            {
                                if(aWords[j] != "")
                                    if (sWherePart != "") sWherePart += " AND ";

                                switch (sField)
                                {
                                    case "SheetUID":
                                        if (bConvertToString)
                                            sWherePart += "((CONVERT(" + sField + ", 'System.String')) = '" + aWords[j] + "')";
                                        else
                                            sWherePart += "(" + sField + " = '" + aWords[j] + "')";
                                        break;
                                    default:
                                        if (bConvertToString)
                                            sWherePart += "((CONVERT(" + sField + ", 'System.String')) LIKE '%" + aWords[j] + "%')";
                                        else
                                            sWherePart += "(" + sField + " LIKE '%" + aWords[j] + "%')";
                                        break;
                                }

                            }
                            if(sWherePart != "")
                            {
                                if (sRetVal != "") sRetVal += " OR ";
                                sRetVal += "(" + sWherePart + ")";
                            }

                        }
                        break;
                }
            }
            return sRetVal;
        }

        #endregion
    }
}
