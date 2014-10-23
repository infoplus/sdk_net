using System;
using System.Collections.Generic;
using System.Linq;
using Studio.Foundation.Json;
using InfoPlus.ApplicationToolkit.Entities;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Web.Services.Description;
using System.Web;
using SJTU.SJTURight.ApplicationToolkit;
using Studio.Foundation.Json.Core.Conversion;
using System.Reflection;
using System.Security.Cryptography;
using System.Net;
using System.Runtime.Caching;

namespace InfoPlus.ApplicationToolkit
{

    /// <summary>
    /// InfoPlus event subscriber, process all EventTypes
    /// </summary>
    [System.Web.Services.WebService(Namespace = "http://infoplus.tk/subscribers/")]
    [System.Web.Services.WebServiceBinding(ConformsTo = System.Web.Services.WsiProfiles.BasicProfile1_1)]
    // Allow this Web Service to be called from script, using ASP.NET AJAX
    [System.Web.Script.Services.ScriptService]
    public class Subscriber : System.Web.Services.WebService, ISubscriber
    {

        ApplicationSettings settings = new ApplicationSettings();

        MemoryCache nonceCache = new MemoryCache("nonce");

        /// <summary>
        /// 5 min as default deviation
        /// </summary>
        static int DEVIATION = 5;
        public Subscriber()
        {
            string path = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath;
            settings.Address = path;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        [SoapRpcMethod(Use = SoapBindingUse.Literal)]
        public string OnEvent(string verify, string version, string eventType, string eventData)
        {
            // Trace
            string s = string.Format("eventType:{0},eventData:{1}", eventType, eventData);
            System.Diagnostics.Trace.WriteLine(s);
            InfoPlusResponse r = new InfoPlusResponse();
            
            try
            {
                // 1.Data type conversion.
                EventTypes eTypes = (EventTypes)Enum.Parse(typeof(EventTypes), eventType);
                // just return the default response when ECHO
                if (eTypes == EventTypes.ECHO) return JsonConvert.ExportToString(r);

                InfoPlusEvent e = JsonConvert.Import<InfoPlusEvent>(eventData);
                if (null == e)
                    return JsonConvert.ExportToString(new InfoPlusResponse(true, true, "InfoPlusEvent malformed."));

                var workflowCode = e.Step.WorkflowCode;
                if (false == e.Step.WorkflowCode.Contains("@")) workflowCode += ("@" + ApplicationSettings.DefaultDomain);

                // 2.Retrieve messengers
                IList<AbstractMessenger> messengers = this.settings.Messengers;
                var targets = from m in messengers
                              where string.Equals(m.Workflow.Code, e.Step.WorkflowCode, StringComparison.CurrentCultureIgnoreCase)
                                || m.Workflow.FullCode == "*@" + ApplicationSettings.DefaultDomain
                              select m;


                // 3.Dispatch messages. 
                // Devoted to Charles Petzold, the first Win32 program, Orz by marstone, 2011/06/29
                // Changed switch(eventType) to Reflection. Devoted to Brian Cantwell Smith, 2011/07
                bool verified = !targets.Any();
                string detail = null;
                foreach (AbstractMessenger messenger in targets)
                {
                    // 4.Check verify.
                    verified = true;
                    if (messenger.RequireVerification && ServiceType.Insecure != ApplicationSettings.ServiceType) 
                    {
                        ResponseEntity re = this.CheckParameters(messenger, verify, version, version, eventType, eventData);
                        if (false == re.Successful)
                        {
                            verified = false;
                            detail = re.error;
                            break;
                        }
                    }

                    // Notice: the CurrentEvent is thread safe. by marstone, 2011/10/18
                    messenger.CurrentEvent = e;

                    InfoPlusResponse response = null;
                    try
                    {
                        var words = (from t in eTypes.ToString().Split(new char[] { '_' })
                                     select t[0] + t.Substring(1).ToLower());
                        string method = "On" + string.Join("", words.ToArray());

                        string log = string.Format("Dispatching {0} to {1}->{2}", eTypes, messenger.GetType().Name, method);
                        System.Diagnostics.Trace.WriteLine(log);

                        response = (InfoPlusResponse)messenger.GetType().InvokeMember(
                            method,
                            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                            null,
                            messenger,
                            new object[] { e }
                        );
                    }
                    catch (Exception mex)
                    {
                        System.Diagnostics.Trace.WriteLine(mex);
                        String message = mex.Message + "\n" + mex.StackTrace;
                        while (true)
                        {
                            if (mex.InnerException == null || mex.InnerException == mex) break;
                            mex = mex.InnerException;
                            message += "\n" + mex.Message + "\n" + mex.StackTrace;
                        }
                        response = new InfoPlusResponse(true, true, "发生未知的错误", message);
                    }
                    if (null != response)
                    {
                        r += response;
                        // skip next messenger if said Break.
                        if (response.Break) break;
                    }
                }

                if (false == verified)
                {
                    r = new InfoPlusResponse(true, true, "Verification Failed.", detail);
                }

                return JsonConvert.ExportToString(r);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                return JsonConvert.ExportToString(new InfoPlusResponse(true, true, "发生未知的错误", ex.Message + ex.StackTrace));
            }
        }


        /// <summary>
        /// Check verify here and return Application that send the request.
        /// null if parameter invalid.
        /// </summary>
        ResponseEntity CheckParameters(AbstractMessenger messenger, string verify, string version, params object[] args)
        {
            string log = "[Subscriber][OnEvent][";
            bool valid = false;
            string details = string.Empty;

            try
            {
                try
                {
                    System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace();
                    log += (stack.FrameCount > 1 ? stack.GetFrame(1).GetMethod().Name : "Unknown Method");
                    log += "]Called with parameters:";
                    log += string.Format("version:{0},apikey:{1},args:{2}", version, verify,
                        string.Join(",", args.Select(a => (null == a) ? "null" : a.ToString()).ToArray()));
                }
                catch { }

                switch (ApplicationSettings.ServiceType)
                {
                    case ServiceType.Entitle: 
                        string key = SJTU.SJTURight.ApplicationToolkit.EntitleServices.ClientKey;
                        ServerTicket ticket = new ServerTicket(key, this.Context.Request.UserHostAddress);

                        string reason;
                        Invoker invoker = ticket.Validate(verify, (int)ValidateLevels.Expire, out reason, args);

                        details = string.Format("expire:{0}|clientId:{1}|clientIP:{2}|session:{3}|reason:{4}",
                            invoker.ExpireStamp, invoker.InvokerId, invoker.InvokerIP, invoker.SessionKey, reason);

                        valid = invoker.IsValid;
                        break;
                    case ServiceType.OAuth2:
                        var authorization = HttpContext.Current.Request.Headers["Authorization"];
                        verify =  ApplicationSettings.StringCut(authorization, "response=\"", "\"");;
                        var method = HttpContext.Current.Request.HttpMethod;
                        var uri1 =  ApplicationSettings.StringCut(authorization, "uri=\"", "\"");;
                        /* uri check is useless, disable it, by marstone
                        var uri2 = HttpContext.Current.Request.Url.AbsoluteUri;
                        if (false == uri2.Equals(uri1, StringComparison.CurrentCultureIgnoreCase))
                        {
                            valid = false;
                            details = string.Format("expectUri:{0},actualUri:{1}", uri2, uri1);
                            break;
                        }
                        */
                        var nonce = ApplicationSettings.StringCut(authorization, "nonce=\"", "\"");
                        if (null == nonce || this.nonceCache.Contains(nonce))
                        {
                            valid = false;
                            details = string.Format("nonce '{0}' invalid or replayed.", nonce);
                            break;
                        }
                        var now = DateTime.Now;
                        var future = now.AddMinutes(20);
                        var index = nonce.LastIndexOf('-');
                        if(index > 0)
                        {
                            var timeStr = nonce.Substring(index + 1);
                            int timestamp;
                            if (true == int.TryParse(timeStr, out timestamp)) 
                            {
                                var then = UnixTime.ToDateTime(timestamp);
                                var timespan = now - then;
                                if ((-DEVIATION) >= timespan.TotalMinutes || timespan.TotalMinutes > DEVIATION)
                                {
                                    valid = false;
                                    details = string.Format("expectTime:{0},actualTime:{1}", now, then);
                                    break;
                                }
                            }
                        }
                        var expect = messenger.Workflow.CalculateDigest(method, uri1, nonce);
                        valid = expect.Equals(verify, StringComparison.CurrentCultureIgnoreCase);
                        if (valid)
                        { 
                            // cache nonce
                            var policy = new CacheItemPolicy();
                            policy.AbsoluteExpiration = now.AddMinutes(20);
                            this.nonceCache.Add(nonce, string.Empty, policy);
                        }
                        details = string.Format("expect:{0},actual:{1}", expect, verify);
                        break;
                }

                log += (valid ? "[Authenticated successfully]:" : "[Authenticated FAILED]:");
                log += string.Format("auth:[{0}]", details);
                return new ResponseEntity(valid ? 0 : 1, log);
            }
            catch (Exception ex)
            {
                valid = false;
                System.Diagnostics.Debug.WriteLine(ex.Message);
                log += string.Format("[Exception][{0}]", ex.Message);
                return new ResponseEntity(-1, log);
            }
            finally
            {
                System.Diagnostics.Trace.WriteLine(log);
            }
        }
        

    }

}