using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GoogleAnalyticsTracker.Core;
using GoogleAnalyticsTracker.Core.TrackerParameters;

namespace GoogleAnalyticsTracker.Simple
{
    public static class PageViewTrackerExtensions
    {
        public static async Task<TrackingResult> TrackPageViewAsync(this SimpleTracker tracker, string pageTitle, string pageUrl)
        {
            var pageViewParameters = new PageView
            {
                DocumentTitle = pageTitle,
                DocumentLocationUrl = pageUrl,
                CacheBuster = tracker.AnalyticsSession.GenerateCacheBuster()
            };

            return await tracker.TrackAsync(pageViewParameters);
        }

        public static async Task<TrackingResult> TrackPageViewsAsync(this SimpleTracker tracker, IEnumerable<Tuple<string, string>> pages)
        {
            var clientID = Guid.NewGuid().ToString();
            var pageViewParameters = pages.Select(p => new PageView
            {
                DocumentTitle = p.Item1,
                DocumentPath = p.Item2,
                CacheBuster = null,
                ClientId = clientID
            });

            return await tracker.TrackAsync(pageViewParameters);
        }
    }
}