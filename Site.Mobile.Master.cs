using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.AspNet.Identity;

namespace GiantGroupM3
{
    public partial class Site_Mobile : MasterPage
    {
        private const string AntiXsrfTokenKey = "__AntiXsrfToken";
        private const string AntiXsrfUserNameKey = "__AntiXsrfUserName";
        private string _antiXsrfTokenValue;

        protected void Page_Init(object sender, EventArgs e)
        {
            // The code below helps to protect against XSRF attacks
            var requestCookie = Request.Cookies[AntiXsrfTokenKey];
            Guid requestCookieGuidValue;
            if (requestCookie != null && Guid.TryParse(requestCookie.Value, out requestCookieGuidValue))
            {
                // Use the Anti-XSRF token from the cookie
                _antiXsrfTokenValue = requestCookie.Value;
                Page.ViewStateUserKey = _antiXsrfTokenValue;
            }
            else
            {
                // Generate a new Anti-XSRF token and save to the cookie
                _antiXsrfTokenValue = Guid.NewGuid().ToString("N");
                Page.ViewStateUserKey = _antiXsrfTokenValue;

                var responseCookie = new HttpCookie(AntiXsrfTokenKey)
                {
                    HttpOnly = true,
                    Value = _antiXsrfTokenValue
                };
                if (FormsAuthentication.RequireSSL && Request.IsSecureConnection)
                {
                    responseCookie.Secure = true;
                }
                Response.Cookies.Set(responseCookie);
            }

            Page.PreLoad += master_Page_PreLoad;
        }

        protected void master_Page_PreLoad(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Set Anti-XSRF token
                ViewState[AntiXsrfTokenKey] = Page.ViewStateUserKey;
                ViewState[AntiXsrfUserNameKey] = Context.User.Identity.Name ?? String.Empty;
            }
            else
            {
                // Validate the Anti-XSRF token
                if ((string)ViewState[AntiXsrfTokenKey] != _antiXsrfTokenValue
                    || (string)ViewState[AntiXsrfUserNameKey] != (Context.User.Identity.Name ?? String.Empty))
                {
                    throw new InvalidOperationException("Validation of Anti-XSRF token failed.");
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // If the OWIN authentication cookie is gone, ensure session is completely wiped too
            if (!Context.User.Identity.IsAuthenticated)
            {
                if (Session["UserLevel"] != null || Session["StaffName"] != null || Session["Username"] != null)
                {
                    Session.Clear();
                    Session.Abandon();
                }
            }
        }

        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            // 1. Clear session variables and abandon session
            Session.Clear();
            Session.Abandon();

            // 2. Sign out from Forms Authentication & OWIN Identity
            FormsAuthentication.SignOut();
            var authManager = Context.GetOwinContext().Authentication;
            if (authManager != null)
            {
                authManager.SignOut(Microsoft.AspNet.Identity.DefaultAuthenticationTypes.ApplicationCookie);
            }

            // 3. CRITICAL: Instantly force current thread/context to anonymous 
            var anonymousPrincipal = new System.Security.Principal.GenericPrincipal(
                new System.Security.Principal.GenericIdentity(string.Empty), null);
            Context.User = anonymousPrincipal;
            System.Threading.Thread.CurrentPrincipal = anonymousPrincipal;

            // 4. Expire and clear authentication and session cookies from the browser
            string[] cookiesToClear = { FormsAuthentication.FormsCookieName, ".AspNet.ApplicationCookie", "ASP.NET_SessionId" };
            foreach (var cookieName in cookiesToClear)
            {
                if (Request.Cookies[cookieName] != null)
                {
                    var cookie = new HttpCookie(cookieName, "")
                    {
                        Expires = DateTime.Now.AddYears(-1),
                        HttpOnly = true
                    };
                    Response.Cookies.Add(cookie);
                }
            }

            // 5. Prevent browser caching
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            // 6. FORCE CLEAR current user context immediately
            Context.User = new System.Security.Principal.GenericPrincipal(
                new System.Security.Principal.GenericIdentity(string.Empty), null);

            // 7. Clear the response buffer and redirect instantly
            Response.Clear();
            Response.Redirect("~/", true);
        }

        // --- Permission & Role Helper Methods for Navigation ---

        public bool IsLoggedIn()
        {
            var authManager = HttpContext.Current.GetOwinContext().Authentication;
            if (authManager == null || !HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return false;
            }

            if (Session["UserLevel"] == null && Session["StaffName"] == null && !IsClient())
            {
                return false;
            }

            return true;
        }

        public bool IsClient()
        {
            if (Session["UserRole"] != null && Session["UserRole"].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (Session["Role"] != null && Session["Role"].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (Session["UserType"] != null && Session["UserType"].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (Session["ClientID"] != null)
            {
                return true;
            }

            return false;
        }

        public int GetUserLevel()
        {
            string[] levelKeys = { "UserLevel", "Level", "User_Level", "StaffLevel", "AccessLevel" };

            foreach (var key in levelKeys)
            {
                if (Session[key] != null)
                {
                    if (Session[key] is int intVal)
                    {
                        return intVal;
                    }
                    if (int.TryParse(Session[key].ToString(), out int parsedLevel))
                    {
                        return parsedLevel;
                    }
                }
            }

            if (Session["StaffName"] != null || Session["Username"] != null)
            {
                return 1;
            }

            return 0;
        }

        // --- Tab & Feature Visibility Rules ---

        public bool CanSeeCalendar()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false;
            return true;
        }

        public bool CanSeeClientsTab()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return true;
            int level = GetUserLevel();
            if (level == 4 || level == 1) return false;
            return level >= 5;
        }

        public bool CanSeeUpdateClientInfoOnly()
        {
            return IsClient();
        }

        public bool CanSeeJobRequestsTab()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return true;
            int level = GetUserLevel();
            if (level < 5) return false;
            return true;
        }

        public bool CanSeeQuotationsTab()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return true;
            int level = GetUserLevel();
            if (level == 1 || level == 4) return false;
            return true;
        }

        public bool CanSeeCreateEditQuote()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false;
            int level = GetUserLevel();
            if (level == 1 || level == 4) return false;
            return level == 6 || level == 5;
        }

        public bool CanSeeViewQuoteOnly()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return true;
            int level = GetUserLevel();
            if (level == 1 || level == 4) return false;
            return level == 5 || level == 6;
        }

        public bool CanSeeAllocationTab()
        {
            if (!IsLoggedIn()) return false;
            return true;
        }

        public bool CanSeeStaffAllocationOnly()
        {
            if (IsClient()) return true;
            return true;
        }

        public bool CanSeeJobsTab()
        {
            if (!IsLoggedIn()) return false;
            return true;
        }

        public bool CanSeeAddEditJobDetails()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false;
            int level = GetUserLevel();
            return level >= 4;
        }

        public bool CanSeeViewJobProgress()
        {
            if (!IsLoggedIn()) return false;
            return true;
        }

        public bool CanSeeEditJobProgress()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false;
            int level = GetUserLevel();
            if (level == 1) return false;
            return level >= 4;
        }

        public bool CanSeeInvoicingTab()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return true;
            int level = GetUserLevel();
            if (level == 1 || level == 4) return false;
            return true;
        }

        public bool CanSeeCreateInvoice()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false;
            int level = GetUserLevel();
            return level == 6;
        }

        public bool CanSeeManageMakePaymentsOnly()
        {
            return true;
        }

        public bool CanSeeReports()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false;
            int level = GetUserLevel();
            return level == 6;
        }
    }
}