using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;

namespace Power8.Helpers
{
    public static class Analytics
    {
        private static AnalyticsClient _web;

        public static void Init(string trackingId, string clientId, string appName, string appVersion)
        {
            _web = new AnalyticsClient(trackingId, clientId, appName, appVersion);
        }

        public static void PostEvent(Category category, string action, string label, int? value)
        {
            AnalyticsCall("event", new Dictionary<string, string>
            {
                {"ec", category.ToString()},
                {"ea", action},
                {"el", label},
                {"ev", value.HasValue ? value.ToString() : null}
            });
        }

        public enum Category
        {
            Deploy, Runtime
        }

        private static void AnalyticsCall(string hitType, Dictionary<string, string> args)
        {
            if (_web == null)
            {
                Log.Raw("Analytics subsystem is not initialized");
                return;
            }
            try
            {
                var result = _web.PostAnalyticsHit(hitType, args);
                Log.Fmt("Code: {0}, data: {1}", _web.ResponseCode, new string(Encoding.Default.GetChars(result)));
            }
            catch (Exception ex)
            {
               Log.Raw(ex.ToString());
            }
        }

        private class AnalyticsClient : WebClient
        {
            public AnalyticsClient(string resourceId, string clientId, string appName, string appVersion)
            {
                Encoding = Encoding.UTF8;
                _resourceId = resourceId;
                _clientId = clientId;
                _appName = appName;
                _appVer = appVersion;
                Headers[HttpRequestHeader.UserAgent] =
                    string.Format("{0}/{1} ({2})", _appName, _appVer, Environment.OSVersion);
                _responseCodeHolder = new ThreadLocal<HttpStatusCode>();
            }
            public byte[] PostAnalyticsHit(string hitType, Dictionary<string, string> hitArgs)
            {
                var data = new NameValueCollection();
                data["v"] = "1";
                data["tid"] = _resourceId;
                data["cid"] = _clientId;
                data["an"] = _appName;
                data["av"] = _appVer;
                data["t"] = hitType;
                if (1 == Interlocked.CompareExchange(ref _sessionStarting, 0, 1))
                {
                    data["sc"] = "start";
                }
                foreach (var a in hitArgs)
                {
                    if (a.Value != null)
                        data[a.Key] = a.Value;
                }
                return UploadValues(@"https://ssl.google-analytics.com/collect", WebRequestMethods.Http.Post, data);
            }

            public HttpStatusCode ResponseCode
            {
                get
                {
                    return _responseCodeHolder.Value;
                }
                private set
                {
                    _responseCodeHolder.Value = value;
                }
            }

            private readonly ThreadLocal<HttpStatusCode> _responseCodeHolder;
            private readonly string _resourceId, _clientId, _appName, _appVer;

            protected override WebResponse GetWebResponse(WebRequest request)
            {
                var resp = base.GetWebResponse(request);
                var hResp = resp as HttpWebResponse;
                if (hResp != null)
                {
                    ResponseCode = hResp.StatusCode;
                }
                return resp;
            }

            private int _sessionStarting = 1;
        }
    }
}
