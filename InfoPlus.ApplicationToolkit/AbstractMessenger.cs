using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfoPlus.ApplicationToolkit.Entities;
using Studio.Foundation.Json;
using System.Reflection;
using System.Web.Caching;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Threading;

namespace InfoPlus.ApplicationToolkit
{
    public abstract class AbstractMessenger
    {
        /// <summary>
        /// The event subscriber address that subscribe/dispatch InfoPlusEvents
        /// </summary>
        public string Subscriber { get; set; }
        /// <summary>
        /// Whether parameter verifications are required.
        /// </summary>
        public bool RequireVerification { get; set; }

        /// <summary>
        ///  The workflow application that providers authentication information
        /// </summary>
        public InfoPlusApplication Workflow { get; set; }

        /// <summary>
        /// Timeout minutes for Caches, such as FormFields
        /// </summary>
        public int Timeout { get; set; }


        /// <summary>
        /// Thread local.
        /// </summary>
        // public ThreadLocal<InfoPlusEvent> CurrentEvent { get; set; }

        /// <summary>
        /// Although ThreadLocal is supported in .NET Framework 4.0
        /// we still use .NET 3.5 for compatible
        /// </summary>
        IDictionary<int, InfoPlusEvent> threadSafeEvents = new ConcurrentDictionary<int, InfoPlusEvent>();
        IDictionary<int, long> threadBirthdays = new ConcurrentDictionary<int, long>();
        /// <summary>
        /// The current InfoPlusEvent
        /// </summary>
        public InfoPlusEvent CurrentEvent
        {
            get
            {
                int key = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return this.threadSafeEvents[key];
            }
            set
            {
                lock (this)
                {
                    int key = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    this.threadSafeEvents[key] = value;
                    long birth = UnixTime.ToInt64(DateTime.UtcNow);
                    this.threadBirthdays[key] = birth;
                    long elder = birth - (this.Timeout + 10) * 60;
                    var threads = (from kv in this.threadBirthdays
                                   where kv.Value < elder
                                   select kv.Key).ToArray();
                    foreach (int t in threads)
                    {
                        this.threadSafeEvents.Remove(t);
                        this.threadBirthdays.Remove(t);
                    }
                }
            }
        }

