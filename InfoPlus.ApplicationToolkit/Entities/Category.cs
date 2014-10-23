using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Category
/// </summary>
namespace InfoPlus.ApplicationToolkit.Entities
{
    public class Category
    {
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public bool PrePull { get; set; }
        public bool ShowCount { get; set; }
        public bool Summary { get; set; }
        public IList<Category> SubCategories = new List<Category>();
        public IList<string> Operations = new List<string>();
    }
}