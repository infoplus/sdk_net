using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class CodeSuggestion
    {
        public string Prefix { get; set; }

        public string Parent { get; set; }
        public bool TopLevel { get; set; }

        /// <summary>
        /// Code TableName
        /// </summary>
        public string Code { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public bool Dirty { get; set; }
    }
}
