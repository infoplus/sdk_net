using System.Text;
using System.Web;
using System.Net;
using Studio.Foundation.Json;
using System.Security.Cryptography;
using System.IO;
using Studio.OAuth2.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Studio.OAuth2.Entities
{
    #region OAuth2 Entities

    public class OAuth2Settings
    {
        public string EndPointToken { get; set; }
        public string EndPointAuthorize { get; set; }
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
    }

    [Serializable]
    public class AccessTokenV2
    {
        /// <summary>
        /// REQUIRED.  The access token issued by the authorization server.
        /// </summary>
        public string access_token { get; set; }
        // 
        public string token_type { get; set; }
        /// <summary>
        /// RECOMMENDED.  The lifetime in seconds of the access token.
        /// </summary>
        public int expires_in { get; set; }

        /// <summary>
        /// OPTIONAL, if identical to the scope requested by the client; otherwise, REQUIRED. 
        /// </summary>
        public string scope { get; set; }
        /// <summary>
        /// REQUIRED if the "state" parameter was present in the client authorization request.  The exact value received from the client.
        /// </summary>
        public string state { get; set; }

        public string refresh_token { get; set; }
        public string error { get; set; }
    }

    public class GrantTypes
    {
        public string Value { get; set; }
        public GrantTypes(string value) { this.Value = value; }
        public override string ToString() { return Value; }

        public static readonly GrantTypes AuthorizationCode = new GrantTypes("authorization_code");
        public static readonly GrantTypes RefreshToken = new GrantTypes("refresh_token");
        public static readonly GrantTypes Password = new GrantTypes("password");
        public static readonly GrantTypes ClientCredentials = new GrantTypes("client_credentials");
    }

    #endregion

}

namespace Studio.OAuth2
{
    /// <summary>
    /// Standalone OAuth 2.0 Consumer Implementation
    /// by marstone, since 2013/02/10
    /// </summary>
    public class OAuth2Consumer
    {

        public const string ContentTypeForm = "application/x-www-form-urlencoded;charset=UTF-8";

        public const string ContentTypeJSON = "application/json;charset=UTF-8";

        static Random DICE = new Random((int)DateTime.Now.Ticks);

        public OAuth2Settings Settings { get; set; }

        public OAuth2Consumer(OAuth2Settings settings)
        {
            this.Settings = settings;
        }

        HMAC hmacsha1 = HMACSHA1.Create();


        #region Token

        /// <summary>
        /// http://tools.ietf.org/html/rfc6749#section-4.1
        /// </summary>
        /// <param name="code"></param>
        /// <param name="redirectUri"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public AccessTokenV2 GrantByAuthorizationCode(string code, string redirectUri, string scope)
        {
            var nvc = new NameValueCollection() { { "code", code }, { "redirect_uri", redirectUri }, { "scope", scope } };
            return this.Token(GrantTypes.AuthorizationCode, nvc);
        }

        public AccessTokenV2 GrantByRefreshToken(string refreshToken)
        {
            return this.Token(GrantTypes.RefreshToken, new NameValueCollection() { { "refresh_token", refreshToken } });
        }

        /// <summary>
        /// http://tools.ietf.org/html/rfc6749#section-4.3
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public AccessTokenV2 GrantByResourceOwnerPasswordCredentials(string userName, string password, string scope)
        {
            var nvc = new NameValueCollection() { { "username", userName }, { "password", password }, { "scope", scope } };
            return this.Token(GrantTypes.Password, nvc);
        }

        /// <summary>
        /// http://tools.ietf.org/html/rfc6749#section-4.4
        /// The client can request an access token using only its client
        /// credentials (or other supported means of authentication) when the
        /// client is requesting access to the protected resources under its
        /// control, or those of another resource owner that have been previously
        /// arranged with the authorization server (the method of which is beyond
        /// the scope of this specification).
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public AccessTokenV2 GrantByClientCredentials(string scope)
        {
            return this.Token(GrantTypes.ClientCredentials, new NameValueCollection() { { "scope", scope } });
        }

