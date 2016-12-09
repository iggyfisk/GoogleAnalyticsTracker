using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GoogleAnalyticsTracker.Core.Interface;

namespace GoogleAnalyticsTracker.Core
{    
    public partial class TrackerBase : IDisposable
    {
        public const string TrackingAccountConfigurationKey = "GoogleAnalyticsTracker.TrackingAccount";
        public const string TrackingDomainConfigurationKey = "GoogleAnalyticsTracker.TrackingDomain";

        const string BeaconUrl = "http://www.google-analytics.com/collect";
        const string BeaconUrlSsl = "https://ssl.google-analytics.com/collect";

        const string BatchUrl = "http://www.google-analytics.com/batch";
        const string BatchUrlSsl = "https://ssl.google-analytics.com/batch"; 

        public string TrackingAccount { get; set; }
        public string TrackingDomain { get; set; }
        public IAnalyticsSession AnalyticsSession { get; set; }

        public string Hostname { get; set; }
        public string Language { get; set; }
        public string UserAgent { get; set; }
        public string CharacterSet { get; set; }        

        public bool ThrowOnErrors { get; set; }        
        public bool UseSsl { get; set; }

        public TrackerBase(string trackingAccount, string trackingDomain, ITrackerEnvironment trackerEnvironment)
            : this(trackingAccount, trackingDomain, new AnalyticsSession(), trackerEnvironment)
        {
        }

        public TrackerBase(string trackingAccount, string trackingDomain, IAnalyticsSession analyticsSession, ITrackerEnvironment trackerEnvironment)
        {
            TrackingAccount = trackingAccount;
            TrackingDomain = trackingDomain;
            AnalyticsSession = analyticsSession;

            Hostname = trackerEnvironment.Hostname;
            Language = "en";
            UserAgent = string.Format("GoogleAnalyticsTracker/3.0 ({0}; {1}; {2})", trackerEnvironment.OsPlatform, trackerEnvironment.OsVersion, trackerEnvironment.OsVersionString);

            InitializeCharset();                  
        }

        private void InitializeCharset()
        {
            CharacterSet = "UTF-8";
        }

        private static byte[] StringToAscii(string s)
        {
            byte[] retval = new byte[s.Length];
            for (int ix = 0; ix < s.Length; ++ix)
            {
                char ch = s[ix];
                if (ch <= 0x7f) retval[ix] = (byte)ch;
                else retval[ix] = (byte)'?';
            }
            return retval;
        }

        private async Task<TrackingResult> RequestUrlAsync(string url, IDictionary<string, string> parameters, string userAgent = null)
        {
            // Create GET string
            var data = new StringBuilder();
            foreach (var parameter in parameters)
            {
                data.Append(string.Format("{0}={1}&", parameter.Key, Uri.EscapeDataString(parameter.Value)));
            }

            // Build TrackingResult
            var returnValue = new TrackingResult
            {
                Url = url,
                Parameters = parameters
            };

            // Determine referer URL
            var referer = string.Format("http://{0}/", TrackingDomain);
            if (parameters.ContainsKey("ReferralUrl"))
            {
                referer = parameters["ReferralUrl"];
            }

            // Create request
            HttpWebRequest request;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(string.Format("{0}?{1}", url, data));                
                request.SetHeader("Referer", referer);
                request.SetHeader("User-Agent", userAgent ?? UserAgent);
            }
            catch (Exception ex)
            {
                if (ThrowOnErrors)                
                    throw;                
                
                returnValue.Success = false;
                returnValue.Exception = ex;
                return returnValue;
            }

            // Perform request
            WebResponse response = null;
            try
            {
                response = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
                returnValue.Success = true;
            }
            catch (Exception ex)
            {
                if (ThrowOnErrors)                
                    throw;                
                else
                {
                    returnValue.Success = false;
                    returnValue.Exception = ex;
                }
            }
            finally
            {
                if (response != null)                
                    response.Dispose();                
            }

            return returnValue;
        }

        private async Task<TrackingResult> PostUrlAsync(string url, IEnumerable<IDictionary<string, string>> parameterLines, string userAgent = null)
        {
            // Create GET string
            var data = new StringBuilder();
            foreach (var parameters in parameterLines)
            {
                foreach(var parameter in parameters)
                {
                    data.Append(string.Format("{0}={1}&", parameter.Key, Uri.EscapeDataString(parameter.Value)));
                }
                data.Length--;
                data.Append("\n");
            }

            // Build TrackingResult
            var returnValue = new TrackingResult
            {
                Url = url
            };

            // Determine referer URL
            var referer = string.Format("http://{0}/", TrackingDomain);
            if (parameterLines.First().ContainsKey("ReferralUrl"))
            {
                referer = parameterLines.First()["ReferralUrl"];
            }

            // Create request
            HttpWebRequest request;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.SetHeader("Referer", referer);
                request.SetHeader("User-Agent", userAgent ?? UserAgent);
                
                byte[] bytes = StringToAscii(data.ToString());
                System.IO.Stream newStream = request.GetRequestStream();
                newStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                if (ThrowOnErrors)
                    throw;

                returnValue.Success = false;
                returnValue.Exception = ex;
                return returnValue;
            }

            // Perform request
            WebResponse response = null;
            try
            {
                response = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
                returnValue.Success = true;
            }
            catch (Exception ex)
            {
                if (ThrowOnErrors)
                    throw;
                else
                {
                    returnValue.Success = false;
                    returnValue.Exception = ex;
                }
            }
            finally
            {
                if (response != null)
                    response.Dispose();
            }

            return returnValue;
        }

        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
