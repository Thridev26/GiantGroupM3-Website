using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GiantGroupM3.Services
{
    /// <summary>
    /// Client-facing emails for every step of a client's journey, plus the activity log (the paper trail).
    ///
    /// Each public method:
    ///  - never throws: a failed email or log write must never undo the action that triggered it;
    ///  - is de-duplicated by an event key, so a double click, a retried PayFast notification or a
    ///    second save cannot send the same email twice;
    ///  - writes what happened to dbo.ClientActivity (Sent, Failed or Skipped).
    ///
    /// Emails go through the Google Apps Script mailer, which only accepts requests that carry the
    /// NotificationSecret from Web.config. Links are built from SiteBaseUrl in Web.config.
    /// </summary>
    public static class ClientNotifications
    {
        private const int TimeoutMs = 15000;
        private static readonly CultureInfo Za = CultureInfo.GetCultureInfo("en-ZA");

        // ==================================================================
        //  Request lifecycle
        // ==================================================================

        /// <summary>The client submitted a request (website or mobile). Call after the request is committed.</summary>
        public static void RequestSubmitted(string connString, int requestId, HttpRequest request)
        {
            Run("RequestSubmitted", requestId, () =>
            {
                Recipient r = LoadByRequest(connString, requestId);
                if (r == null) return;

                Mail m = new Mail
                {
                    Subject = "We have received your request (Request " + requestId + ")",
                    HeaderTitle = "Request Received",
                    Subtitle = "The Giant Group Client Update",
                    ButtonText = "View My Requests",
                    ButtonPath = "/Requests/ViewRequests.aspx",
                    Footnote = "You are receiving this email because a service request was made under your account."
                };
                m.Paragraphs.Add("Thank you. We have received your service request and our team will review it shortly.");
                m.Paragraphs.Add("We will email you as soon as a site evaluation date has been set.");
                m.Details.Add(Row("Request number", requestId));
                m.Details.Add(Row("Site address", r.Address));
                m.Details.Add(Row("Services requested", JoinServices(connString, requestId)));
                m.Details.Add(Row("Urgency", r.Urgency));

                Dispatch(connString, r, "RequestSubmitted", "submitted:" + requestId, m, request);
            });
        }

        /// <summary>A site evaluation date was set for the request. Call after the date is saved.</summary>
        public static void EvaluationScheduled(string connString, int requestId, HttpRequest request)
        {
            Run("EvaluationScheduled", requestId, () =>
            {
                Recipient r = LoadByRequest(connString, requestId);
                if (r == null || !r.EvaluationDate.HasValue) return;

                string when = r.EvaluationDate.Value.ToString("dd MMMM yyyy", Za);
                Mail m = new Mail
                {
                    Subject = "Site evaluation scheduled for " + when + " (Request " + requestId + ")",
                    HeaderTitle = "Site Evaluation Scheduled",
                    Subtitle = "The Giant Group Client Update",
                    ButtonText = "View My Requests",
                    ButtonPath = "/Requests/ViewRequests.aspx",
                    Footnote = "You are receiving this email because a service request was made under your account."
                };
                m.Paragraphs.Add("Our team is scheduled to visit your site on " + when + ".");
                m.Paragraphs.Add("Please make sure the site is accessible on the day. We will let you know once the evaluation has been completed.");
                m.Details.Add(Row("Request number", requestId));
                m.Details.Add(Row("Site address", r.Address));
                m.Details.Add(Row("Evaluation date", when));

                Dispatch(connString, r, "EvaluationScheduled",
                    "evalscheduled:" + requestId + ":" + r.EvaluationDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    m, request);
            });
        }

        /// <summary>The site was evaluated and the client can now request a quote. Call after the status changes.</summary>
        public static void SiteEvaluated(string connString, int requestId, HttpRequest request)
        {
            Run("SiteEvaluated", requestId, () =>
            {
                Recipient r = LoadByRequest(connString, requestId);
                if (r == null) return;

                Mail m = new Mail
                {
                    Subject = "Your site evaluation is complete - you can now request a quote",
                    HeaderTitle = "Site Evaluation Complete",
                    Subtitle = "The Giant Group Client Update",
                    ButtonText = "Request a Quote",
                    ButtonPath = "/RequestQuote.aspx",
                    Footnote = "You are receiving this email because a service request was made under your account."
                };
                m.Paragraphs.Add("Good news. Our team has completed the site evaluation for your request.");
                m.Paragraphs.Add("You can now request a quote for the work. Log in to your account and select Request a Quote to continue.");
                m.Details.Add(Row("Request number", requestId));
                m.Details.Add(Row("Site address", r.Address));
                m.Details.Add(Row("Evaluation date", r.EvaluationDate.HasValue
                    ? r.EvaluationDate.Value.ToString("dd MMMM yyyy", Za) : null));

                Dispatch(connString, r, "SiteEvaluated", "evaluated:" + requestId, m, request);
            });
        }

        /// <summary>
        /// A quote was generated for the request. The breakdown comes from the calculation that produced it,
        /// so the email and the PDF show the same figures. Call after the quote is committed.
        /// </summary>
        public static void QuoteReady(string connString, int requestId, HttpRequest request,
                                      decimal services, decimal travel, decimal vat, decimal total)
        {
            Run("QuoteReady", requestId, () =>
            {
                Recipient r = LoadByRequest(connString, requestId);
                QuoteInfo q = LoadLatestQuote(connString, requestId);
                if (r == null || q == null) return;
                r.QuoteId = q.Id;

                Mail m = new Mail
                {
                    Subject = "Your quote is ready - Request " + requestId,
                    HeaderTitle = "Your Quote Is Ready",
                    Subtitle = "The Giant Group Quotations",
                    ButtonText = "View My Quote",
                    ButtonPath = "/ViewQuote.aspx",
                    Footnote = "Quotes are valid until the date shown. A PDF copy is attached. If you have any questions, please contact us.",
                    AttachPdf = true,
                    PdfTitle = "Quotation",
                    PdfReference = "Quote " + q.Id,
                    PdfFileName = "Quote-" + q.Id + ".pdf"
                };
                m.Paragraphs.Add("Thank you for your patience. Your quote for the work you requested is now ready.");
                m.Paragraphs.Add("Please review the breakdown below. You can view the full quote and respond from your account.");
                m.Details.Add(Row("Quote number", q.Id));
                m.Details.Add(Row("Request number", requestId));
                m.Details.Add(Row("Site address", r.Address));
                m.Details.Add(Row("Services", Money(services)));
                m.Details.Add(Row("Travel", Money(travel)));
                m.Details.Add(Row("VAT", Money(vat)));
                m.Details.Add(Row("Total (incl. VAT)", Money(total)));
                m.Details.Add(Row("Valid until", ShortDate(q.Expiry)));

                Dispatch(connString, r, "QuoteReady", "quote:" + q.Id, m, request);
            });
        }

        /// <summary>The client accepted or declined a quote on the website. Call after the change is committed.</summary>
        public static void QuoteResponded(string connString, int quoteId, HttpRequest request)
        {
            Run("QuoteResponded", quoteId, () =>
            {
                QuoteInfo q = LoadQuote(connString, quoteId);
                Recipient r = LoadByQuote(connString, quoteId);
                if (q == null || r == null) return;

                if (q.Status == "Accepted") SendQuoteAccepted(connString, r, q, request, true);
                else if (q.Status == "Rejected") SendQuoteDeclined(connString, r, q, request, true);
            });
        }

        /// <summary>
        /// Management saved a quote (website or mobile). Tells the client if the quote was accepted, declined
        /// or re-issued with a new amount or expiry date. Saving the same values again sends nothing.
        /// </summary>
        public static void QuoteUpdatedByStaff(string connString, int quoteId, HttpRequest request)
        {
            Run("QuoteUpdatedByStaff", quoteId, () =>
            {
                QuoteInfo q = LoadQuote(connString, quoteId);
                Recipient r = LoadByQuote(connString, quoteId);
                if (q == null || r == null) return;

                if (q.Status == "Accepted") SendQuoteAccepted(connString, r, q, request, false);
                else if (q.Status == "Rejected") SendQuoteDeclined(connString, r, q, request, false);
                else if (q.Status == "Sent") SendQuoteReissued(connString, r, q, request);
            });
        }

        /// <summary>A job was created from an accepted quote. Call after the job is committed.</summary>
        public static void JobScheduled(string connString, int jobId, HttpRequest request)
        {
            Run("JobScheduled", jobId, () =>
            {
                Recipient r = LoadByJob(connString, jobId);
                JobInfo j = LoadJob(connString, jobId);
                if (r == null || j == null) return;

                Mail m = new Mail
                {
                    Subject = "Your job has been scheduled (Job " + jobId + ")",
                    HeaderTitle = "Job Scheduled",
                    Subtitle = "The Giant Group Client Update",
                    ButtonText = "View Job Progress",
                    ButtonPath = "/ClientJobProgress.aspx",
                    Footnote = "You are receiving this email because a job was booked under your account."
                };
                m.Paragraphs.Add("Your job has been scheduled with our team.");
                m.Paragraphs.Add("We will email you as the work progresses and when it is complete.");
                m.Details.Add(Row("Job number", jobId));
                m.Details.Add(Row("Request number", r.RequestId));
                m.Details.Add(Row("Site address", r.Address));
                m.Details.Add(Row("Start date", j.StartDate.HasValue ? ShortDate(j.StartDate) : "To be confirmed"));
                m.Details.Add(Row("Status", j.Status));

                Dispatch(connString, r, "JobScheduled", "jobcreated:" + jobId, m, request);
            });
        }

        /// <summary>
        /// A job's status changed. Only "In Progress" and "Completed" are emailed, to keep messages meaningful.
        /// Call after the status change is committed.
        /// </summary>
        public static void JobStatusChanged(string connString, int jobId, HttpRequest request)
        {
            Run("JobStatusChanged", jobId, () =>
            {
                JobInfo j = LoadJob(connString, jobId);
                if (j == null) return;
                Recipient r = LoadByJob(connString, jobId);
                if (r == null) return;

                Mail m = new Mail
                {
                    ButtonText = "View Job Progress",
                    ButtonPath = "/ClientJobProgress.aspx",
                    Footnote = "You are receiving this email because a job was booked under your account.",
                    Subtitle = "The Giant Group Client Update"
                };
                m.Details.Add(Row("Job number", jobId));
                m.Details.Add(Row("Request number", r.RequestId));
                m.Details.Add(Row("Site address", r.Address));
                m.Details.Add(Row("Status", j.Status));

                if (j.Status == "In Progress")
                {
                    m.Subject = "Work has started on your job (Job " + jobId + ")";
                    m.HeaderTitle = "Work Started";
                    m.Paragraphs.Add("Our crew has started work on your job.");
                    m.Paragraphs.Add("We will let you know as soon as it is complete.");
                }
                else if (j.Status == "Completed")
                {
                    m.Subject = "Your job is complete (Job " + jobId + ")";
                    m.HeaderTitle = "Job Completed";
                    m.Paragraphs.Add("Our team has completed the work on your job. Thank you for choosing The Giant Group.");
                    m.Paragraphs.Add("Your account shows your balance and payment options.");
                }
                else
                {
                    return;
                }

                Dispatch(connString, r, "JobStatusChanged", "jobstatus:" + jobId + ":" + j.Status, m, request);
            });
        }

                        // ==================================================================
        //  Quote helpers
        // ==================================================================

        /// <summary>
        /// A Systems Manager or Owner approved a quote that was waiting for review (status Not sent, now Sent).
        /// This is the first time the client hears about the quote, so they get the full "quote ready" email with the PDF.
        /// Call after the change is committed.
        /// </summary>
        public static void QuoteApproved(string connString, int quoteId, HttpRequest request)
        {
            Run("QuoteApproved", quoteId, () =>
            {
                QuoteInfo q = LoadQuote(connString, quoteId);
                Recipient r = LoadByQuote(connString, quoteId);
                if (q == null || r == null) return;

                Mail m = new Mail
                {
                    Subject = "Your quote is ready - Request " + r.RequestId,
                    HeaderTitle = "Your Quote Is Ready",
                    Subtitle = "The Giant Group Quotations",
                    ButtonText = "View My Quote",
                    ButtonPath = "/ViewQuote.aspx",
                    Footnote = "A PDF copy is attached. Quotes are valid until the date shown. If you have any questions, please contact us.",
                    AttachPdf = true,
                    PdfTitle = "Quotation",
                    PdfReference = "Quote " + q.Id,
                    PdfFileName = "Quote-" + q.Id + ".pdf"
                };
                m.Paragraphs.Add("Thank you for your patience. Your quote for the work you requested has been reviewed and is now ready.");
                m.Paragraphs.Add("You can view the quote in your account and accept it there.");
                m.Details.Add(Row("Quote number", q.Id));
                m.Details.Add(Row("Request number", r.RequestId));
                m.Details.Add(Row("Site address", r.Address));
                m.Details.Add(Row("Total (incl. VAT)", Money(q.Amount)));
                m.Details.Add(Row("Valid until", ShortDate(q.Expiry)));

                Dispatch(connString, r, "QuoteApproved", "quoteapproved:" + q.Id, m, request);
            });
        }

        private static void SendQuoteAccepted(string connString, Recipient r, QuoteInfo q, HttpRequest request, bool byClient)
        {
            Mail m = new Mail
            {
                Subject = byClient ? "Thank you - your quote has been accepted" : "Your quote has been accepted",
                HeaderTitle = "Quote Accepted",
                Subtitle = "The Giant Group Quotations",
                ButtonText = "View My Quote",
                ButtonPath = "/ViewQuote.aspx",
                Footnote = "We will keep you updated as your job moves forward."
            };
            m.Paragraphs.Add(byClient
                ? "Thank you for accepting quote " + q.Id + ". Our team will now schedule your job."
                : "Our team has recorded quote " + q.Id + " as accepted on your account. We will now schedule your job.");
            m.Details.Add(Row("Quote number", q.Id));
            m.Details.Add(Row("Request number", r.RequestId));
            m.Details.Add(Row("Total (incl. VAT)", Money(q.Amount)));

            Dispatch(connString, r, "QuoteAccepted", "quotestatus:" + q.Id + ":Accepted", m, request);
        }

        private static void SendQuoteDeclined(string connString, Recipient r, QuoteInfo q, HttpRequest request, bool byClient)
        {
            Mail m = new Mail
            {
                Subject = "Quote declined - Request " + r.RequestId,
                HeaderTitle = "Quote Declined",
                Subtitle = "The Giant Group Quotations",
                ButtonText = "Contact Us",
                ButtonPath = "/Contact.aspx",
                Footnote = "If this was a mistake, please contact us as soon as possible."
            };
            m.Paragraphs.Add(byClient
                ? "You have declined quote " + q.Id + ". If this was a mistake or your plans have changed, please contact us."
                : "Our team has closed quote " + q.Id + " as declined. Please contact us if you have any questions.");
            m.Details.Add(Row("Quote number", q.Id));
            m.Details.Add(Row("Request number", r.RequestId));

            Dispatch(connString, r, "QuoteDeclined", "quotestatus:" + q.Id + ":Rejected", m, request);
        }

        private static void SendQuoteReissued(string connString, Recipient r, QuoteInfo q, HttpRequest request)
        {
            Mail m = new Mail
            {
                Subject = "Your quote has been updated (Request " + r.RequestId + ")",
                HeaderTitle = "Quote Updated",
                Subtitle = "The Giant Group Quotations",
                ButtonText = "View My Quote",
                ButtonPath = "/ViewQuote.aspx",
                Footnote = "A PDF copy is attached. Quotes are valid until the date shown. If you have any questions, please contact us.",
                AttachPdf = true,
                PdfTitle = "Quotation",
                PdfReference = "Quote " + q.Id,
                PdfFileName = "Quote-" + q.Id + ".pdf"
            };
            m.Paragraphs.Add("Your quote has been updated. The current version is attached as a PDF and summarised below.");
            m.Details.Add(Row("Quote number", q.Id));
            m.Details.Add(Row("Request number", r.RequestId));
            m.Details.Add(Row("Site address", r.Address));
            m.Details.Add(Row("Total (incl. VAT)", Money(q.Amount)));
            m.Details.Add(Row("Valid until", ShortDate(q.Expiry)));

            string key = "quoteupdate:" + q.Id + ":" + q.Amount.ToString("F2", CultureInfo.InvariantCulture)
                       + ":" + (q.Expiry.HasValue ? q.Expiry.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "none");
            Dispatch(connString, r, "QuoteUpdated", key, m, request);
        }

        // ==================================================================
        //  Sending and the activity log
        // ==================================================================

        /// <summary>Checks for a duplicate, sends, then records the outcome.</summary>
        private static void Dispatch(string connString, Recipient r, string eventType, string eventKey, Mail m, HttpRequest request)
        {
            if (string.IsNullOrEmpty(r.Email))
            {
                Record(connString, r, eventType, eventKey, m.Subject, null, "Skipped", "The client account has no email address.");
                return;
            }

            if (AlreadySent(connString, eventKey)) return;

            string error;
            bool ok = Send(m, r, request, out error);
            Record(connString, r, eventType, eventKey, m.Subject, r.Email, ok ? "Sent" : "Failed", error);
        }

        private static bool Send(Mail m, Recipient r, HttpRequest request, out string error)
        {
            error = null;
            try
            {
                string url = ConfigurationManager.AppSettings["EmailServiceUrl"];
                string secret = ConfigurationManager.AppSettings["NotificationSecret"];
                string root = SiteRoot(request);

                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(root))
                {
                    error = "Email service, notification secret or site address is not configured.";
                    Trace.TraceError("ClientNotifications not sent (" + m.Subject + "): " + error);
                    return false;
                }

                JObject body = JObject.FromObject(new
                {
                    type = "notification",
                    secret = secret,
                    email = r.Email,
                    subject = m.Subject,
                    headerTitle = m.HeaderTitle,
                    subtitle = m.Subtitle,
                    greeting = "Hello " + r.FirstName + ",",
                    paragraphs = m.Paragraphs.ToArray(),
                    details = m.Details.ToArray(),
                    buttonText = m.ButtonText,
                    buttonUrl = m.ButtonPath == null ? null : root + m.ButtonPath,
                    footnote = m.Footnote,
                    attachPdf = m.AttachPdf,
                    pdfTitle = m.PdfTitle,
                    pdfReference = m.PdfReference,
                    pdfClient = r.FullName + (string.IsNullOrEmpty(r.Address) ? "" : "\n" + r.Address),
                    pdfFileName = m.PdfFileName
                });

                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                using (var web = new TimeoutWebClient(TimeoutMs))
                {
                    web.Encoding = System.Text.Encoding.UTF8;
                    web.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string reply = web.UploadString(url, "POST", body.ToString(Formatting.None));

                    // The script answers {"status":"success"} or {"status":"error", ...}
                    if (reply.IndexOf("\"error\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        error = reply;
                        Trace.TraceError("ClientNotifications rejected (" + m.Subject + "): " + reply);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Trace.TraceError("ClientNotifications send failed (" + m.Subject + "): " + ex.Message);
                return false;
            }
        }

        private static bool AlreadySent(string connString, string eventKey)
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM dbo.ClientActivity WHERE eventKey = @key AND emailStatus = 'Sent';";
                using (var conn = new SqlConnection(connString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@key", SqlDbType.VarChar, 200).Value = Truncate(eventKey, 200);
                    conn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
                }
            }
            catch (Exception ex)
            {
                // If the log table is missing, emails must still go out, so treat the event as not yet sent.
                Trace.TraceWarning("ClientNotifications could not check for duplicates (table missing?): " + ex.Message);
                return false;
            }
        }

        private static void Record(string connString, Recipient r, string eventType, string eventKey,
                                   string description, string emailTo, string status, string error)
        {
            try
            {
                const string sql = @"
                    INSERT INTO dbo.ClientActivity
                        (clientID, jobRequestID, quoteID, jobID, eventType, eventKey, description, emailTo, emailStatus, errorMessage)
                    VALUES
                        (@client, @request, @quote, @job, @type, @key, @desc, @to, @status, @error);";

                using (var conn = new SqlConnection(connString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@client", SqlDbType.Int).Value = (object)r.ClientId ?? DBNull.Value;
                    cmd.Parameters.Add("@request", SqlDbType.Int).Value = (object)r.RequestId ?? DBNull.Value;
                    cmd.Parameters.Add("@quote", SqlDbType.Int).Value = (object)r.QuoteId ?? DBNull.Value;
                    cmd.Parameters.Add("@job", SqlDbType.Int).Value = (object)r.JobId ?? DBNull.Value;
                    cmd.Parameters.Add("@type", SqlDbType.VarChar, 40).Value = Truncate(eventType, 40);
                    cmd.Parameters.Add("@key", SqlDbType.VarChar, 200).Value = Truncate(eventKey, 200);
                    cmd.Parameters.Add("@desc", SqlDbType.VarChar, 300).Value = (object)Truncate(description, 300) ?? DBNull.Value;
                    cmd.Parameters.Add("@to", SqlDbType.VarChar, 254).Value = (object)Truncate(emailTo, 254) ?? DBNull.Value;
                    cmd.Parameters.Add("@status", SqlDbType.VarChar, 20).Value = status;
                    cmd.Parameters.Add("@error", SqlDbType.VarChar, 500).Value = (object)Truncate(error, 500) ?? DBNull.Value;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClientNotifications could not record activity (" + eventType + "): " + ex.Message);
            }
        }

        /// <summary>Builds the site root, for example https://ist-wst-group-1.azurewebsites.net.</summary>
        public static string SiteRoot(HttpRequest request)
        {
            string configured = ConfigurationManager.AppSettings["SiteBaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim().TrimEnd('/');
            if (request == null) return null;

            // Azure terminates HTTPS in front of the app, so the scheme comes from X-Forwarded-Proto.
            string scheme = request.Headers["X-Forwarded-Proto"];
            scheme = string.IsNullOrEmpty(scheme) ? request.Url.Scheme : scheme.Split(',')[0].Trim();
            return scheme + "://" + request.Url.Authority;
        }

        // ==================================================================
        //  Loading the people and records behind each email
        // ==================================================================

        private static Recipient LoadByRequest(string connString, int requestId)
        {
            const string sql = @"
                SELECT jr.jobRequestID, jr.clientID, c.clientName, c.clientSurname, c.emailAddress,
                       jr.siteAddress, jr.urgencyLevel, jr.siteEvaluationDate
                FROM dbo.JobRequest jr
                INNER JOIN dbo.Client c ON c.clientID = jr.clientID
                WHERE jr.jobRequestID = @id;";

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = requestId;
                conn.Open();
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;

                    string first = ReadStr(rd, "clientName");
                    string last = ReadStr(rd, "clientSurname");
                    string full = ((first ?? "") + " " + (last ?? "")).Trim();

                    return new Recipient
                    {
                        RequestId = ReadInt(rd, "jobRequestID").Value,
                        ClientId = ReadInt(rd, "clientID") ?? 0,
                        FirstName = string.IsNullOrEmpty(first) ? "there" : first,
                        FullName = string.IsNullOrEmpty(full) ? "Client" : full,
                        Email = (ReadStr(rd, "emailAddress") ?? "").Trim(),
                        Address = ReadStr(rd, "siteAddress"),
                        Urgency = ReadStr(rd, "urgencyLevel"),
                        EvaluationDate = ReadDate(rd, "siteEvaluationDate")
                    };
                }
            }
        }

        private static Recipient LoadByQuote(string connString, int quoteId)
        {
            object o = Scalar(connString, "SELECT jobRequestID FROM dbo.Quote WHERE QuoteID = @id", quoteId);
            if (o == null) return null;
            Recipient r = LoadByRequest(connString, Convert.ToInt32(o, CultureInfo.InvariantCulture));
            if (r != null) r.QuoteId = quoteId;
            return r;
        }

        private static Recipient LoadByJob(string connString, int jobId)
        {
            const string sql = @"
                SELECT q.jobRequestID
                FROM dbo.Job j
                INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID
                WHERE j.jobID = @id;";
            object o = Scalar(connString, sql, jobId);
            if (o == null) return null;
            Recipient r = LoadByRequest(connString, Convert.ToInt32(o, CultureInfo.InvariantCulture));
            if (r != null) r.JobId = jobId;
            return r;
        }

        private static QuoteInfo LoadQuote(string connString, int quoteId)
        {
            return ReadQuote(connString,
                "SELECT QuoteID, amount, expiryDate, quoteStatus FROM dbo.Quote WHERE QuoteID = @id;", quoteId);
        }

        private static QuoteInfo LoadLatestQuote(string connString, int requestId)
        {
            return ReadQuote(connString,
                "SELECT TOP 1 QuoteID, amount, expiryDate, quoteStatus FROM dbo.Quote WHERE jobRequestID = @id ORDER BY QuoteID DESC;",
                requestId);
        }

        private static QuoteInfo ReadQuote(string connString, string sql, int id)
        {
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                conn.Open();
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    return new QuoteInfo
                    {
                        Id = ReadInt(rd, "QuoteID").Value,
                        Amount = ReadDec(rd, "amount"),
                        Expiry = ReadDate(rd, "expiryDate"),
                        Status = ReadStr(rd, "quoteStatus")
                    };
                }
            }
        }

        private static JobInfo LoadJob(string connString, int jobId)
        {
            const string sql = "SELECT jobID, startDate, jobStatus, quoteID FROM dbo.Job WHERE jobID = @id;";
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = jobId;
                conn.Open();
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    return new JobInfo
                    {
                        Id = ReadInt(rd, "jobID").Value,
                        StartDate = ReadDate(rd, "startDate"),
                        Status = ReadStr(rd, "jobStatus"),
                        QuoteId = ReadInt(rd, "quoteID")
                    };
                }
            }
        }

        private static string JoinServices(string connString, int requestId)
        {
            const string sql = @"
                SELECT jt.jobTypeName
                FROM dbo.RequestItem ri
                INNER JOIN dbo.JobType jt ON jt.jobTypeID = ri.jobTypeID
                WHERE ri.jobRequestID = @id
                ORDER BY jt.jobTypeName;";

            var names = new List<string>();
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = requestId;
                conn.Open();
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        string name = ReadStr(rd, "jobTypeName");
                        if (!string.IsNullOrEmpty(name)) names.Add(name);
                    }
                }
            }
            return names.Count == 0 ? null : string.Join(", ", names);
        }

        private static object Scalar(string connString, string sql, int id)
        {
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                conn.Open();
                object o = cmd.ExecuteScalar();
                return o == DBNull.Value ? null : o;
            }
        }

        private static decimal ScalarDecimal(string connString, string sql, int id)
        {
            object o = Scalar(connString, sql, id);
            return o == null ? 0m : Convert.ToDecimal(o, CultureInfo.InvariantCulture);
        }

        // ==================================================================
        //  Small helpers
        // ==================================================================

        private static void Run(string name, int id, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClientNotifications." + name + " failed for id " + id + ": " + ex);
            }
        }

        private static string[] Row(string label, object value)
        {
            string text = value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
            return new[] { label, string.IsNullOrWhiteSpace(text) ? "Not specified" : text };
        }

        private static string Money(decimal amount)
        {
            return "R " + amount.ToString("N2", CultureInfo.InvariantCulture);
        }

        private static string ShortDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd MMMM yyyy", Za) : "Not set";
        }

        private static string Truncate(string value, int max)
        {
            if (value == null) return null;
            return value.Length <= max ? value : value.Substring(0, max);
        }

        private static string ReadStr(IDataRecord rd, string column)
        {
            object o = rd[column];
            return o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static int? ReadInt(IDataRecord rd, string column)
        {
            object o = rd[column];
            return o == DBNull.Value ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static decimal ReadDec(IDataRecord rd, string column)
        {
            object o = rd[column];
            return o == DBNull.Value ? 0m : Convert.ToDecimal(o, CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadDate(IDataRecord rd, string column)
        {
            object o = rd[column];
            return o == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(o, CultureInfo.InvariantCulture);
        }

        /// <summary>WebClient has no timeout by default. A slow mail service must not hang a page or the PayFast callback.</summary>
        private sealed class TimeoutWebClient : WebClient
        {
            private readonly int _timeoutMs;

            public TimeoutWebClient(int timeoutMs)
            {
                _timeoutMs = timeoutMs;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request != null) request.Timeout = _timeoutMs;
                return request;
            }
        }

        private sealed class Recipient
        {
            public int RequestId;
            public int ClientId;
            public string FirstName;
            public string FullName;
            public string Email;
            public string Address;
            public string Urgency;
            public DateTime? EvaluationDate;
            public int? QuoteId;
            public int? JobId;
        }

        private sealed class QuoteInfo
        {
            public int Id;
            public decimal Amount;
            public DateTime? Expiry;
            public string Status;
        }

        private sealed class JobInfo
        {
            public int Id;
            public DateTime? StartDate;
            public string Status;
            public int? QuoteId;
        }

        private sealed class Mail
        {
            public string Subject;
            public string HeaderTitle;
            public string Subtitle;
            public string ButtonText;
            public string ButtonPath;
            public string Footnote;
            public bool AttachPdf;
            public string PdfTitle;
            public string PdfReference;
            public string PdfFileName;
            public readonly List<string> Paragraphs = new List<string>();
            public readonly List<string[]> Details = new List<string[]>();
        }
    }
}
