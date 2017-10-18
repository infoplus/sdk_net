using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml;
using System.Reflection;
using InfoPlus.ApplicationToolkit.Entities;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;

namespace InfoPlus.ApplicationToolkit
{

    /// <summary>
    /// Summary description for Settings
    /// </summary>
    public class ApplicationSettings : IConfigurationSectionHandler
    {
        static object _lock = new object();

        public static Random DICE = new Random(DateTime.Now.Millisecond);

        public static readonly string SESSION_KEY_ACCOUNT = "Account";

        public object Create(object parent, object configContext, XmlNode section)
        {
            lock (_lock)
            {
                // 1. Parse services we invoke.
                var nodes = section.SelectNodes("services");
                string serviceId = (string)this.parseAttribute<string>(nodes[0], "identification", null);
                // default domain
                ApplicationSettings._DEFAULT_DOMAIN = (string)this.parseAttribute<string>(nodes[0], "domain", "localhost");
                // auth endpoint
                string _AuthEndPoint = (string)this.parseAttribute<string>(nodes[0], "authorize", null);
                // for "Insecure" sevice type only
                ApplicationSettings._INFOPLUS_MAGIC = (string)this.parseAttribute<string>(nodes[0], "secret", "INFOPLUS_MAGIC");
                
                // service type
                var typeAttr = nodes[0].Attributes["type"];
                ApplicationSettings._ServiceType = ServiceType.Entitle;
                var sType = null == typeAttr ? string.Empty : typeAttr.Value;
                if (Enum.IsDefined(typeof(ServiceType), sType))
                {
                    ApplicationSettings._ServiceType = (ServiceType)Enum.Parse(typeof(ServiceType), sType, true);
                }

                nodes = section.SelectNodes("services/service");
                SERVICES = new List<EndPointDescription>();
                foreach (XmlNode s in nodes)
                {
                    EndPointDescription ep = new EndPointDescription();
                    ep.Identification = serviceId;
                    ep.Address = s.Attributes["address"].Value;
                    SERVICES.Add(ep);
                    if ((bool)this.parseAttribute<bool>(nodes[0], "trustAllCert", false))
                        ServicePointManager.ServerCertificateValidationCallback = TrustAllCertHandler;

                }

                // let's choose one.
                int i = DICE.Next(ApplicationSettings.SERVICES.Count);
                ApplicationSettings.SERVICE = ApplicationSettings.SERVICES[i];

                // 2. Parse messengers we provide.
                nodes = section.SelectNodes("messengers");
                // compatable 1
                if (null == nodes || nodes.Count == 0)
                    nodes = section.SelectNodes("workflows/workflow");

                ApplicationSettings.MESSENGERS = new List<AbstractMessenger>();
                foreach (XmlNode ms in nodes)
                {
                    string subscriber = (string)this.parseAttribute<string>(ms, "address", null);
                    bool requireVerification = (bool)this.parseAttribute<bool>(ms, "requireVerification", true);
                    var code = (string)this.parseAttribute<string>(ms, "code", null);
                    var secret = (string)this.parseAttribute<string>(ms, "secret", null);
                    var scope = (string)this.parseAttribute<string>(ms, "scope", null);
                    var release = (bool)this.parseAttribute<bool>(ms, "release", true);
                    var minVersion = (long)this.parseAttribute<long>(ms, "minVersion", 0);
                    var maxVersion = (long)this.parseAttribute<long>(ms, "maxVersion", long.MaxValue);

                    // compatable 2
                    if (string.IsNullOrEmpty(code) && ApplicationSettings.ServiceType != ServiceType.Entitle)
                        throw new ConfigurationErrorsException("workflow code is not set.", ms);
                    InfoPlusApplication app = null;
                    if (null != code)
                    {
                        app = new InfoPlusApplication(code, secret, scope, _AuthEndPoint, release, minVersion, maxVersion);
                        ApplicationSettings.workflows[app.FullCode] = app;
                    }
                    foreach (XmlNode m in ms)
                    {
                        string type = m.Attributes["type"].Value;

                        InfoPlusApplication appOverride = null;
                        var code2 = (string)this.parseAttribute<string>(m, "workflow", null);
                        var secret2 = (string)this.parseAttribute<string>(m, "secret", null);
                        var scope2 = (string)this.parseAttribute<string>(m, "scope", null);
                        var release2 = (bool)this.parseAttribute<bool>(m, "release", true);
                        var minVersion2 = (long)this.parseAttribute<long>(m, "minVersion", 0);
                        var maxVersion2 = (long)this.parseAttribute<long>(m, "maxVersion", long.MaxValue);

                        if (false == string.IsNullOrEmpty(code2))
                        {
                            appOverride = new InfoPlusApplication(code2, secret2, scope2, _AuthEndPoint, release2, minVersion2, maxVersion2);
                            ApplicationSettings.workflows[appOverride.FullCode] = appOverride;
                        }
                        
                        if(null == app && null == appOverride)
                            throw new ConfigurationErrorsException("app not set.", ms);

                        // Cache timeout
                        int timeout = (int)this.parseAttribute<int>(m, "timeout", 0);
                        if (timeout < 0) timeout = 10;

                        if (string.IsNullOrEmpty(subscriber)) continue;
                        string[] splits = type.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries); ;
                        string assembly = "App_Code";
                        if (splits.Length > 1) assembly = splits[1];
                        AbstractMessenger messenger = (AbstractMessenger)Assembly.Load(assembly).CreateInstance(splits[0]);
                        messenger.Subscriber = subscriber;
                        messenger.Workflow = app ?? appOverride;
                        messenger.RequireVerification = requireVerification;
                        messenger.Timeout = timeout;
                        ApplicationSettings.MESSENGERS.Add(messenger);
                        System.Diagnostics.Trace.WriteLine("Create messenger:" + type);
                    }
                }
                // 3.Mashups
                nodes = section.SelectNodes("mashups");
                mashups = new Dictionary<string, string>();
                foreach (XmlNode ms in nodes)
                {
                    string bs = ms.Attributes["base"].Value;
                    foreach (XmlNode m in ms)
                    {
                        string key = m.Attributes["key"].Value;
                        string val = m.Attributes["value"].Value;
                        mashups[key] = bs + val;
                    }
                }
                // return nothing. we'v already saved this section to ApplicationSettings.
                return null;
            }
        }

