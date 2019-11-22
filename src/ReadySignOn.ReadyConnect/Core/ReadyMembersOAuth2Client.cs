using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace ReadySignOn.ReadyConnect.Core
{
    public class ReadyMembersOAuth2Client : OAuth2Client
    {
        /// <summary>
        /// The authorization endpoint.
        /// </summary>
        private const string AuthorizationEndpoint = "https://members.readysignon.com/connect/authorize";
        /// <summary>
        /// The token endpoint.
        /// </summary>
        private const string TokenEndpoint = "https://members.readysignon.com/connect/token";
        /// <summary>
        /// The user info endpoint.
        /// </summary>
        private const string UserInfoEndpoint = "https://readyconnectsvc.readysignon.com/connect/userinfo";

        private const string EndSessionEndpoint = "https://readyconnectsvc.readysignon.com/connect/Logout";

        /// <summary>
        /// The app id.
        /// </summary>
        private readonly string _appId;
        /// <summary>
        /// The app secret.
        /// </summary>
        private readonly string _appSecret;

        /// <summary>
        /// The requested scopes.
        /// </summary>
        private readonly string[] _requestedScopes;

        public ReadyMembersOAuth2Client(string appId, string appSecret)
            : this(appId, appSecret, new[] { "email" }) { }

        public ReadyMembersOAuth2Client(string appId, string appSecret, params string[] requestedScopes) : base("readymembers")
        {
            if (string.IsNullOrWhiteSpace(appId))
                throw new ArgumentNullException("appId");

            if (string.IsNullOrWhiteSpace(appSecret))
                throw new ArgumentNullException("appSecret");

            if (requestedScopes == null)
                throw new ArgumentNullException("requestedScopes");

            if (requestedScopes.Length == 0)
                throw new ArgumentException("One or more scopes must be requested.", "requestedScopes");

            _appId = appId;
            _appSecret = appSecret;
            _requestedScopes = requestedScopes;
        }

        public override void RequestAuthentication(HttpContextBase context, Uri returnUrl)
        {
            string redirectUrl = this.GetServiceLoginUrl(returnUrl).AbsoluteUri;
            context.Response.Redirect(redirectUrl, endResponse: true);
        }

        public new AuthenticationResult VerifyAuthentication(HttpContextBase context)
        {
            throw new NoNullAllowedException();
        }

        public override AuthenticationResult VerifyAuthentication(HttpContextBase context, Uri returnPageUrl)
        {
            string code = context.Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                return AuthenticationResult.Failed;
            }

            string accessToken = this.QueryAccessToken(returnPageUrl, code);
            if (accessToken == null)
            {
                return AuthenticationResult.Failed;
            }

            IDictionary<string, string> userData = this.GetUserData(accessToken);
            if (userData == null)
            {
                return AuthenticationResult.Failed;
            }

            string id = userData["id"];
            string name;

            // Some oAuth providers do not return value for the 'username' attribute. 
            // In that case, try the 'name' attribute. If it's still unavailable, fall back to 'id'
            if (!userData.TryGetValue("username", out name) && !userData.TryGetValue("name", out name))
            {
                name = id;
            }

            // add the access token to the user data dictionary just in case page developers want to use it
            userData["accesstoken"] = accessToken;

            return new AuthenticationResult(isSuccessful: true, provider: ProviderName, providerUserId: id, userName: name, extraData: userData);
        }


        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            var state = string.IsNullOrEmpty(returnUrl.Query) ? string.Empty : returnUrl.Query.Substring(1);

            return BuildUri(AuthorizationEndpoint, new NameValueCollection
            {
                { "client_id", _appId },
                { "scope", string.Join(" ", _requestedScopes) },
                { "redirect_uri", returnUrl.GetLeftPart(UriPartial.Path) },
                { "state", state },
            });
        }

        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            var uri = BuildUri(UserInfoEndpoint, new NameValueCollection { { "access_token", accessToken } });

            var webRequest = (HttpWebRequest)WebRequest.Create(uri);

            using (var webResponse = webRequest.GetResponse())
            using (var stream = webResponse.GetResponseStream())
            {
                if (stream == null)
                    return null;

                using (var textReader = new StreamReader(stream))
                {
                    var json = textReader.ReadToEnd();
                    var extraData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    var data = extraData.ToDictionary(x => x.Key, x => x.Value.ToString());

                    data.Add("picture", string.Format("https://members.readysignon.com/{0}/picture", data["id"]));

                    return data;
                }
            }
        }

        public string QueryAccessTokenByCode(Uri returnUrl, string authorizationCode)
        {
            return this.QueryAccessToken(returnUrl, authorizationCode);
        }

        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var uri = BuildUri(TokenEndpoint, new NameValueCollection
            {
                { "code", authorizationCode },
                { "client_id", _appId },
                { "client_secret", _appSecret },
                { "redirect_uri", returnUrl.GetLeftPart(UriPartial.Path) },
            });

            var webRequest = (HttpWebRequest)WebRequest.Create(uri);
            string accessToken = null;

            using (var response = (HttpWebResponse)webRequest.GetResponse())
            {
                // handle response from FB 
                // this will not be a url with params like the first request to get the 'code'
                Encoding rEncoding = Encoding.GetEncoding(response.CharacterSet);

                using (var sr = new StreamReader(response.GetResponseStream(), rEncoding))
                {
                    var serializer = new JavaScriptSerializer();
                    var jsonObject = serializer.DeserializeObject(sr.ReadToEnd());
                    var jConvert = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(jsonObject));

                    Dictionary<string, object> desirializedJsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jConvert.ToString());
                    accessToken = desirializedJsonObject["access_token"].ToString();
                }
            }

            return accessToken;
        }

        private static Uri BuildUri(string baseUri, NameValueCollection queryParameters)
        {
            var keyValuePairs = queryParameters.AllKeys.Select(k => HttpUtility.UrlEncode(k) + "=" + HttpUtility.UrlEncode(queryParameters[k]));
            var qs = String.Join("&", keyValuePairs);

            var builder = new UriBuilder(baseUri) { Query = qs };
            return builder.Uri;
        }

        /// <summary>
        /// Facebook works best when return data be packed into a "state" parameter.
        /// This should be called before verifying the request, so that the url is rewritten to support this.
        /// </summary>
        public static void RewriteRequest()
        {
            var ctx = HttpContext.Current;

            var stateString = HttpUtility.UrlDecode(ctx.Request.QueryString["state"]);
            if (stateString == null || !stateString.Contains("__provider__=readymembers"))
                return;

            var q = HttpUtility.ParseQueryString(stateString);
            q.Add(ctx.Request.QueryString);
            q.Remove("state");

            ctx.RewritePath(ctx.Request.Path + "?" + q);
        }
    }
}