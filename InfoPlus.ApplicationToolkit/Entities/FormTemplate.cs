using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoPlus.ApplicationToolkit.Entities
{
    /// <summary>
    /// Summary description for FormTemplate
    /// </summary>
    public class FormTemplate
    {
        // 用来标记一个流程，在Workflow Designer里设定，不会因工作流或表单的变化而改变。
        public string WorkflowCode { get; set; }
        public string WorkflowId { get; set; }      // required to Start a FormInstance
        public string WorkflowName { get; set; }    // Not avaliable
        public string WorkflowTags { get; set; }    // 该工作流的Tag
        
        public string TemplateId { get; set; }		// Id，GUID
        public string TemplateName { get; set; }	// 名称
        public long CreationDate { get; set; }		// 创建时间
        // public int Status { get; set; }          // 暂未使用。0:Draft, 1:Online, 2:Offline
        public string StartUri { get; set; }	    // 发起页的Uri

        public InfoPlusUser CreatorTemplate { get; set; }	        // 创建人Id
        
        IList<FormField> formFields = new List<FormField>();
        /// <summary>
        /// // 字段数组
        /// </summary>
        public IList<FormField> FormFields
        {
            get { return formFields; }
            set { formFields = value; }
        }


        IList<FormTemplate> historyVersions = new List<FormTemplate>();
        /// <summary>
        /// 历史版本列表，仅当本模板为最新版是有意义
        /// </summary>
        public IList<FormTemplate> HistoryVersions
        {
            get { return historyVersions; }
            set { historyVersions = value; }
        }
        FormTemplate headVersion;
        /// <summary>
        /// 最新版本的FormTemplate，不存在意味着本模板既最新版
        /// </summary>
        public FormTemplate HeadVersion
        {
            get { return this.headVersion; }
            set { this.headVersion = value; }
        }

    }
}