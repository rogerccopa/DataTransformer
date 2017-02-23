using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace DataTransformer.Models
{
    public static class Constants
    {
        /// <summary>
        /// starts with "[", any character or space, ends with "]"
        /// </summary>
        public static Regex rgxEmbeddedColName = new Regex(@"\[([a-z]|[A-Z])[\w|\s|.]*\]");
        /// <summary>
        /// starts with "(", any number, any mathOp or space, any number, ends with ")"
        /// </summary>
        public static Regex rgxEmbeddedMath = new Regex(@"\(([-+]?[0-9]*\.?[0-9]+)([\s]|[\/\+\-\*])+([-+]?[0-9]*\.?[0-9]+)\)");
        
        // The following is just in case we need to support (N mathOp N mathOp N)
        //Regex myRegex = new Regex(@"\(([-+]?[0-9]*\.?[0-9]+)([\s]|[\/\+\-\*])+([-+]?[0-9]*\.?[0-9]+)(([\s]|[\/\+\-\*])+([-+]?[0-9]*\.?[0-9]+))?\)");

    }
}