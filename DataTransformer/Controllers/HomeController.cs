using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web.Configuration;
using System.Reflection;
using System.Web.Hosting;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Text;

// Download DLL from http://epplus.codeplex.com/releases/view/625020    (it can read & create *.xlsx only)
using OfficeOpenXml;

// Download DLL from http://exceldatareader.codeplex.com/   (it can read *.xls, *.xlsx)
using Excel;

using DataTransformer.Models;

namespace DataTransformer.Controllers
{
    [MyCustomGlobalErrHandler]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            string transformedFilePath = "/tmp/ProductsSample_" + nowStr + ".xlsx";

            ViewBag.TempDownloadFilePath = transformedFilePath;
            ViewBag.TempDownloadFileName = Path.GetFileName(transformedFilePath);
            ViewBag.TempDownloadFilePathHome = "/homefiles/ProductsSample_12345.xlsx";
            ViewBag.SessionId = Request["SessionId"] != null ? Request["SessionId"] : "0";
            
            return View();
        }

        [HttpPost]
        public ActionResult Upload()
        {
            if (Request.Files.Count == 0) { return RedirectToAction("Index"); }
            if (Request.Files[0].ContentLength == 0) { return RedirectToAction("Index"); }

            DataTable tblItems = new DataTable("Invalid File Content");
            HttpPostedFileBase file = Request.Files[0];

            if (file != null && file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);

                if (fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                {
                    tblItems = ReadXlsFile(file);
                }
                else // read *.xlsx
                {
                    tblItems = ReadXlsxFile(file);
                }

                MarkExtraRowsOnTable(tblItems, int.Parse(ConfigurationManager.AppSettings["MaxUserDataItems"]));

                StringBuilder sbTabDelimitedLines = ParseDataTableToString(tblItems);
                
                tblItems.TableName = fileName;
                TempData["tblItems"] = tblItems;

                string userIp = Request.UserHostAddress;
                string query =  "SET NOCOUNT ON;" +
                                "INSERT INTO Sessions(UserIp, Filename) " +
                                "VALUES('" + userIp + "','" + fileName + "'); " +
                                "select scope_identity() as lastUid;";
                string dbConnStr = WebConfigurationManager.ConnectionStrings["DataTransfDB"].ConnectionString;
                DatabaseManager custDbMgr = new DatabaseManager(dbConnStr);
                DataTable tblNewSession = custDbMgr.GetRecords(query);
                TempData["SessionId"] = tblNewSession.Rows[0]["lastUid"].ToString();

                string filePath = Path.Combine(Server.MapPath("~/userfiles/"), "tmp" + TempData["SessionId"].ToString() + ".txt");
                StreamWriter sw = new StreamWriter(filePath);
                sw.Write(sbTabDelimitedLines.ToString());
                sw.Close();

                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                filePath = "/tmp/" + fileNameWithoutExtension + "_Transformed" +TempData["SessionId"].ToString() + ".xlsx";
                TempData["DownloadFilePath"] = filePath;
                TempData["DownloadFileName"] = Path.GetFileName(filePath);

                return RedirectToAction("Index2");
            }

            return RedirectToAction("Index"); // back to home page
        }

        private void MarkExtraRowsOnTable(DataTable tbl, int maxRows)
        {
            if (tbl.Rows.Count > maxRows) { tbl.Rows[maxRows].Delete(); }
        }

        public ActionResult Index2()
        {
            if (TempData.Count == 0) { return RedirectToAction("Index"); }

            ViewBag.SessionId = TempData["SessionId"].ToString();
            ViewBag.DownloadFilePath = TempData["DownloadFilePath"].ToString();
            ViewBag.DownloadFileName = TempData["DownloadFileName"].ToString();
            DataTable tblItems = (DataTable)TempData["tblItems"];

            // put column names in <option> tags
            StringBuilder sbCols = new StringBuilder();
            foreach (DataColumn col in tblItems.Columns)
            {
                sbCols.AppendFormat("<option value=\"{0}\">{1}</option>", col.ColumnName, col.ColumnName);
            }
            ViewBag.ColsOpts = "'" + sbCols.ToString() + "'";

            // put logic ops in <option> tags
            StringBuilder sbOps = new StringBuilder();
            foreach (LogicOp op in Enum.GetValues(typeof(LogicOp)).Cast<LogicOp>())
            {
                sbOps.AppendFormat("<option value=\"{0}\">{1}</option>", (int)op, op.ToString().Replace('_', ' '));
            }
            ViewBag.OpsOpts = "'" + sbOps.ToString() + "'";

            return View(tblItems);
        }

        private DataTable ReadXlsxFile(HttpPostedFileBase httpPostedFile)
        {
            DataTable tblItems;

            using (IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(httpPostedFile.InputStream))
            {
                // DataSet - The result of each spreadsheet will be created in the result.Tables
                excelReader.IsFirstRowAsColumnNames = true;
                DataSet result = excelReader.AsDataSet();
                tblItems = result.Tables[0];
            }

            return tblItems;
        }

        private DataTable ReadXlsFile(HttpPostedFileBase httpPostedFile)
        {
            DataTable tblItems;
            
            using (IExcelDataReader excelReader = ExcelReaderFactory.CreateBinaryReader(httpPostedFile.InputStream))
            {
                // DataSet - The result of each spreadsheet will be created in the result.Tables
                excelReader.IsFirstRowAsColumnNames = true;
                DataSet result = excelReader.AsDataSet();
                tblItems = result.Tables[0];
            }

            return tblItems;
        }

        private StringBuilder ParseDataTableToString(DataTable tblItems)
        {
            StringBuilder sbUserData = new StringBuilder();

            int noOfCols = tblItems.Columns.Count;

            // read column headers
            for (int c = 1; c <= noOfCols; c++)
            {
                DataColumn column = tblItems.Columns[c - 1];
                sbUserData.Append(column.ColumnName + (c == noOfCols ? Environment.NewLine : "\t"));
            }

            // read data rows
            foreach (DataRow row in tblItems.Rows)
            {
                if (row.RowState == DataRowState.Deleted) { break; }

                for (int c = 1; c <= noOfCols; c++)
                {
                    sbUserData.Append(row[c - 1].ToString() + (c == noOfCols ? Environment.NewLine : "\t"));
                }
            }

            return sbUserData;
        }

        private StringBuilder ReadXlsxOnlyFileToDataTable(HttpPostedFileBase httpPostedFile, DataTable tblItems)
        {
            StringBuilder sbUserData = new StringBuilder();

            using (var package = new ExcelPackage(httpPostedFile.InputStream))
            {
                var currentSheet = package.Workbook.Worksheets;

                var workSheet = currentSheet.First();
                var noOfCols = workSheet.Dimension.End.Column;
                var noOfRows = workSheet.Dimension.End.Row > 101 ? 101 : workSheet.Dimension.End.Row;
                for (int row = 1; row <= noOfRows; row++)
                {
                    if (row == 1)
                    {
                        for (int col = 1; col <= noOfCols; col++)
                        {
                            DataColumn column = new DataColumn();
                            column.ColumnName = workSheet.Cells[row, col].Value.ToString();
                            tblItems.Columns.Add(column);

                            sbUserData.Append(column + (col == noOfCols ? Environment.NewLine : "\t"));
                        }
                        continue;
                    }

                    DataRow newRow = tblItems.NewRow();
                    for (int col = 1; col <= noOfCols; col++)
                    {
                        newRow[tblItems.Columns[col - 1].ColumnName] = workSheet.Cells[row, col].Value.ToString();

                        sbUserData.Append(workSheet.Cells[row, col].Value.ToString() + (col == noOfCols ? Environment.NewLine : "\t"));
                    }
                    tblItems.Rows.Add(newRow);
                }
            }

            return sbUserData;
        }
    }
}
