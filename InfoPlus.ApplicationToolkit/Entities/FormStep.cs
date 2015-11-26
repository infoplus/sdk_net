using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class FormStep
    {
        public long StepId { get; set; }			// 表单步骤Id
        public int State { get; set; }              // 步骤状态：1->待做，2->已做, 3->待合并
        public string StepCode { get; set; }		// 表单步骤Code
        public string StepName { get; set; }		// 表单步骤名称（暂时无法获取）

        public string TemplateId { get; set; }		// 表单模板Id
        public string TemplateName { get; set; }	// 表单模板名称

        public string WorkflowId { get; set; }      // 工作流Id
        public string WorkflowCode { get; set; }    // 工作流Code
        public long WorkflowVersion { get; set; }   // 工作流版本的时间戳
        public string Domain { get; set; }          // 工作流所属租户域名

        public string ApplicationId { get; set; }		// 表单所属的应用Id
        public string ApplicationName { get; set; }		// 表单所属的应用名称

        public FormInstance Instance { get; set; }		// 表单实例

        public InfoPlusUser AssignUser { get; set; }	// 待办人[1]，为空表示未指定到人
        public long AssignTime { get; set; }		    // 任务获得时间
        public InfoPlusUser ActionUser { get; set; }	// 处理人[2]，仅对doing/done等有效
        public long ActionTime { get; set; }   		    // 任务处理时间

        public string RenderUri { get; set; }			// 查看/处理表单的Uri

        public string TaskId { get; set; }              // GUID for this task.(also known as RemarkId)
        public string Remark { get; set; } 

        public string SplitIdentifier { get; set; }     //分支id
    }
}