        static void EnsureSettingsLoaded()
        {
            if (null == MESSENGERS)
                lock (_lock)
                    if (null == MESSENGERS)
                        ConfigurationManager.GetSection("infoPlusSettings");
        }

        object parseAttribute<T>(XmlNode node, string attribute, T defaultValue)
        {
            var attr = node.Attributes[attribute];
            if (null == attr)
                return defaultValue;
            var val = attr.Value;
            if (typeof(T) == typeof(string))
                return val;
            else if (typeof(T) == typeof(int))
            {
                int e;
                if (int.TryParse(val, out e)) return e;
                return defaultValue;
            }
            else if (typeof(T) == typeof(long))
            {
                long e;
                if (long.TryParse(val, out e)) return e;
                return defaultValue;
            }
            else if (typeof(T) == typeof(bool))
            {
                bool e;
                if (bool.TryParse(val, out e)) return e;
                return defaultValue;
            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// The subscriber path, as the filter of messengers
        /// </summary>
        public string Address { get; set; }

        static IDictionary<string, string> mashups = null;
        public static IDictionary<string, string> MASHUPS
        {
            get
            {
                EnsureSettingsLoaded();
                return ApplicationSettings.mashups;
            }
        }

        static IList<AbstractMessenger> MESSENGERS = null;
        /// <summary>
        /// Every Messenger is in response of a Workflow per site(SDK).
        /// </summary>
        public IList<AbstractMessenger> Messengers
        {
            get
            {
                EnsureSettingsLoaded();
                if (string.IsNullOrEmpty(this.Address) || this.Address == "*")
                    return MESSENGERS;
                else
                    return MESSENGERS.Where(m => string.Equals(m.Subscriber, this.Address, 
                        StringComparison.CurrentCultureIgnoreCase) 
                        || m.Subscriber == "*").ToList();
            }
        }

        public static T GetMessenger<T>() where T : AbstractMessenger
        {
            EnsureSettingsLoaded();
            var x = MESSENGERS.Where(m => m is T).FirstOrDefault();
            return (T)x;
        }

        public static AbstractMessenger GetMessenger(Type type)
        {
            EnsureSettingsLoaded();
            var x = MESSENGERS.Where(m => m.GetType() == type).FirstOrDefault();
            return x;
        }

        static IList<EndPointDescription> SERVICES = null;
        public static IList<EndPointDescription> INFOPLUS_SERVICES
        {
            get
            {
                EnsureSettingsLoaded();
                return SERVICES;
            }
        }

        static EndPointDescription SERVICE = null;
        public static EndPointDescription INFOPLUS_SERVICE
        {
            get
            {
                EnsureSettingsLoaded();
                return ApplicationSettings.SERVICE;
            }
        }


        public static string _DEFAULT_DOMAIN = null; 
        public static string DefaultDomain 
        {
            get
            {
                EnsureSettingsLoaded();
                return ApplicationSettings._DEFAULT_DOMAIN;
            }
        }

        public static string _INFOPLUS_MAGIC { get; set; }
        public static string INFOPLUS_MAGIC
        {
            get
            {
                EnsureSettingsLoaded();
                return ApplicationSettings._INFOPLUS_MAGIC;
            }
        }

        public static ServiceType _ServiceType;
        public static ServiceType ServiceType
        {
            get
            {
                EnsureSettingsLoaded();
                return ApplicationSettings._ServiceType;
            }
        }

        public static InfoPlusApplication FindApp(string workflow) 
        {
            if (false == workflow.Contains("@"))
                workflow += ("@" + ApplicationSettings.DefaultDomain);
            if (false == ApplicationSettings.WORKFLOWS.ContainsKey(workflow))
                throw new Exception("NO_SUCH_WORKFLOW:" + workflow);
            return ApplicationSettings.WORKFLOWS[workflow];
        }


        public static InfoPlusApplication FindValidApp()
        {
            return ApplicationSettings.WORKFLOWS.Values.FirstOrDefault();
        }

        static IDictionary<string, InfoPlusApplication> workflows = new Dictionary<string, InfoPlusApplication>();
        static IDictionary<string, InfoPlusApplication> WORKFLOWS
        {
            get
            {
                if (false == workflows.Any())
                {
                    EnsureSettingsLoaded();
                    if (false == workflows.Any())
                        throw new ConfigurationErrorsException("no workflow found.");
                }
                return workflows;
            }
        }


        public static string CalculateMD5Hash(string input)
        {
            return ApplicationSettings.CalculateMD5Hash(input, System.Text.Encoding.UTF8);
        }

        public static string CalculateMD5Hash(string input, System.Text.Encoding encoding)
        {
            byte[] x = encoding.GetBytes(input);
            byte[] y = new MD5CryptoServiceProvider().ComputeHash(x);
            string result = HexEncoding.ToString(y);
            return result.ToLower();
        }

        public static string StringCut(string source, string prefix, string ending)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            int index = source.IndexOf(prefix, StringComparison.CurrentCulture);
            if (index >= 0)
            {
                source = source.Substring(index + prefix.Length);
                index = source.IndexOf(ending, StringComparison.CurrentCulture);
                if (index >= 0) source = source.Substring(0, index);
            }
            return source;
        }


