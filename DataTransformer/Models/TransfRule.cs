using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DataTransformer.Models
{
    public class TransfRule
    {
        public string IfCol = "";
        public LogicOp IfOp = LogicOp.Contains;
        public string IfValue = "";
        public string TgtCol = "";
        public string TransfTo = "";

        public bool HasEmbdCols = false;
        public List<EmbeddedColumn> EmbdCols;
    }

    public class EmbeddedColumn
    {
        public string EmbdColName = "";
        public string ColumnName = "";
    }
}