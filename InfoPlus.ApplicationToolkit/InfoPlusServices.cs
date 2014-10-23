using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using SJTU.SJTURight.ApplicationToolkit;
using Studio.Foundation.Json;
using Studio.Foundation.Json.Core.Conversion;
using InfoPlus.ApplicationToolkit.Entities;
using System.Collections.Specialized;

namespace InfoPlus.ApplicationToolkit
{
    /// <summary>
    /// Summary description for InfoPlusSerivces
    /// </summary>
    [DataObject(true)]
    public class InfoPlusServices
    {

        public static readonly string METHOD_START = "Start";
        public static readonly string METHOD_FIELDS = "ListWorkflowFields";

        public static readonly string METHOD_ALTER = "Alter";
        public static readonly string METHOD_DO_ACTION = "DoAction";
        public static readonly string METHOD_DATA = "QueryFormData";

        public static readonly string METHOD_CANDO = "ListCanDoTemplates";
        public static readonly string METHOD_TODO = "ListFormStepSummaries";

        public static readonly string RESOURCE_IDENTIFIER = "_id_";

        public static IDictionary<string, string> METHODS_MAP = new Dictionary<string, string>()
        {
            { "Start", "PUT process" },
            { "ListWorkflowFields", "GET app/{0}/fields" },

            { "Alter", "POST process/{0}" },
            { "DoAction", "POST task/{0}/submit" },
            { "QueryFormData", "GET process/{0}/data" },

            { "ListCanDoTemplates", "GET me/apps" },
            { "ListFormStepSummaries", "GET me/tasks/{0}" }

        };

        public InfoPlusApplication App { get; set; }

        /// <summary>
        /// Default contructor
        /// </summary>
        public InfoPlusServices(InfoPlusApplication app) 
        {
            this.App = app;
            if (null == this.App)
                throw new Exception("INVALID_APP");
        }

        public InfoPlusServices(string workflow)
        {
            this.App = ApplicationSettings.FindApp(workflow);
            if (null == this.App)
                throw new Exception("INVALID_APP");
        }


        #region Form Templates APIs : Templates, Fields, Preview

        /// <summary>
        /// 表单模板接口
        /// 2.1.	ListCanDoTemplates
        /// 作用：列举当前用户所有可以发起的表单模板
        /// </summary>
        public static IList<FormTemplate> ListCanDoTemplates(string userId)
        {
            string method = InfoPlusServices.METHOD_CANDO;
            var nvc = new List<KeyValuePair<string, object>>();
            nvc.Add(new KeyValuePair<string, object>("userId", userId));
            return ApplicationSettings.FindValidApp().Invoke<FormTemplate>(method, nvc);
        }

        /*
        /// <summary>
        /// 表单模板接口
        /// 2.2.	FindFormTemplate
        /// 作用：获取特定FormTemplate的详情
        /// </summary>
        // public static string FindFormTemplate(string templateId)
        // {  return null;  }
        */
        /// <summary>
        /// 表单模板接口
        /// 2.3.	ListFormTemplateFields
        /// 作用：枚举给定表单模板的表单
        /// </summary>
        public static ResponseEntity<FormField> ListWorkflowFields(string workflowId, string workflowCode)
        {
            string method = InfoPlusServices.METHOD_FIELDS;
            var nvc = new List<KeyValuePair<string, object>>();
            if (string.IsNullOrEmpty(workflowId))
            {
                nvc.Add(new KeyValuePair<string, object>("workflowId", workflowId));
                nvc.Add(new KeyValuePair<string, object>(InfoPlusServices.RESOURCE_IDENTIFIER, workflowCode));
            }
            else
            {
                nvc.Add(new KeyValuePair<string, object>(InfoPlusServices.RESOURCE_IDENTIFIER, workflowId));
                nvc.Add(new KeyValuePair<string, object>("workflowCode", workflowCode));
            }
            return ApplicationSettings.FindApp(workflowCode).Invoke<FormField>(method, nvc);
        }

        /// <summary>
        /// 表单模板接口
        /// 2.4.	Start
        /// 作用：启动一个表单实例。
        /// 返回值：1.instanceId, 2.第一步骤的formStepId.ToString(), 3.第一步骤的访问Uri
        /// </summary>
        public static ResponseEntity<string> Start(string creator, string userId, string businessId,
            string workflowCode, string initialData)
        {
            string method = InfoPlusServices.METHOD_START;
            var nvc = new List<KeyValuePair<string, object>>();
            nvc.Add(new KeyValuePair<string, object>("creator", creator));
            nvc.Add(new KeyValuePair<string, object>("userId", userId));
            nvc.Add(new KeyValuePair<string, object>("businessId", businessId));
            nvc.Add(new KeyValuePair<string, object>(InfoPlusServices.RESOURCE_IDENTIFIER, workflowCode));
            nvc.Add(new KeyValuePair<string, object>("initialData", initialData));
            return ApplicationSettings.FindApp(workflowCode).Invoke<string>(method, nvc);
        }

        #endregion

        #region Form Instances APIs : Categories, Instances, Remarks, Attachments