        public static string VERSION
        {
            get
            {
                return CompileDate.ToString("yyyyMMdd");
            }
        }


        public static DateTime CompileDate
        {
            get
            {
                if (!compileDate.HasValue)
                    compileDate = RetrieveLinkerTimestamp(ExecutingAssembly.Location);
                return compileDate ?? new DateTime();
            }
        }
        private static System.DateTime? compileDate;


        /// <summary>
        /// Gets the executing assembly.
        /// </summary>
        /// <value>The executing assembly.</value>
        public static System.Reflection.Assembly ExecutingAssembly
        {
            get { return executingAssembly ?? (executingAssembly = System.Reflection.Assembly.GetExecutingAssembly()); }
        }
        private static System.Reflection.Assembly executingAssembly;

        private static System.Version executingAssemblyVersion;
        /// <summary>
        /// Gets the executing assembly version.
        /// </summary>
        /// <value>The executing assembly version.</value>
        public static System.Version ExecutingAssemblyVersion
        {
            get { return executingAssemblyVersion ?? (executingAssemblyVersion = ExecutingAssembly.GetName().Version); }
        }

        /// <summary>
        /// Retrieves the linker timestamp.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        /// <remarks>http://www.codinghorror.com/blog/2005/04/determining-build-date-the-hard-way.html</remarks>
        public static System.DateTime RetrieveLinkerTimestamp(string filePath)
        {
            const int peHeaderOffset = 60;
            const int linkerTimestampOffset = 8;
            var b = new byte[2048];
            System.IO.FileStream s = null;
            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                    s.Close();
            }
            var dt = new System.DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(System.BitConverter.ToInt32(b, System.BitConverter.ToInt32(b, peHeaderOffset) + linkerTimestampOffset));
            return dt.AddHours(System.TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
        }

        static bool TrustAllCertHandler(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors error)
        {
            // Ignore errors
            return true;
        }

    }
}