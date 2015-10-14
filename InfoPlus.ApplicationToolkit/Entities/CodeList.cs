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

        /// <summary>
        /// the method MUST be readonly, don't change this.Items
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual CodeList Filter(string filter)
        {
            var filtered = from c in this.Items
                           // Notice: NULL is considered "the same as" string.Empty
                           where c.ParentId == filter || string.IsNullOrEmpty(filter + c.ParentId)
                           select c;
            return new CodeList { Items = filtered.ToList(), Name = this.Name };
        }

    }

    public enum CodeTypes
    {
        Internal,
        External
    }
}
