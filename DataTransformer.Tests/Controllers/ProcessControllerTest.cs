using System;
using System.Collections.Generic;
using System.Data;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using DataTransformer.Controllers;
using DataTransformer.Models;

namespace DataTransformer.Tests.Controllers
{
    [TestClass]
    public class ProcessControllerTest
    {
        [TestMethod]
        public void ConvertDataTest()
        {
            // Arrange
            DataTable tbl = GetTestDataTable();
            List<TransfRule> rules = GetTestRules();
            DataConverter dataConverter = new DataConverter();

            // Act
            dataConverter.ConvertData(tbl, rules);

            // Assert
            Assert.AreEqual("Four hundred and twenty - 420", 
                            tbl.Rows[1]["product name"].ToString());
            Assert.AreEqual("216", 
                            tbl.Rows[2]["quantity"].ToString());
            Assert.AreEqual("REPLACE(A product so beautiful and comfortable. It is a king size bed, product, MyPRODUCT)",
                            tbl.Rows[0]["product name"].ToString());
        }

        private List<TransfRule> GetTestRules()
        {
            List<TransfRule> rules = new List<TransfRule>();
            rules.Add(new TransfRule()
            {
                HasEmbdCols = true,
                EmbdCols = new List<EmbeddedColumn>() {
                                        new EmbeddedColumn() {  EmbdColName = "[quantity]",
                                                                ColumnName = "quantity"} },
                IfCol = "quantity",
                IfOp = LogicOp.Is,
                IfValue = "420",
                TgtCol = "product name",
                TransfTo = "Four hundred and twenty - [quantity]"
            });
            rules.Add(new TransfRule()
            {
                HasEmbdCols = true,
                EmbdCols = new List<EmbeddedColumn>() {
                                        new EmbeddedColumn() {  EmbdColName = "[quantity]",
                                                                ColumnName = "quantity"} },
                IfCol = "Product Price",
                IfOp = LogicOp.Is_Greater_Than,
                IfValue = "1000",
                TgtCol = "quantity",
                TransfTo = "([quantity] + ([quantity]/2))"
            });
            rules.Add(new TransfRule()
            {
                HasEmbdCols = true,
                EmbdCols = new List<EmbeddedColumn>() {
                                        new EmbeddedColumn() {  EmbdColName = "[product name]",
                                                                ColumnName = "product name"} },
                IfCol = "PRODUCT NAME",
                IfOp = LogicOp.Contains,
                IfValue = "product",
                TgtCol = "product name",
                TransfTo = "REPLACE([product name], product, MyPRODUCT)"
            });

            return rules;
        }

        private DataTable GetTestDataTable()
        {
            DataTable tbl = new DataTable();
            tbl.Columns.Add(new DataColumn("Code"));
            tbl.Columns.Add(new DataColumn("Product Name"));
            tbl.Columns.Add(new DataColumn("Product price"));
            tbl.Columns.Add(new DataColumn("QUANTITY"));
            tbl.Columns.Add(new DataColumn("Date In Stock"));

            DataRow row = tbl.NewRow();
            row["code"] = "ABC123";
            row["product name"] = "A product so beautiful and comfortable. It is a king size bed";
            row["product price"] = "7.75";
            row["quantity"] = "53";
            row["Date In Stock"] = "2016-06-30 14:35:15";
            tbl.Rows.Add(row);

            row = tbl.NewRow();
            row["code"] = "DEF456";
            row["product name"] = "Beautiful and comfortable king bed updated";
            row["product price"] = "97.15";
            row["quantity"] = "420";
            row["Date In Stock"] = "2017-01-02 12:45:25";
            tbl.Rows.Add(row);

            row = tbl.NewRow();
            row["code"] = "GHI789";
            row["product name"] = "This bag features a grey and white stripe print, teal trim, two buckles, studded details, " +
                                  "an outer zipper \"pocket\". And, an inner zipper pocket.";
            row["product price"] = "1975.50";
            row["quantity"] = "144";
            row["Date In Stock"] = "2016-11-20 15:35:10";
            tbl.Rows.Add(row);

            return tbl;
        }
    }
}