        /// <summary>
        /// 表单实例接口 
        /// 3.1. ListCategories 
        /// 作用：列举系统文件夹/分类。
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        //public static ResponseEntity<Category> ListCategories(string userId)
        //{
        //    string method = "ListCategories"; 
        //    var nvc = new NameValueCollection();
        //    nvc.Add("userId", userId);
        //    return ApplicationSettings.FindValidApp().Invoke<Category>(method, nvc);
        //}

        /// <summary>
        /// 表单实例接口 
        /// 3.2.	ListFormStepSumaries
        /// 作用：列举对应文件夹中的表单步骤，并按照lastUpdate时间逆序排序。
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="category"></param>
        /// <param name="start"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static ResponseEntity<FormStep> ListFormStepSummaries(string userId, string category, int start, int limit)
        {
            string method = InfoPlusServices.METHOD_TODO;
            var nvc = new List<KeyValuePair<string, object>>();
            nvc.Add(new KeyValuePair<string, object>("userId", userId));
            nvc.Add(new KeyValuePair<string, object>(InfoPlusServices.RESOURCE_IDENTIFIER, category));
            nvc.Add(new KeyValuePair<string, object>("start", start));
            nvc.Add(new KeyValuePair<string, object>("limit", limit));
            return ApplicationSettings.FindValidApp().Invoke<FormStep>(method, nvc);
        }

        /// <summary>
        /// 表单实例接口
        /// 3.3. QueryFormData
        /// 作用：查询指定表单实例或表单步骤的表单数据。当仅指定到表单实例时，将返回最后一次表单提交后的表单数据。
        /// </summary>
        /// <param name="instanceId">Start返回的instanceId</param>
        /// <param name="formStepId">表单步骤Id</param>
        /// <returns></returns>
        public static ResponseEntity<IDictionary<string, object>> QueryFormData(long entryId, long formStepId)
        {
            string method = InfoPlusServices.METHOD_DATA;
            var nvc = new List<KeyValuePair<string, object>>();
            nvc.Add(new KeyValuePair<string, object>(InfoPlusServices.RESOURCE_IDENTIFIER, entryId));
            nvc.Add(new KeyValuePair<string, object>("formStepId", formStepId));
            return ApplicationSettings.FindValidApp().Invoke<IDictionary<string, object>>(method, nvc);
        }

        /// <summary>
        /// 表单实例接口 
        /// 3.4.	Alter
        /// 作用：编程的改变工作流实例的状态或者数据（变量）。
        /// 参数：
        /// 参数名	类型	说明
        /// verify	string	
        /// entryId	long	工作流实例Id
        /// content   要修改什么内容：数据、状态、超时等
        /// data	String	根据content而异。
        ///         当content为数据时，此处为表单数据的JSON,通过data参数指定的（字段/变量名->值）的哈希表来修改、追加表单数据。
        ///         当content为状态时，此处为{ status:{InstanceState}, reason:{string} } : 参见AlterState类。
        ///             status	int	其中，status可以为：
        ///                 a)	挂起(2)
        ///                 b)	恢复(1)
        ///                 c)	完成(5)（已废弃，参见DoAction接口）
        ///                 d)	中止(3)
        ///                 e)	NULL(0)，不改变状态，仅修改表单数据/变量。
        ///         当content为超时时... 尚不支持
        /// </summary>
        public static ResponseEntity<object> Alter(long entryId, AlterScopes scope, string data)
        {
            string method = InfoPlusServices.METHOD_ALTER;
            var nvc = new List<KeyValuePair<string, object>>();
            nvc.Add(new KeyValuePair<string, object>(InfoPlusServices.RESOURCE_IDENTIFIER, entryId));
            nvc.Add(new KeyValuePair<string, object>("scope", scope));
            nvc.Add(new KeyValuePair<string, object>("data", data));
            return ApplicationSettings.FindValidApp().Invoke<object>(method, nvc);
        }

        /// <summary>
        /// 表单实例接口 
        /// 3.5. DoAction 
        /// 作用：自动完成某个步骤，注：该步骤必须在工作流编辑器标记为可以被自动执行。
        /// </summary>
        public static ResponseEntity<FormStep> DoAction(long entryId, string stepCode, string actionCode, IDictionary<string, string> stepUsers, string comment)
        {
            string method = InfoPlusServices.METHOD_ALTER;
            var nvc = new List<KeyValuePair<string, object>>();
            nvc.Add(new KeyValuePair<string, object>(InfoPlusServices.RESOURCE_IDENTIFIER, entryId));
            nvc.Add(new KeyValuePair<string, object>("stepCode", stepCode));
            nvc.Add(new KeyValuePair<string, object>("actionCode", actionCode));
            var users = null == stepUsers ? string.Empty : JsonConvert.ExportToString(stepUsers);
            nvc.Add(new KeyValuePair<string, object>("users", users));
            nvc.Add(new KeyValuePair<string, object>("comment", comment ?? string.Empty));
            return ApplicationSettings.FindValidApp().Invoke<FormStep>(method, nvc);
        }

        #endregion

    }
}