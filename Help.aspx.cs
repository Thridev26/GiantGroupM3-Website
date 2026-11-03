using System;
using System.Collections.Generic;

namespace GiantGroupM3
{
    public class FaqItem
    {

        public string Icon { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }   // trusted, hard-coded HTML only
    }

    public partial class Help : System.Web.UI.Page
    {
       
        private enum Audience { Guest, Client, Staff }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            Audience audience = GetAudience();
            List<FaqItem> items;

            switch (audience)
            {
                case Audience.Staff:
                    lblHeading.Text = "Staff Help &amp; User Guide";
                    lblSubheading.Text = "How to manage requests, quotes, allocations and invoicing in the Giant Group portal.";
                    lblAudience.Text = "Showing the guide for: Management";
                    lblAudience.Visible = true;
                    items = StaffFaq();
                    break;

                case Audience.Client:
                    lblHeading.Text = "Client Help &amp; User Guide";
                    lblSubheading.Text = "Everything you need to request work, review quotes and follow your job.";
                    lblAudience.Text = "Showing the guide for: Client account";
                    lblAudience.Visible = true;
                    items = ClientFaq();
                    break;

                default:
                    lblHeading.Text = "Help &amp; FAQ";
                    lblSubheading.Text = "Quick answers about The Giant Group and how to get in touch.";
                    pnlGuestNotice.Visible = true;
                    items = PublicFaq();
                    break;
            }

            rptFaq.DataSource = items;
            rptFaq.DataBind();
        }

        /// <summary>Same session keys used across the portal: UserLevel 5/6 = management, ClientID = client.</summary>
        private Audience GetAudience()
        {
            if (Session["UserLevel"] != null
                && int.TryParse(Session["UserLevel"].ToString(), out int level)
                && (level == 5 || level == 6))
                return Audience.Staff;

            if (Session["ClientID"] != null
                && int.TryParse(Session["ClientID"].ToString(), out int clientId)
                && clientId > 0)
                return Audience.Client;

            return Audience.Guest;
        }

        private string Link(string path, string text)
        {
            return "<a href=\"" + ResolveUrl(path) + "\" class=\"text-success fw-bold text-decoration-none\">" + text + "</a>";
        }

        // ------------------------------------------------------------------
        //  Public: deliberately light. No menu names, no module names, no workflow detail.
        // ------------------------------------------------------------------
        private List<FaqItem> PublicFaq()
        {
            return new List<FaqItem>
            {
                new FaqItem {
                    Icon = "fa-tree",
                    Question = "How do I request a tree-felling service or quote?",
                    Answer = "Send us the details through our " + Link("~/Contact.aspx", "Contact page") + " or call us any time. Tell us what needs doing and where, and our team will get back to you promptly."
                },
                new FaqItem {
                    Icon = "fa-map-marker-alt",
                    Question = "Which areas do you service?",
                    Answer = "We are based in Queensburgh and proudly service Durban, KwaZulu-Natal and the surrounding areas. If you are unsure whether we cover your area, just ask."
                },
                new FaqItem {
                    Icon = "fa-comments",
                    Question = "Can I get quick answers without calling?",
                    Answer = "Yes. The chat assistant at the bottom right of every page can answer general questions about our services."
                },
                new FaqItem {
                    Icon = "fa-file-invoice-dollar",
                    Question = "How do quotes and payments work?",
                    Answer = "After we have assessed your request we will send you a formal quotation. Once you accept the quote you can pay online through PayFast from the Invoicing page, in full or in part."
                },
                new FaqItem {
                    Icon = "fa-user-shield",
                    Question = "Do I need an account?",
                    Answer = "You can browse and contact us without one. Account holders get access to extra features, and a full guide appears here once you " + Link("~/Account/Login.aspx", "sign in") + "."
                }
            };
        }

