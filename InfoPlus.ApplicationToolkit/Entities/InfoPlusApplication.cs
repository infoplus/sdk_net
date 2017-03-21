using SJTU.SJTURight.ApplicationToolkit;
using Studio.Foundation.Json;
using Studio.Foundation.Json.Core.Conversion;
using Studio.OAuth2;
using Studio.OAuth2.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class InfoPlusApplication
    {
        public string Secret { get; set; }
        public string Scope { get; set; }
        public bool Release { get; set; }
        public long MinVersion { get; set; }
        public long MaxVersion { get; set; }

        public InfoPlusApplication(string code, string secret, string scope, string authUri, bool release, long minVersion, long maxVersion)
        {
            if(false == code.Contains("@"))
                code = code + "@" + ApplicationSettings.DefaultDomain;

            this.fullCode = code;
            this.Secret = secret;
            this.Scope = scope;
            this.Release = release;
            this.MinVersion = minVersion;
            this.MaxVersion = maxVersion;

            if (false == string.IsNullOrEmpty(authUri))
            {
                var settings = new OAuth2Settings();
                settings.ConsumerKey = this.fullCode;
                settings.ConsumerSecret = this.Secret;
                if(false == authUri.EndsWith("/")) authUri += "/";
                settings.EndPointAuthorize = authUri + "auth";
                settings.EndPointToken = authUri + "token";
                this.OAuth2 = new OAuth2Consumer(settings);
            }
        }

        string fullCode;
        public string FullCode { get { return fullCode; } }

        public string Code
        {
            get
            {
                return FullCode.Substring(0, FullCode.LastIndexOf("@"));
            }
        }

        public string Domain
        {
            get
            {
                return FullCode.Substring(FullCode.LastIndexOf("@") + 1);
            }
        }

        /// <summary>
        /// HA1=MD5(username:realm:password)
        /// HA2=MD5(method:digestURI)
        /// response=MD5(HA1:nonce:HA2)
        /// </summary>
        /// <returns></returns>
        public string CalculateDigest(string method, string digestURI, string nonce) 
        {
            string ha1 = ApplicationSettings.CalculateMD5Hash(Code + ":" + Domain + ":" + Secret, Encoding.ASCII);
            string ha2 = ApplicationSettings.CalculateMD5Hash(method + ":" + digestURI, Encoding.ASCII);
            return ApplicationSettings.CalculateMD5Hash(ha1 + ":" + nonce + ":" + ha2, Encoding.ASCII);
        }

        public OAuth2Consumer OAuth2 { get; set; }

        static object token_lock = new object();
        AccessTokenV2 _accessToken;
        private string _AuthEndPoint;
        
        public AccessTokenV2 AccessToken
        {
            get
            {
                long then = UnixTime.ToInt64(DateTime.Now);
                if (null == _accessToken || then > _accessToken.expires_in - 10)
                {
                    lock (token_lock)
                    {
                        if (null == _accessToken || then > _accessToken.expires_in - 10)
                        {
                            if (null == _accessToken || null == _accessToken.refresh_token)
                                _accessToken = this.OAuth2.GrantByClientCredentials(this.Scope);
                            else
                                _accessToken = this.OAuth2.GrantByRefreshToken(_accessToken.refresh_token);

                            if (null == _accessToken)
                                throw new Exception("TOKEN_GRANT_FAILED");   
                        }
                    }
                }
                return _accessToken;
            }
        }


        public ResponseEntity<T> Invoke<T>(string method, IList<KeyValuePair<string, object>> arguments)
        {
            string result = null;
            var endPoint = ApplicationSettings.INFOPLUS_SERVICE.Address;
            switch (ApplicationSettings.ServiceType)
            { 
                case ServiceType.Entitle:
                    var serviceId = ApplicationSettings.INFOPLUS_SERVICE.Identification;
                    object[] args = new object[arguments.Count];
                    for (int i = 0; i < arguments.Count; i++) args[i] = arguments[i].Value;
                    result = EntitleServices.InvokeApplicationService(serviceId, endPoint, method, args);
                    break;
                case ServiceType.OAuth2:
                    var map = this.Release ? InfoPlusServices.METHODS_MAP : InfoPlusServices.METHODS_MAP_DEBUG;
                    if (false == map.ContainsKey(method))
                        throw new NotSupportedException(method);
                    method = map[method];
                    var token = this.AccessToken;
                    var index = method.IndexOf(' ');
                    var verb = method.Substring(0, index);
                    var res = method.Substring(index + 1);
                    if (res.Contains("{0}"))
                    {
                        var id = arguments.FirstOrDefault(a => a.Key == InfoPlusServices.RESOURCE_IDENTIFIER);
                        res = string.Format(res, id.Value);
                    }
                    NameValueCollection nvc = new NameValueCollection();
                    foreach (var pair in arguments)
                        if (pair.Key != InfoPlusServices.RESOURCE_IDENTIFIER && null != pair.Value)
                            nvc.Add(pair.Key, pair.Value.ToString());
                    if (false == endPoint.EndsWith("/")) endPoint += "/";
                    result = this.OAuth2.RestRequest(endPoint + res, verb, token.access_token, nvc);
                    break;
                case ServiceType.Insecure:
                    int count = 1;
                    args = new object[count + arguments.Count];
                    args[0] = ApplicationSettings.INFOPLUS_MAGIC;
                    // args[1] = ApplicationSettings.VERSION;
                    for (int i = 0; i < arguments.Count; i++) args[i + count] = arguments[i].Value;
                    result = (string)WebServiceHelper.InvokeWebService(endPoint, method, args);
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (string.IsNullOrEmpty(result)) return default(ResponseEntity<T>);
            ResponseEntity<T> re = JsonConvert.Import<ResponseEntity<T>>(result);
            return re;
        }
    }
}
