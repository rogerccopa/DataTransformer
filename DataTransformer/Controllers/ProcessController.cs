using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

// Download DLL from http://epplus.codeplex.com/releases/view/625020
using OfficeOpenXml;

using DataTransformer.Models;
using System.Text.RegularExpressions;

namespace DataTransformer.Controllers
{
    [MyCustomGlobalErrHandler]
    public class ProcessController : Controller
    {
        public string ApplyRules()
        {
            List<TransfRule> rules = new List<TransfRule>();

            for (int i=0; i<100; i++)
            {
                string colSource = Request.Form["colSource" + i];
                string op = Request.Form["op" + i];
                string txtIfValue = Request.Form["txtIfValue" + i];
                string colTarget = Request.Form["colTarget" + i];
                string txtTransfTo = Request.Form["txtTransfTo" + i];

                if (string.IsNullOrEmpty(txtIfValue) || string.IsNullOrEmpty(txtTransfTo)) { break; }

                List<EmbeddedColumn> embeddedColumns = new List<EmbeddedColumn>();
                MatchCollection matches = Constants.rgxEmbeddedColName.Matches(txtTransfTo);
                foreach (Match match in matches)
                {
                    string colName = match.Value.Substring(1, match.Value.Length - 2).ToLower();
                    embeddedColumns.Add(new EmbeddedColumn() { EmbdColName = match.Value, ColumnName = colName });
                }

                rules.Add(new TransfRule()
                {
                    IfCol = colSource,
                    IfOp = (LogicOp)int.Parse(op),
                    IfValue = txtIfValue,
                    TgtCol = colTarget,
                    TransfTo = txtTransfTo,
                    EmbdCols = embeddedColumns
                });
            }

            string jsonRules = JsonConvert.SerializeObject(rules);
            string sessionId = Request.Form["sessionId"];
            string dbConnStr = WebConfigurationManager.ConnectionStrings["DataTransfDB"].ConnectionString;
            string filePath = Path.Combine(Server.MapPath("~/userfiles/"), "tmp" + sessionId + ".txt");

            if (sessionId.Equals("0")) // call from home page
            {
                filePath = Path.Combine(Server.MapPath("~/homefiles/"), "ProductsSample.txt");
            }
            else
            {
                string query = "INSERT INTO TransfRules(SessionId, JsonRules) VALUES('" + sessionId + "','" + jsonRules + "')";
                DatabaseManager custDbMgr = new DatabaseManager(dbConnStr);
                custDbMgr.UpdateRecords(query);
            }
            
            DataTable tblItems = new DataTable();
            List<string> lines = System.IO.File.ReadAllLines(filePath).ToList();
            string[] cols = lines[0].Split('\t');

            foreach (string col in cols)
            {
                tblItems.Columns.Add(new DataColumn(col));
            }

            for (int i = 1; i < lines.Count; i++)
            {
                tblItems.Rows.Add(lines[i].Split('\t'));
            }

            // validate embedded columns against actual data table columns
            foreach (TransfRule rule in rules)
            {
                for (int i = 0; i < rule.EmbdCols.Count; i++)
                {
                    EmbeddedColumn embeddedColumn = rule.EmbdCols[i];

                    if (!tblItems.Columns.Contains(embeddedColumn.ColumnName))
                    {
                        rule.EmbdCols.RemoveAt(i);
                        i--;
                    }
                }

                if (rule.EmbdCols.Count > 0) { rule.HasEmbdCols = true; }
            }

            DataConverter dataConverter = new DataConverter();
            
            try
            {
                dataConverter.ConvertData(tblItems, rules);
            }
            catch(Exception ex)
            {
                string errorMsg = ex.Message.Length > 255 ? ex.Message.Substring(0, 245) : ex.Message;

                // log exception in db
                string query = "INSERT INTO ErrorLogs(SessionId, ErrorMsg) VALUES('" + sessionId + "','" + errorMsg.Replace("'","''") + "')";
                DatabaseManager custDbMgr = new DatabaseManager(dbConnStr);
                custDbMgr.UpdateRecords(query);
                throw;
            }

            string fileName = Request.Form["tmpFN"];
            string excelFilePath = Path.Combine(Server.MapPath("~/tmp/"), fileName);
            SaveDataTableToExcelFile(tblItems, excelFilePath);

            // convert tbl to json string
            string dataTransformed = ConvertDataTableToJSONString(tblItems);

            return dataTransformed;
        }

        private void SaveDataTableToExcelFile(DataTable tbl, string excelFilePath)
        {
            System.IO.File.Delete(excelFilePath);

            using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                // create sheet
                string sheetName = "Sheet1";
                xlPackage.Workbook.Worksheets.Add(sheetName);
                ExcelWorksheet xlWorkSheet = xlPackage.Workbook.Worksheets[1];
                xlWorkSheet.Name = sheetName;

                int r = 1;
                
                // put column headers
                for (int c = 1; c <= tbl.Columns.Count; c++)
                {
                    DataColumn column = tbl.Columns[c - 1];
                    xlWorkSheet.Cells[r, c].Value = column.ColumnName;
                }

                // put data rows
                for (r = 2; r <= (tbl.Rows.Count+1); r++)
                {
                    DataRow row = tbl.Rows[r - 2];

                    for (int c = 1; c <= tbl.Columns.Count; c++)
                    {
                        xlWorkSheet.Cells[r, c].Value = row[c - 1].ToString();
                    }
                }

                xlPackage.Save();
            }
        }

        private string ConvertDataTableToJSONString(DataTable tbl)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append("{");
            sb.Append("\"c0\":\"#\"");

            for (int c = 1; c <= tbl.Columns.Count; c++)
            {
                DataColumn column = tbl.Columns[c - 1];
                sb.Append(",\"c" + c + "\":\"" + column.ColumnName + "\"");
            }
            sb.Append("}");

            for (int r = 1; r <= tbl.Rows.Count; r++)
            {
                DataRow row = tbl.Rows[r - 1];
                sb.Append(",{");
                sb.Append("\"c0\":\"" + r + "\"");

                for (int c = 1; c <= tbl.Columns.Count; c++)
                {
                    DataColumn column = tbl.Columns[c - 1];

                    sb.Append(",\"c" + c + "\":\"" + row[column.ColumnName].ToString().Replace("\"", "\\\"") + "\"");
                }

                sb.Append("}");
            }

            sb.Append("]");
            // JsonConvert.SerializeObject(tblItems);

            return sb.ToString();
        }

        public string DownloadDataTransformed()
        {
            /*
            using (var package = new ExcelPackage(existingFile))
            {
                // edit cells and add details into the excel file...

                //Here u save the file, i would like this to be a stream
                using (FileStream aFile = new FileStream(MapPath("../../template/LeadsExport.xlsx"), FileMode.Create))
                {
                    try
                    {
                        package.SaveAs(aFile);
                        aFile.Close();
                    }
                    catch (Exception ex)
                    {
                    }
                }

                Response.Clear();
                Response.AddHeader("content-disposition", "attachment; filename=LeadsExport.xlsx");
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.BinaryWrite(package.GetAsByteArray());
                Response.End();
            }
            */
            return "ToDo";
        }
        
    }
}