        // ------------------------------------------------------------------
        //  Signed-in client: detailed guidance on the private pages
        // ------------------------------------------------------------------
        private List<FaqItem> ClientFaq()
        {
            return new List<FaqItem>
            {
                new FaqItem {
                    Icon = "fa-tree",
                    Question = "How do I submit a new job request?",
                    Answer = "Go to " + Link("~/Requests/SubmitRequest.aspx", "Submit Job Request") + ", choose the services you need, enter the site address and pin the site on the map, then submit. Our team visits the site to take the measurements, and a quote can be prepared once they are recorded."
                },
                new FaqItem {
                    Icon = "fa-camera",
                    Question = "Can I send site photos?",
                    Answer = "Photo upload is not yet available on the website. Please send photos through the " + Link("~/Contact.aspx", "Contact page") + " and we will add them to your request."
                },
                new FaqItem {
                    Icon = "fa-calculator",
                    Question = "How do I get a quote for my request?",
                    Answer = "Open " + Link("~/RequestQuote.aspx", "Request Quote") + " and click <strong>Select Request</strong> next to your job. You will see your services, an estimated travel fee based on the site location, VAT and the total. Click <strong>Save &amp; Generate Quote</strong> to create it. Each request can only have one quote."
                },
                new FaqItem {
                    Icon = "fa-file-invoice-dollar",
                    Question = "Where do I review, accept or reject my quotes?",
                    Answer = "Open " + Link("~/ViewQuote.aspx", "View Quotes") + " and click <strong>Review</strong> on a quote. You can print it, then accept or reject it. A quote can only be answered while its status is <strong>Sent</strong> and before it expires, and your response can't be changed afterwards."
                },
                new FaqItem {
                    Icon = "fa-clock",
                    Question = "How long is a quote valid?",
                    Answer = "A quote is valid for one month from the date it was issued. The expiry date is shown on the quote and in your quotes list. If it has expired, please " + Link("~/Contact.aspx", "contact us") + " and we'll help."
                },
                new FaqItem {
                    Icon = "fa-chart-line",
                    Question = "How can I track the progress of my job?",
                    Answer = "Open the <strong>Jobs</strong> menu and select <strong>View Job Progress</strong> to see status updates and scheduled timelines for your site."
                },
                new FaqItem {
                    Icon = "fa-credit-card",
                    Question = "How do I view invoices and make payments?",
                    Answer = "Use the <strong>Invoicing</strong> menu to review issued bills and payment statuses, and <strong>Manage &amp; Make Payments</strong> to settle your account once the work is complete."
                },
                new FaqItem {
                    Icon = "fa-comments",
                    Question = "How does the chat assistant work?",
                    Answer = "The assistant at the bottom right of every page can answer questions about our services and help you find your way around the portal."
                },
                new FaqItem {
                    Icon = "fa-user-shield",
                    Question = "How is my account kept secure?",
                    Answer = "You only ever see your own requests, quotes and invoices. Always sign out on shared computers, and never share your password. If you think someone else has accessed your account, " + Link("~/Contact.aspx", "contact us") + " straight away."
                }
            };
        }

        // ------------------------------------------------------------------
        //  Signed-in management
        // ------------------------------------------------------------------
        private List<FaqItem> StaffFaq()
        {
            return new List<FaqItem>
            {
                new FaqItem {
                    Icon = "fa-calculator",
                    Question = "How do I generate a quote for a client's request?",
                    Answer = "Open " + Link("~/RequestQuote.aspx", "Request Quote") + " and select the request. Management sees all open requests with the client's name. Figures (services, travel, VAT, total) are always recalculated from the database when you save. Item details must exist and a request can only have one quote."
                },
                new FaqItem {
                    Icon = "fa-edit",
                    Question = "How do I edit or archive quotes?",
                    Answer = "In " + Link("~/ViewQuote.aspx", "View Quotes") + ", click <strong>Select/Edit</strong> to change the expiry date, amount or status. Use the status filter to find Sent, Accepted or Rejected quotes. Changing a quote's status also updates the job request, so you don't need to do both."
                },
                new FaqItem {
                    Icon = "fa-print",
                    Question = "How do clients receive the quote document?",
                    Answer = "Every quote has a printable page (the <strong>Print</strong> button) that can be saved as a PDF from the browser's print dialog. Clients can only open their own quotes."
                },
                new FaqItem {
                    Icon = "fa-tools",
                    Question = "How do I allocate equipment and staff to a job?",
                    Answer = "Use <strong>Assign Staff</strong> and <strong>Assign Asset</strong> to book people and machinery onto a job's dates and time slots. The system blocks any double-booking. Allocation is available to System Managers and Owners."
                },
                new FaqItem {
                    Icon = "fa-camera",
                    Question = "Where are site photos managed?",
                    Answer = "The website does not yet have a site photo upload page. Site photos are currently captured through the staff mobile app."
                },
                new FaqItem {
                    Icon = "fa-credit-card",
                    Question = "How do invoicing and payments work?",
                    Answer = "The <strong>Invoicing</strong> menu lists issued bills and payment statuses, and <strong>Manage &amp; Make Payments</strong> is where accounts are settled after the work is completed."
                },
                new FaqItem {
                    Icon = "fa-user-shield",
                    Question = "What can each role access?",
                    Answer = "Owners can see everything, including client records and invoicing. System Managers manage quotes and allocations. Operations Managers manage requests, site evaluations and the team calendar. Clients only see their own requests, quotes and invoices. Please sign out on shared computers."
                }
            };
        }
    }
}