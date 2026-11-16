using System;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.UI;

namespace Project
{
    public class Global : HttpApplication
    {
        private const string LoginPage = "~/Default.aspx";

        void Application_Start(object sender, EventArgs e)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;

            var authConfig = Security.AuthenConfig.Current;
            if (authConfig.ConfigurationError != null)
            {
                // Logins are refused with a configuration error until this is fixed.
                System.Diagnostics.Trace.TraceError("Login configuration invalid: " + authConfig.ConfigurationError);
                LogStartupIssue("AuthConfigError", authConfig.ConfigurationError);
            }
            if (authConfig.ConfigurationWarning != null)
            {
                System.Diagnostics.Trace.TraceWarning("Login configuration: " + authConfig.ConfigurationWarning);
                LogStartupIssue("AuthConfigWarning", authConfig.ConfigurationWarning);
            }
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private static void LogStartupIssue(string task, string message)
        {
            try
            {
                new Core().Logs(System.Configuration.ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString,
                    "Auth", task, message, "system");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("Could not write startup log: " + ex.GetType().Name);
            }
        }

        void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            var page = Context.Handler as Page;
            if (page == null)
            {
                return;
            }

            string path = page.AppRelativeVirtualPath ?? string.Empty;
            if (path.Equals(LoginPage, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!AppAuth.IsLoggedIn)
            {
                Response.Redirect("~/Default.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (AppAuth.IsSessionExpired)
            {
                AppAuth.SignOut();
                Response.Redirect("~/Default.aspx?expired=1", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (path.StartsWith("~/Admin/", StringComparison.OrdinalIgnoreCase) && !AppAuth.IsAdmin)
            {
                Response.Redirect("~/Home", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        void Application_PreSendRequestHeaders(object sender, EventArgs e)
        {
            Response.Headers.Remove("X-Powered-By");
            Response.Headers.Remove("X-AspNet-Version");
            Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }
    }
}
