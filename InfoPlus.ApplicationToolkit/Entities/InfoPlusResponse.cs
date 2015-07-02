using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{

    public class InfoPlusResponse
    {
        public InfoPlusResponse()
        {
            this.Cancel = false;
            this.Break = false;
            this.Prompt = string.Empty;
            this.Detail = string.Empty;
            FormData = new Dictionary<string, object>();
        }

        public InfoPlusResponse(bool isCancel, bool isBreak, string prompt)
        {
            this.Cancel = isCancel;
            this.Break = isBreak;
            this.Prompt = prompt;
            this.Detail = string.Empty;
            FormData = new Dictionary<string, object>();
        }

        public InfoPlusResponse(bool isCancel, bool isBreak, string prompt, string detail)
        {
            this.Cancel = isCancel;
            this.Break = isBreak;
            this.Prompt = prompt;
            this.Detail = detail;
            FormData = new Dictionary<string, object>();
        }

        /// <summary>
        /// cancel the current doing if Cancel == True
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// break the subcriber and prevent further Messengers
        /// </summary>
        public bool Break { get; set; }

        /// <summary>
        /// when you cancel the event, give the user a prompt.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// for Expiration.
        /// 0: do nothing
        /// -1: kill
        /// positive: seconds to extend.
        /// </summary>
        public long Then { get; set; }

        /// <summary>
        /// Then, after Expiration, submit an Action
        /// </summary>
        public long ThenAction { get; set; }


        /// <summary>
        /// Detail will contain exception infomation
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// Response form data, when: 
        ///     WorkflowStarting as Initialization data
        ///     FieldChanging as auto fills.
        /// </summary>
        public IDictionary<string, object> FormData { get; set; }

        /// <summary>
        /// Codes. Maybe a list of CodeList, which used when:
        /// 1.ON_FIELD_SUGGESTING return only one CodeList suggested,
        /// 2.ON_STEP_RENDERING/ED, return CodeLists should be initialized.
        /// 3.ON_FIELD_CHANGING, as above.
        /// </summary>
        public IList<CodeList> Codes { get; set; }

        public static InfoPlusResponse operator +(InfoPlusResponse r1, InfoPlusResponse r2)
        {
            InfoPlusResponse r = new InfoPlusResponse();
            if (r1.Prompt == string.Empty)
            {
                r.Prompt = r2.Prompt;
            }
            else
            {
                r.Prompt = r1.Prompt;
                if (r2.Prompt != string.Empty)
                {
                    r.Prompt += "、" + r2.Prompt;
                }
            }
            if (r1.Detail == string.Empty)
            {
                r.Detail = r2.Detail;
            }
            else
            {
                r.Detail = r1.Detail;
                if (r2.Detail!=string.Empty)
                {
                    r.Detail += "\n" + r2.Detail;
                }
            }
            r.Cancel = r1.Cancel | r2.Cancel;
            r.Break = r1.Break | r2.Break;

            r.FormData = r1.FormData ?? new Dictionary<string, object>();

            if (null != r2.FormData)
            {
                foreach (string key in r2.FormData.Keys)
                {
                    object val = r2.FormData[key];
                    if (r.FormData.ContainsKey(key))
                        r.FormData[key] = val;
                    else
                        r.FormData.Add(key, val);
                }
            }

            if (null == r2.Codes)
                r.Codes = r1.Codes;
            else if (null == r1.Codes)
                r.Codes = r2.Codes;
            else if (null != r1.Codes && null != r2.Codes) // combine
            {
                r.Codes = r1.Codes;

                foreach (var l2 in r2.Codes)
                {
                    var l1 = r.Codes.FirstOrDefault(l => string.Equals(l.Name, l2.Name,
                        StringComparison.CurrentCultureIgnoreCase));

                    if (null == l1)
                    {
                        r.Codes.Add(l2);
                    }
                    else
                    {
                        foreach (CodeItem i in l2.Items)
                            if (false == l1.Items.Any(c => c.CodeId == i.CodeId))
                                l1.Items.Add(i);
                    }
                }
            }

            return r;
        }
    }


}
