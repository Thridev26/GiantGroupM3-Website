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
    public partial class SiteMaster : MasterPage
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
            // so LoginView recognizes the state change on the very same redirect cycle
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
            // 6. FORCE CLEAR current user context immediately so UI methods re-evaluate
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

            // Allow staff OR clients to pass the login check
            if (Session["UserLevel"] == null && Session["StaffName"] == null && !IsClient())
            {
                return false;
            }

            return true;
        }

        public bool IsClient()
        {
            // Check all possible session keys your login code might populate
            if (Session["UserRole"] != null && Session["UserRole"].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (Session["Role"] != null && Session["Role"].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            // Added this to catch Session["UserType"] set during Login.aspx.cs
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
            // Check all possible session keys where user level might be stored
            string[] levelKeys = { "UserLevel", "Level", "User_Level", "StaffLevel", "AccessLevel" };

            foreach (var key in levelKeys)
            {
                if (Session[key] != null)
                {
                    // Handle if it's already an integer
                    if (Session[key] is int intVal)
                    {
                        return intVal;
                    }
                    // Handle if it's a string representation of a number
                    if (int.TryParse(Session[key].ToString(), out int parsedLevel))
                    {
                        return parsedLevel;
                    }
                }
            }

            // Fallback: If Teboho is logged in as staff but level is unmapped, 
            // force a default level of 1 so unassigned staff don't accidentally get admin rights!
            if (Session["StaffName"] != null || Session["Username"] != null)
            {
                return 1; // Treat unknown logged-in staff as Level 1 for safety
            }

            return 0;
        }

        // --- Tab & Feature Visibility Rules ---

        public bool CanSeeCalendar()
        {
            // If not logged in at all, return false
            if (!IsLoggedIn()) return false;

            // If the user is a client, return false
            if (IsClient()) return false;

            // If it's a logged-in staff member (meaning they are logged in and not a client), return true
            return true;
        }

        public bool CanSeeClientsTab()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return true; // Clients see the tab (for updating info)
            int level = GetUserLevel();
            // Level 4 and Level 1 and Level 5 should NOT see clients tab. Level 6 can see it.
            if (level == 4 || level == 1 || level == 5) return false;
            return level >= 6;
        }

        public bool CanSeeUpdateClientInfoOnly()
        {
            // Clients should only see "Update Client Info" under Clients tab
            return IsClient();
        }

        public bool CanSeeJobRequestsTab()
        {
            if (!IsLoggedIn()) return false;

            // Clients must ALWAYS be able to see the Job Requests tab
            if (IsClient()) return true;

            // Staff restrictions (Level 5 and above)
            int level = GetUserLevel();
            if (level < 6) return false;

            return true;
        }

        public bool CanSeeQuotationsTab()
        {
            if (!IsLoggedIn()) return false;

            // Clients are allowed to see it (for view-only access)
            if (IsClient()) return true;

            int level = GetUserLevel();

            // Level 1 and Level 4 staff members must NOT see the Quotations tab/words
            if (level == 1 || level == 4) return false;

            return true;
        }

        public bool CanSeeCreateEditQuote()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false; // Clients never see Create/Edit Quote

            int level = GetUserLevel();
            // Level 1 and 4 must NOT see the tab at all
            if (level == 1 || level == 4) return false;

            // Assuming higher admin levels (like level 6) get Create/Edit, 
            // while Level 5 is restricted to view-only based on your note.
            return level == 6 || level == 5; // Adjust if level 5
        }

        public bool CanSeeViewQuoteOnly()
        {
            if (!IsLoggedIn()) return false;


            // Clients and Level 5 should see the View Quote sub-tab, 
            // while higher levels (like Level 6) can also see everything.
            if (IsClient()) return true;

            int level = GetUserLevel();
            // Level 1 and 4 must NOT see the tab at all
            if (level == 1 || level == 4) return false;
            return level == 5 || level == 6;
        }

        public bool CanSeeAllocationTab()
        {
            if (!IsLoggedIn()) return false;
            return true; // Available, but sub-items controlled below
        }

        public bool CanSeeStaffAllocationOnly()
        {
            // All clients should only see view staff allocation under Allocation tab
            if (IsClient()) return true;
            return true; // Staff can see too, or adjust if needed
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
            return true; // View progress available to staff/clients who have job access
        }

        public bool CanSeeEditJobProgress()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false; // Clients cannot edit progress
            int level = GetUserLevel();
            if (level == 1) return false; // Level 1 cannot edit progress
            return level >= 4; // Adjust based on which staff levels can edit progress
        }

        public bool CanSeeInvoicingTab()
        {
            if (!IsLoggedIn()) return false;

            // Clients are allowed to see it
            if (IsClient()) return true;

            int level = GetUserLevel();

            // Level 1 and Level 4 staff members must NOT see the Invoicing tab/words
            if (level == 1 || level == 4 || level == 5) return false;

            return true;
        }

        public bool CanSeeCreateInvoice()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false;
            int level = GetUserLevel();
            return level == 6; // Level 6 only
        }

        public bool CanSeeManageMakePaymentsOnly()
        {
            // Clients should only see manage/make payments under invoice tab
            return true;
        }

        public bool CanSeeReports()
        {
            if (!IsLoggedIn()) return false;
            if (IsClient()) return false;
            int level = GetUserLevel();
            return level == 6; // Level 5 explicitly should NOT see reports tab anymore
        }
    }
}