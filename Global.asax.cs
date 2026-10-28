using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace GiantGroupM3
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // Phones and tablets get Site.Mobile.Master. Every page declares MasterPageFile="~/Site.Master",
        // so the choice is made here, during PreInit, before the page loads its master.
        protected void Application_PreRequestHandlerExecute(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            if (context == null) return;

            var page = context.Handler as System.Web.UI.Page;
            if (page == null) return;

            if (IsTouchDeviceRequest(context.Request))
            {
                page.PreInit += UseMobileMaster;
            }
        }

        private static void UseMobileMaster(object sender, EventArgs e)
        {
            var page = (System.Web.UI.Page)sender;
            if (string.Equals(page.MasterPageFile, "~/Site.Master", StringComparison.OrdinalIgnoreCase))
            {
                page.MasterPageFile = "~/Site.Mobile.Master";
            }
        }

        private static bool IsTouchDeviceRequest(HttpRequest request)
        {
            if (request.Browser != null && request.Browser.IsMobileDevice) return true;

            var ua = request.UserAgent ?? string.Empty;
            return ua.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0
                || ua.IndexOf("iPod", StringComparison.OrdinalIgnoreCase) >= 0
                || ua.IndexOf("iPad", StringComparison.OrdinalIgnoreCase) >= 0
                || ua.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0
                || ua.IndexOf("Mobile", StringComparison.OrdinalIgnoreCase) >= 0
                || ua.IndexOf("Tablet", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Track requests per IP address: IP -> (Request Count, Window Start Time)
        private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _requestTracker = new ConcurrentDictionary<string, (int, DateTime)>();

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            if (context == null) return;

            // Bypass rate limiting and auth checks for the mobile API handler
            if (context.Request.AppRelativeCurrentExecutionFilePath.EndsWith("MobileApi.ashx", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Get the user's IP address
            var clientIp = context.Request.UserHostAddress;
            var now = DateTime.UtcNow;

            // Clean or update request tracking for this IP (Limit: 60 requests per 1 minute window)
            var clientData = _requestTracker.AddOrUpdate(clientIp,
                _ => (1, now),
                (key, old) =>
                {
                    // Reset window if more than 1 minute has passed
                    if ((now - old.WindowStart).TotalMinutes > 1)
                    {
                        return (1, now);
                    }
                    return (old.Count + 1, old.WindowStart);
                });

            // If the client exceeds 60 requests in a minute, block them with HTTP 429
            if (clientData.Count > 60)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.StatusDescription = "Too Many Requests";
                context.Response.Write("Rate limit exceeded. Please wait a moment before trying again.");
                context.ApplicationInstance.CompleteRequest();
            }
        }

    }
}