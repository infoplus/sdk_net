using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Net;
using System.Web.Services.Description;
using System.Collections.Generic;
using System.Reflection;

namespace InfoPlus.ApplicationToolkit
{
    public class WebServiceHelper
    {
        #region InvokeWebService

        /// <summary>
        /// 动态调用web服务
        /// </summary>
        /// <param name="url">web服务WSDL地址</param>
        /// <param name="methodname">方法</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static object InvokeWebService(string wsdlUrl, string methodname, object[] args)
        {
            return WebServiceHelper.InvokeWebService(wsdlUrl, null, methodname, args);
        }

        public static object InvokeWebService(string wsdlUrl, string methodname, IList<object> args)
        {
            object[] arr = new object[args.Count];
            args.CopyTo(arr, 0);
            return WebServiceHelper.InvokeWebService(wsdlUrl, null, methodname, arr);
        }

        static IDictionary<string, Assembly> CacheAssembly = new Dictionary<string, Assembly>();
        static IDictionary<string, DateTime> CacheTimeouts = new Dictionary<string, DateTime>();
        static IDictionary<string, string> CacheServices = new Dictionary<string, string>();

        static object _lock = new object();

        static string @namespace = "Studio.Web.DynamicWebCalling";

        /// <summary>
        /// 10 minutes
        /// </summary>
        static int CACHE_TIME_OUT = 10;

        public static void ClearCache()
        {
            CacheAssembly = new Dictionary<string, Assembly>();
            CacheTimeouts = new Dictionary<string, DateTime>();
            CacheServices = new Dictionary<string, string>();
        }

        public static Assembly CreateAssemblyCached(string wsdlUrl, ref string className)
        {
            string key = AddWSDL(wsdlUrl);

            lock (_lock)
            {

                // Query cache first.
                if (CacheAssembly.ContainsKey(key))
                {
                    if (CacheTimeouts.ContainsKey(key))
                    {
                        if ((DateTime.UtcNow - CacheTimeouts[key]).TotalMinutes <= CACHE_TIME_OUT)
                        {
                            if (false == string.IsNullOrEmpty(CacheServices[key]))
                                className = CacheServices[key];
                            return CacheAssembly[key];
                        }
                    }
                }

                try
                {
                    //获取WSDL
                    WebClient wc = new WebClient();
                    Stream stream = wc.OpenRead(key);
                    ServiceDescription sd = ServiceDescription.Read(stream);
                    ServiceDescriptionImporter sdi = new ServiceDescriptionImporter();
                    // set className according to service name
                    if (sd.Services.Count > 0)
                        className = sd.Services[0].Name;
                    sdi.AddServiceDescription(sd, "", "");
                    CodeNamespace cn = new CodeNamespace(@namespace);

                    //生成客户端代理类代码
                    CodeCompileUnit ccu = new CodeCompileUnit();
                    ccu.Namespaces.Add(cn);
                    sdi.Import(cn, ccu);
                    Microsoft.CSharp.CSharpCodeProvider csc = new Microsoft.CSharp.CSharpCodeProvider();
                    // ICodeCompiler icc = csc.CreateCompiler();

                    //设定编译参数
                    CompilerParameters cplist = new CompilerParameters();
                    cplist.GenerateExecutable = false;
                    cplist.GenerateInMemory = true;
                    cplist.ReferencedAssemblies.Add("System.dll");
                    cplist.ReferencedAssemblies.Add("System.XML.dll");
                    cplist.ReferencedAssemblies.Add("System.Web.Services.dll");
                    cplist.ReferencedAssemblies.Add("System.Data.dll");

                    //编译代理类
                    CompilerResults cr = csc.CompileAssemblyFromDom(cplist, ccu);
                    if (true == cr.Errors.HasErrors)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        foreach (System.CodeDom.Compiler.CompilerError ce in cr.Errors)
                        {
                            sb.Append(ce.ToString());
                            sb.Append(System.Environment.NewLine);
                        }
                        throw new Exception(sb.ToString());
                    }



                    //生成代理实例，并调用方法
                    System.Reflection.Assembly assembly = cr.CompiledAssembly;
                    // add to cache
                    CacheAssembly[key] = assembly;
                    CacheTimeouts[key] = DateTime.UtcNow;
                    CacheServices[key] = className;
                    return assembly;
                }
                catch (Exception ex)
                {
                    if (null == ex.InnerException)
                        throw new Exception(ex.Message, new Exception(ex.StackTrace));
                    else
                        throw new Exception(ex.InnerException.Message, new Exception(ex.InnerException.StackTrace));
                }
            }
        }

        public static object InvokeWebService(string wsdlUrl, string className, string method, object[] args)
        {
            try
            {
                string clsName = null;
                Assembly assembly = CreateAssemblyCached(wsdlUrl, ref clsName);
                if (string.IsNullOrEmpty(className)) className = clsName;
                if (string.IsNullOrEmpty(className)) className = WebServiceHelper.GetWsClassName(wsdlUrl);
                Type t = assembly.GetType(@namespace + "." + className, true, true);
                object obj = Activator.CreateInstance(t);
                System.Reflection.MethodInfo mi = t.GetMethod(method);

                return mi.Invoke(obj, args);
            }
            catch (Exception ex)
            {
                if (null == ex.InnerException)
                    throw new Exception(ex.Message, new Exception(ex.StackTrace));
                else
                    throw new Exception(ex.InnerException.Message, new Exception(ex.InnerException.StackTrace));
            }
        }

        static string GetWsClassName(string wsUrl)
        {
            string[] parts = wsUrl.Split('/');
            string[] pps = parts[parts.Length - 1].Split(new char[] { '.', '?' });
            return pps[0];
        }

        static string AddWSDL(string url)
        {
            if (url.Length > 5)
            {
                url = (url.Substring(url.Length - 5, 5).ToLower().Equals("?wsdl")) ? url : (url += "?wsdl");
            }
            return url;
        }

        #endregion
    }
}