        public AccessTokenV2 Token(GrantTypes type, NameValueCollection nvc)
        {
            string result = null;
            var error = "unknown";
            try
            {
                // var hvc = HttpUtility.ParseQueryString(string.Empty);
                // foreach (string key in nvc.Keys) hvc.Add(key, nvc[key]);
                /// var query = "grant_type=" + type + "&" + hvc.ToString();
                var query = String.Join("&", nvc.AllKeys.Select(a => a + "=" + HttpUtility.UrlEncode(nvc[a])));
                query = "grant_type=" + type + "&" + query;
                result = this.HttpPostBasicAuth(this.Settings.EndPointToken, query);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                error = ex.Message;
            }
            if (null == result) 
                return new AccessTokenV2() { error = error };

            AccessTokenV2 token = new AccessTokenV2();
            // Homebrew Json deserialization, made in China, by marstone
            var pairs = result.Trim(new char[] { ' ', '{', '}' }).Split(',');
            var dict = pairs.ToDictionary<string, string>(p => p.Substring(0, p.IndexOf(":")).Trim().Trim(new char[] { ':' }));
            foreach (var key in dict.Keys) 
            {
                foreach (var prop in typeof(AccessTokenV2).GetProperties())
                {
                    if (prop.Name == key.Trim(new [] {'"'}))
                    {
                        object val = dict[key].Substring(dict[key].IndexOf(":") + 1).Trim().Trim(new char[] { ':', '"' });
                        if(prop.PropertyType == typeof(int)) val = int.Parse((string)val);
                        prop.SetValue(token, val, null);
                    }
                }
            }
            return token;
        }

        #endregion

        #region Api


        public string RestRequest(string endPoint, string verb, string accessToken, NameValueCollection nvc)
        {
            // var hvc = HttpUtility.ParseQueryString(string.Empty);
            // foreach (string key in nvc.Keys) hvc.Add(key, nvc[key]);
            // var data = Encoding.UTF8.GetBytes(hvc.ToString());
            // look at: http://stackoverflow.com/questions/829080/how-to-build-a-query-string-for-a-url-in-c
            // http://stackoverflow.com/questions/3865975/namevaluecollection-to-url-query

            var query = String.Join("&", nvc.AllKeys.Select(a => a + "=" + HttpUtility.UrlEncode(nvc[a])));
            var data = Encoding.UTF8.GetBytes(query);

            var headers = new Dictionary<HttpRequestHeader, string>();
            headers.Add(HttpRequestHeader.Authorization, "Bearer " + accessToken);
            headers.Add(HttpRequestHeader.ContentType, ContentTypeForm);
            return OAuth2Consumer.HttpRequest(endPoint, verb, data, headers);
        }

        #endregion


        #region Utilities

        public string HttpPostBasicAuth(string endPoint, string query, string contentType = ContentTypeForm)
        { 
            var data = Encoding.UTF8.GetBytes(query);
            var headers = new Dictionary<HttpRequestHeader, string>();
            var username = this.Settings.ConsumerKey;
            var password = this.Settings.ConsumerSecret;
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            headers.Add(HttpRequestHeader.Authorization,  "Basic " + credentials);
            headers.Add(HttpRequestHeader.ContentType, contentType);
            return OAuth2Consumer.HttpRequest(endPoint, "POST", data, headers);
        }


        public static string HttpPost2(string url, string data)
        {
            string fullUrl = url + "?" + data;
            HttpWebRequest request = WebRequest.Create(fullUrl) as HttpWebRequest;
            request.Method = "POST";
            request.ContentLength = 0;
            System.IO.Stream stream = null;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            stream = response.GetResponseStream();
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                var result = reader.ReadToEnd();
                stream.Close();
                return result;
            }
        }

        public static string HttpRequest(string endPoint, string verb, byte[] data, IDictionary<HttpRequestHeader, string> headers)
        {
            var method = verb;
            string result;
            // Create The Http Request
            var request = (HttpWebRequest)WebRequest.Create(endPoint);
            request.ServicePoint.Expect100Continue = false;
            // request.Proxy = new WebProxy("127.0.0.1", 8888);
            request.Method = method;
            BuildHeaders(request, headers);
                
            if (null != data && verb != "GET")
            {
                request.ContentLength = data.Length;
                var requestStream = request.GetRequestStream();
                // Write the data to the request stream.
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
            }

            // Call for response
            var response = (HttpWebResponse)request.GetResponse();
            // Get Response characters stream
            var responseStream = response.GetResponseStream();
            if (null == responseStream)
                return null;

            using (StreamReader responseReader = new StreamReader(responseStream, Encoding.UTF8))
            {
                result = responseReader.ReadToEnd();
            }
            return result;
        }

        protected static void BuildHeaders(HttpWebRequest request, IDictionary<HttpRequestHeader, string> headers)
        {
            // Set the ContentType property of the WebRequest.
            if (null != headers)
            {
                foreach (var header in headers)
                {

                    if (false == WebHeaderCollection.IsRestricted(header.Key.ToString()))
                    {
                        // The restricted headers are:
                        // Accept
                        // Connection
                        // Content-Length
                        // Content-Type
                        // Date
                        // Expect
                        // Host
                        // If-Modified-Since
                        // Range
                        // Referer
                        // Transfer-Encoding
                        // User-Agent
                        // Proxy-Connection
                        switch (header.Key)
                        {
                            case HttpRequestHeader.ContentType:
                                request.ContentType = header.Value;
                                break;
                            default:
                                request.Headers[header.Key] = header.Value;
                                break;
                        }
                    }
                }
            }
        }
        #endregion

    }

}