        public virtual InfoPlusResponse OnInstanceStarting(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceStarted(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceCompleting(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceCompleted(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceExpiring(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceExpired(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceKilling(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceKilled(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceCompensation(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceExporting(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnInstanceRendering(InfoPlusEvent e) { return this.Update(e); }

        public virtual InfoPlusResponse OnActionSaving(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnActionSaved(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnFieldChanging(InfoPlusEvent e) { return this.Update(e); }
        
        /* 
         * The following events are divided.
        public virtual InfoPlusResponse OnActionDoing(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnActionDone(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnStepRendering(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnStepRendered(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnActionClicking(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnStepPrinting(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnStepExpiring(InfoPlusEvent e) { return this.Update(e); }
        public virtual InfoPlusResponse OnStepExpired(InfoPlusEvent e) { return this.Update(e); }
        */

        /// <summary>
        /// Suggesting is implemented for cache & pinyin by default.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual InfoPlusResponse OnFieldSuggesting(InfoPlusEvent e) 
        {
            if (null == e.Suggestion)
                return this.Update(e);
            try
            {
                string code = e.Suggestion.Code;
                var cl = AbstractMessenger.cachedCodeLists;
                var ce = AbstractMessenger.cachedCodeExpires;
                if (e.Suggestion.Dirty || false == cl.ContainsKey(code) || ce[code] < UnixTime.ToInt64(DateTime.Now))
                {
                    lock (_code_lock)
                    {
                        if (e.Suggestion.Dirty || false == cl.ContainsKey(code) || ce[code] < UnixTime.ToInt64(DateTime.Now))
                        {
                            CodeList list = this.OnQueryCodeTable(code);
                            if (null == list || null == list.Items || 0 == list.Items.Count)
                            {
                                throw new Exception("CodeTable " + code + " invalid.");
                            }
                            foreach (var item in list.Items)
                            {
                                item.CalculateSpell();
                            }
                            // add to cache
                            AbstractMessenger.cachedCodeLists[code] = list;
                            AbstractMessenger.cachedCodeExpires[code] = UnixTime.ToInt64(DateTime.Now.AddHours(1));
                        }
                    }
                }
                CodeList l = this.Suggest(e.Suggestion);
                var response = new InfoPlusResponse();
                response.Codes = new List<CodeList>();
                response.Codes.Add(l);
                return response;
            }
            catch (Exception ex)
            {
                return new InfoPlusResponse(true, true, ex.Message);
            }
        }

        /// <summary>
        ///  Evict CodeTable Cache
        /// </summary>
        /// <param name="code"></param>
        public static void EvictCodeTableCache(string code)
        {
            lock (_code_lock)
            {
                if (string.IsNullOrEmpty(code))
                {
                    AbstractMessenger.cachedCodeLists.Clear();
                    AbstractMessenger.cachedCodeExpires.Clear();
                }
                else
                {
                    AbstractMessenger.cachedCodeLists.Remove(code);
                    AbstractMessenger.cachedCodeExpires.Remove(code);
                }
            }
        }

        protected virtual CodeList OnQueryCodeTable(string code)
        {
            throw new NotImplementedException("NOT_IMPLEMENTED:" + code);
        }

        static string ToFirstUpper(string str)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// This event is sealed. not virtual as the other events interface.
        /// You must implement it like: OnStep{StepCode}Action{ActionCode}Doing(e)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public InfoPlusResponse OnActionDoing(InfoPlusEvent e) 
        {
            string method = string.Format("OnStep{0}Action{1}Doing",
                AbstractMessenger.ToFirstUpper(e.Step.StepCode), 
                AbstractMessenger.ToFirstUpper(e.ActionCode));
            return this.CallEventMethodWrapper(method, e);
        }

        public InfoPlusResponse OnActionDone(InfoPlusEvent e) 
        {
            var methods = new List<string>();
            var stepCode = AbstractMessenger.ToFirstUpper(e.Step.StepCode);
            var actionCode = AbstractMessenger.ToFirstUpper(e.ActionCode);
            methods.Add(string.Format("OnStep{0}Action{1}Done", stepCode, actionCode));
            if(null != e.NextSteps)
                foreach(var step in e.NextSteps)
                    methods.Add(string.Format("ToStep{0}Done", AbstractMessenger.ToFirstUpper(step.StepCode)));
            if(null != e.EndStep)
                methods.Add(string.Format("ToStep{0}Done", AbstractMessenger.ToFirstUpper(e.EndStep.StepCode)));

            bool called = false;
            var response = this.Update(e);

            foreach (string method in methods)
            {
                var r2 = this.CallEventMethod(method, e);
                if (null != r2)
                {
                    called = true;
                    response += r2;
                }
            }
            if (false == called)
                System.Diagnostics.Trace.WriteLine("All Methods in (" +
                    string.Join(",", methods.ToArray()) + ") are not found.");

            return response;
        }

        public InfoPlusResponse OnActionClicking(InfoPlusEvent e)
        {
            string method = string.Format("OnStep{0}Action{1}Clicking", 
                AbstractMessenger.ToFirstUpper(e.Step.StepCode), 
                AbstractMessenger.ToFirstUpper(e.ActionCode));
            return this.CallEventMethodWrapper(method, e);
        }

        public InfoPlusResponse OnStepRendering(InfoPlusEvent e) 
        {
            string method = string.Format("OnStep{0}Rendering", AbstractMessenger.ToFirstUpper(e.Step.StepCode));
            return this.CallEventMethodWrapper(method, e);
        }

        public InfoPlusResponse OnStepRendered(InfoPlusEvent e) 
        {
            string method = string.Format("OnStep{0}Rendered", AbstractMessenger.ToFirstUpper(e.Step.StepCode));
            return this.CallEventMethodWrapper(method, e);
        }

        public InfoPlusResponse OnStepPrinting(InfoPlusEvent e)
        {
            string method = string.Format("OnStep{0}Printing", AbstractMessenger.ToFirstUpper(e.Step.StepCode));
            return this.CallEventMethodWrapper(method, e);
        }

        public InfoPlusResponse OnStepExporting(InfoPlusEvent e)
        {
            string method = string.Format("OnStep{0}Exporting", AbstractMessenger.ToFirstUpper(e.Step.StepCode));
            return this.CallEventMethodWrapper(method, e);
        }

        public InfoPlusResponse OnStepExpiring(InfoPlusEvent e)
        {
            string method = string.Format("OnStep{0}Expiring", AbstractMessenger.ToFirstUpper(e.Step.StepCode));
            return this.CallEventMethodWrapper(method, e);
        }

        public InfoPlusResponse OnStepExpired(InfoPlusEvent e)
        {
            string method = string.Format("OnStep{0}Expired", AbstractMessenger.ToFirstUpper(e.Step.StepCode));
            return this.CallEventMethodWrapper(method, e);
        }

        public InfoPlusResponse OnStepWithdrawing(InfoPlusEvent e)
        {
            if (string.IsNullOrEmpty(e.ActionCode))
            {
                string method = string.Format("OnStep{0}Withdrawing", AbstractMessenger.ToFirstUpper(e.Step.StepCode));
                return this.CallEventMethodWrapper(method, e);
            }
            else 
            {
                string method = string.Format("OnStep{0}Action{1}Withdrawing",
                        AbstractMessenger.ToFirstUpper(e.Step.StepCode),
                        AbstractMessenger.ToFirstUpper(e.ActionCode));
                return this.CallEventMethodWrapper(method, e);
            }
        }

        public InfoPlusResponse OnStepWithdrawn(InfoPlusEvent e)
        {
            if (string.IsNullOrEmpty(e.ActionCode))
            {
                string method = string.Format("OnStep{0}Withdrawn", AbstractMessenger.ToFirstUpper(e.Step.StepCode));
                return this.CallEventMethodWrapper(method, e);
            }
            else
            {
                string method = string.Format("OnStep{0}Action{1}Withdrawn",
                        AbstractMessenger.ToFirstUpper(e.Step.StepCode),
                        AbstractMessenger.ToFirstUpper(e.ActionCode));
                return this.CallEventMethodWrapper(method, e);
            }
        }

        InfoPlusResponse CallEventMethodWrapper(string method, InfoPlusEvent e)
        {
            var response = this.Update(e);
            var r2 = this.CallEventMethod(method, e);
            if (null != r2)
                response = r2;
            else
                System.Diagnostics.Trace.WriteLine("Method " + method + " not found.");
            return response;
        }

        InfoPlusResponse CallEventMethod(string method, InfoPlusEvent e)
        {
            var type = this.GetType();
            if (null == type.GetMethod(method))
                return null;

            var response = this.GetType().InvokeMember(
                method,
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                null,
                this,
                new object[] { e }
            );
            if (response is InfoPlusResponse)
                return (InfoPlusResponse)response;
            return new InfoPlusResponse();
        }

        /// <summary>
        /// Update Cache
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected InfoPlusResponse Update(InfoPlusEvent e)
        {
            return new InfoPlusResponse();
        }
        
        public static T GetInstance<T>() where T : AbstractMessenger
        {
            return ApplicationSettings.GetMessenger<T>();
        }

        #region CachedFormFields

        DateTime lastUpdateFormFields = DateTime.MinValue;

        IDictionary<string, IList<FormField>> cachedFormFields = new Dictionary<string, IList<FormField>>();
        /// <summary>
        /// maintain a copy of fields for developer.
        /// you can also Invoke InfoPlusSerivces.ListTemplateFields(templateId, workflowCode) instead
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public IList<FormField> CachedFormFields
        {
            get
            {
                InfoPlusEvent e = this.CurrentEvent;
                TimeSpan span = DateTime.Now - lastUpdateFormFields;
                string workflowId = null;
                if (null != e && null != e.Step)
                    workflowId = e.Step.WorkflowId;

                string key = this.Workflow.FullCode;
                if (false == string.IsNullOrEmpty(workflowId))
                    key = workflowId;

                if (null == cachedFormFields || span.TotalMinutes > this.Timeout)
                {
                    ResponseEntity<FormField> re = InfoPlusServices.ListWorkflowFields(workflowId, this.Workflow.FullCode);
                    if (re.Successful)
                    {
                        this.cachedFormFields[key] = re.entities;
                        lastUpdateFormFields = DateTime.Now;
                    }
                }
                if (this.cachedFormFields.ContainsKey(key))
                    return this.cachedFormFields[key];
                else
                    return null;
            }
        }

        #endregion

        #region CachedCodeTables

        static object _evictLock = new object();

        protected int EvictCode(string code, string itemId)
        {
            CodeList codes;
            if (false == AbstractMessenger.cachedCodeLists.TryGetValue(code, out codes))
                return 0;
            if(null == codes.Items || false == codes.Items.Any())
                return 0;

            int result = 0;
            lock (_evictLock)
            {
                int index = 0;
                while(index < codes.Items.Count)
                {
                    var item = codes.Items[index];
                    if (item.CodeId == itemId)
                    {
                        if (true == codes.Items.Remove(item))
                            result++;
                    }
                    else
                    {
                        index++;
                    }
                }
            }
            return result;
        }

        // static, for the whole app
        static IDictionary<string, CodeList> cachedCodeLists = new ConcurrentDictionary<string, CodeList>();
        static IDictionary<string, long> cachedCodeExpires = new ConcurrentDictionary<string, long>();
        static object _code_lock = new object();

        protected CodeList Suggest(CodeSuggestion suggestion)
        {
            CodeList codes = AbstractMessenger.cachedCodeLists[suggestion.Code];
            if (null == codes)
                throw new Exception("CodeTable invalid.");

            var parent = suggestion.Parent;
            var isTopLevel = suggestion.TopLevel;
            var prefix = suggestion.Prefix;
            var pageSize = suggestion.PageSize;
            if (pageSize <= 0)
            {
                pageSize = 15;
            }

            {
                IList<CodeItem> items = codes.Items;
                if (false == string.IsNullOrEmpty(parent) || false == isTopLevel)
                {
                    items = codes.Filter(parent).Items;
                }
                // make a copy.
                codes = new CodeList { Items = new List<CodeItem>(items), Name = codes.Name };
            }

            if (prefix.Length == 0)
            {
                var x = (from c in codes.Items
                         where c.IsEnabled
                         orderby c.ItemIndex, c.CodeName ascending
                         select c);
                codes.Items = x.Skip(pageSize * suggestion.PageNo).Take(pageSize).ToArray();
            }
            else
            {
                var x = (from c in codes.Items
                         where c.IsEnabled &&
                            (this.IndexOf((c.CodeName + '|' + c.Spell).ToLower(), prefix) >= 0)
                         orderby c.ItemIndex, this.IndexOf((c.CodeName + '|' + c.Spell).ToLower(), prefix), c.CodeName ascending
                         select c);
                codes.Items = x.Skip(pageSize * suggestion.PageNo).Take(pageSize).ToArray();
            }

            return codes;
        }

        /// <summary>
        /// IndexOf pattern match source in EVERY dot-sperated parts
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        int IndexOf(string source, string pattern)
        {
            if (null == pattern) return 0;
            if (null == source) return -1;
            int score = 0;
            string[] patterns = pattern.Split(new char[] {',', ' '});
            foreach (string p in patterns)
            {
                int i = source.IndexOf(p, StringComparison.Ordinal);
                if (i < 0) return -1;
                score += i;
            }
            // 给CodeName为空的加点分，让其排到后面去
            if (source.StartsWith("|")) score += 1000;
            return score;
        }

        #endregion


        internal bool Match(InfoPlusEvent e)
        {
            // to compatible with old version without domain
            if (null == e.Step.Domain)
            {
                return string.Equals(this.Workflow.Code, e.Step.WorkflowCode, StringComparison.CurrentCultureIgnoreCase);
            }
            else
            {
                // match code
                var match = string.Equals(this.Workflow.Code, e.Step.WorkflowCode, StringComparison.CurrentCultureIgnoreCase) || this.Workflow.Code == "*";
                if (!match) return false;
                // match domain
                 match = string.Equals(this.Workflow.Domain, e.Step.Domain, StringComparison.CurrentCultureIgnoreCase) || this.Workflow.Domain == "*";
                if (!match) return false;
                // match version
                return (e.Step.WorkflowVersion >= this.Workflow.MinVersion && e.Step.WorkflowVersion <= this.Workflow.MaxVersion);
            }
        }
    }
}
