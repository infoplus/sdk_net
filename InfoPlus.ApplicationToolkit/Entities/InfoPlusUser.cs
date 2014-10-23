using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for User
/// </summary>
namespace InfoPlus.ApplicationToolkit.Entities
{
    public class InfoPlusUser
    {
        public string Account { get; set; }			// 唯一标识
        public string UserCode { get; set; }		// 工号
        public string TrueName { get; set; }		// 姓名
        public string OrganizeId { get; set; }		// 组织机构Id，可空
        public string OrganizeName { get; set; }	// 组织机构名称，可空
    }
}