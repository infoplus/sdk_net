using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Studio.Foundation.Json.Core.Conversion;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class InfoPlusFile
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
        public string Kind { get; set; }
        public long Size { get; set; }
        public string Mime { get; set; }

        /// <summary>
        /// Parse json object.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static InfoPlusFile Convert(string entity)
        {
            return JsonConvert.Import<InfoPlusFile>(entity);
        }
    }
}
