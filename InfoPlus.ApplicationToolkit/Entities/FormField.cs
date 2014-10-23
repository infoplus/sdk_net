using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoPlus.ApplicationToolkit.Entities
{
    /// <summary>
    /// Summary description for FormField
    /// </summary>
    public class FormField
    {
        public static readonly string TYPE_CODE = "Code";
        public static readonly string TYPE_USER = "User";
        public static readonly string TYPE_ORGANIZE = "Organize";

        public string Name { get; set; }		// 字段名，在表单模板中唯一，表单模板中的字段标识
        public string Type { get; set; }		// 字段描述
        public string GroupName { get; set; }	// 所属分组，在表单模板中唯一
        public int RepeatDepth { get; set; }	// 嵌套深度，0表示最外层

        public string[] GroupObjectNames
        {
            get
            {
                if (this.GroupName == null) return new string[] { };
                string[] splits = this.GroupName.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length <= 1) return new string[] { };
                IList<string> nameList = new List<string>();

                for (int i = 1; i < splits.Length; i++)
                {
                    string name = splits[i];
                    if (name.StartsWith("group"))
                    {
                        name = name.Substring(5);
                    }
                    if (name.Length > 0)
                    {
                        name = name.Substring(0, 1).ToUpper() + name.Substring(1);
                    }
                    nameList.Add(name);
                }
                return nameList.ToArray();
            }
        }

    }
}