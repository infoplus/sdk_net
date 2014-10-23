using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class CodeList
    {

        public CodeList()
        {
            this.Items = new List<CodeItem>();
        }

        // public string ListId { get; set; }
        public string Name { get; set; }
        public CodeTypes CodeType { get; set; }
        public IList<CodeItem> Items { get; set; }
        public DateTime Expire { get; set; }
    }

    public enum CodeTypes
    {
        Internal,
        External
    }
}
