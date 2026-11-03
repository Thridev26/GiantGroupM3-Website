<%@ Page Title="Client Portal" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ClientPortal.aspx.cs" Inherits="GiantGroupM3.ClientPortal" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .cp-wrap { max-width: 1140px; margin: 0 auto; padding-bottom: 1rem; }
        .cp-date { font-size: .9rem; color: #a9cdb9; margin-bottom: .5rem; font-weight: 500; }
        .cp-banner h1 { font-size: clamp(1.8rem, 3.6vw, 2.6rem); margin-bottom: .6rem; }
        .cp-banner p { max-width: 560px; margin: 0; line-height: 1.6; }

        /* Process steps: a real sequence, so numbered */
        .cp-steps { display: grid; grid-template-columns: repeat(4, 1fr); gap: 0; padding: 1.5rem 0; }
        .cp-step { position: relative; padding: 0 1.5rem; }
        .cp-step + .cp-step { border-left: 1px solid var(--gg-border); }
        .cp-step-n {
            width: 30px; height: 30px; border-radius: 50%; margin-bottom: .75rem;
            display: inline-flex; align-items: center; justify-content: center;
            font-weight: 700; font-size: .85rem;
            background: var(--gg-accent-soft); color: var(--gg-accent);
        }
        .cp-step strong { display: block; font-size: .95rem; color: var(--gg-text); }
        .cp-step span { font-size: .84rem; color: var(--gg-muted); line-height: 1.5; display: block; margin-top: .2rem; }

        /* Action cards */
        .cp-action { padding: 1.75rem; display: flex; gap: 1.25rem; align-items: flex-start; transition: border-color .2s ease; }
        .cp-action:hover { border-color: var(--gg-500); }
        .cp-action h3 { font-size: 1.1rem; font-weight: 700; margin: 0 0 .4rem; color: var(--gg-text); }
        .cp-action p { font-size: .92rem; line-height: 1.6; color: var(--gg-muted); margin: 0 0 1.1rem; }

        /* Side panels */
        .cp-panel-head { display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.1rem; border-bottom: 1px solid var(--gg-border); }
        .cp-panel-head h3 { margin: 0; font-size: 1rem; font-weight: 700; color: var(--gg-text); }
        .cp-panel-head a { font-size: .85rem; font-weight: 600; color: var(--gg-accent); text-decoration: none; }
        .cp-empty { padding: 1.75rem 1.25rem; text-align: center; }
        .cp-empty p { color: var(--gg-muted); font-size: .92rem; margin-bottom: 1rem; }
        .cp-links a { display: flex; align-items: center; gap: .75rem; padding: .8rem 1.1rem; color: var(--gg-text) !important; text-decoration: none !important; font-size: .92rem; font-weight: 500; border-bottom: 1px solid var(--gg-border); transition: background-color .15s ease; }
        .cp-links a:last-child { border-bottom: 0; }
        .cp-links a:hover { background: var(--gg-surface-2); }
        .cp-links a i:first-child { color: var(--gg-accent); font-size: 1.1rem; }
        .cp-links a i:last-child { margin-left: auto; color: var(--gg-muted); font-size: .85rem; }

        @media (max-width: 991.98px) {
            .cp-steps { grid-template-columns: repeat(2, 1fr); row-gap: 1.5rem; }
            .cp-step:nth-child(3) { border-left: 0; }
        }
        @media (max-width: 575.98px) {
            .cp-steps { grid-template-columns: 1fr; }
            .cp-step + .cp-step { border-left: 0; border-top: 1px solid var(--gg-border); padding-top: 1.25rem; }
            .cp-action { flex-direction: column; }
        }
    </style>

    <div class="cp-wrap my-4 px-lg-2">

        <!-- Header Banner -->
        <div class="gg-banner cp-banner mb-4 d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3">
            <div>
                <div class="cp-date"><asp:Literal ID="litToday" runat="server" /></div>
                <h1>Welcome, <asp:Literal ID="litClientName" runat="server" Text="Valued Client" Mode="Encode" />!</h1>
                <p>Track your property service requests, review estimates, and schedule site work seamlessly.</p>
            </div>
            <div class="flex-shrink-0">
                <a runat="server" href="~/Requests/SubmitRequest.aspx" class="gg-btn gg-btn-light"><i class="bi bi-plus-lg"></i>New Request</a>
            </div>
        </div>

        <!-- Error notice (only shown if the database could not be reached) -->
        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="mb-4">
            <div class="gg-alert" role="alert">
                <i class="bi bi-exclamation-triangle"></i>
                <span>We couldn't load your latest figures just now. Please refresh the page, or contact support if this keeps happening.</span>
            </div>
        </asp:Panel>

        <!-- Quick Status Overview -->
        <div class="row g-3 mb-4">
            <div class="col-md-4">
                <div class="gg-kpi gg-surface">
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="gg-kpi-label">Active Requests</span>
                        <span class="gg-tile"><i class="bi bi-hourglass-split"></i></span>
                    </div>
                    <div class="gg-kpi-value"><asp:Literal ID="litActiveRequests" runat="server" Text="0" /></div>
                    <div class="gg-kpi-note">Currently processing</div>
                </div>
            </div>

            <div class="col-md-4">
                <div class="gg-kpi gg-surface is-amber">
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="gg-kpi-label">Quotes Ready</span>
                        <span class="gg-tile is-amber"><i class="bi bi-file-earmark-text"></i></span>
                    </div>
                    <div class="gg-kpi-value"><asp:Literal ID="litQuotesReady" runat="server" Text="0" /></div>
                    <div class="gg-kpi-note" style="color: var(--gg-amber); font-weight: 600;">Awaiting your approval</div>
                </div>
            </div>

            <div class="col-md-4">
                <div class="gg-kpi gg-surface is-blue">
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="gg-kpi-label">Approved Jobs</span>
                        <span class="gg-tile is-blue"><i class="bi bi-check2-circle"></i></span>
                    </div>
                    <div class="gg-kpi-value"><asp:Literal ID="litCompletedJobs" runat="server" Text="0" /></div>
                    <div class="gg-kpi-note">Quotes signed &amp; approved</div>
                </div>
            </div>
        </div>

        <!-- How it works -->
        <div class="gg-surface cp-steps mb-5" aria-label="How a request moves through our process">
            <div class="cp-step">
                <span class="cp-step-n">1</span>
                <strong>Submit a request</strong>
                <span>Describe the job and attach photos of the site.</span>
            </div>
            <div class="cp-step">
                <span class="cp-step-n">2</span>
                <strong>Site evaluation</strong>
                <span>Our team sets a date to inspect the work.</span>
            </div>
            <div class="cp-step">
                <span class="cp-step-n">3</span>
                <strong>Review your quote</strong>
                <span>Approve the quote once it has been sent to you.</span>
            </div>
            <div class="cp-step">
                <span class="cp-step-n">4</span>
                <strong>Work is scheduled</strong>
                <span>Crews and equipment are allocated to your site.</span>
            </div>
        </div>

        <!-- Client Action Modules -->
        <div class="row g-4">
            <div class="col-lg-7">
                <h2 class="gg-section-title mb-3">Service Actions</h2>

                <div class="d-flex flex-column gap-3">
                    <div class="cp-action gg-surface">
                        <span class="gg-tile is-lg"><i class="bi bi-pencil-square"></i></span>
                        <div>
                            <h3>Submit New Job Request</h3>
                            <p>Need tree removal, branch trimming, or site clearing? Submit photos and details directly to our field team for immediate review.</p>
                            <a runat="server" href="~/Requests/SubmitRequest.aspx" class="gg-btn gg-btn-primary gg-btn-arrow">Request Service</a>
                        </div>
                    </div>

                    <div class="cp-action gg-surface">
                        <span class="gg-tile is-lg"><i class="bi bi-images"></i></span>
                        <div>
                            <h3>Manage Requests &amp; Site Photos</h3>
                            <p>Cancel eligible requests and add, view or remove BEFORE site photos for your service requests.</p>
                            <a runat="server" href="~/Requests/ViewRequests.aspx" class="gg-btn gg-btn-outline gg-btn-arrow">Manage My Requests</a>
                        </div>
                    </div>

                    <div class="cp-action gg-surface">
                        <span class="gg-tile is-lg"><i class="bi bi-clock-history"></i></span>
                        <div>
                            <h3>Request &amp; Job History</h3>
                            <p>See the latest recorded status of your active service requests and jobs in one place.</p>
                            <a runat="server" href="~/ClientJobProgress.aspx" class="gg-btn gg-btn-outline gg-btn-arrow">View Job Progress</a>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-lg-5">
                <h2 class="gg-section-title mb-3">Recent requests</h2>

                <div class="gg-surface mb-4">
                    <asp:Repeater ID="rptRecent" runat="server">
                        <HeaderTemplate>
                            <div class="cp-panel-head">
                                <h3>Latest activity</h3>
                                <a runat="server" href="~/ClientJobProgress.aspx">View all</a>
                            </div>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <a runat="server" href="~/ClientJobProgress.aspx" class="gg-row">
                                <div>
                                    <div class="gg-row-title">Request #<%#: Eval("jobRequestID") %></div>
                                    <div class="gg-row-sub">Open to see details and quotes</div>
                                </div>
                                <span class="gg-chip <%# StatusClass(Eval("status")) %>"><%#: Eval("status") %></span>
                            </a>
                        </ItemTemplate>
                    </asp:Repeater>

                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="cp-empty">
                        <span class="gg-tile is-lg mb-3"><i class="bi bi-inbox"></i></span>
                        <p>You haven't submitted a request yet. Start one and we'll arrange a site evaluation.</p>
                        <a runat="server" href="~/Requests/SubmitRequest.aspx" class="gg-btn gg-btn-primary">Submit your first request</a>
                    </asp:Panel>
                </div>

                <div class="gg-surface cp-links">
                    <div class="cp-panel-head"><h3>Need something else?</h3></div>
                    <a runat="server" href="~/RequestQuote.aspx"><i class="bi bi-file-earmark-plus"></i>Request a quote<i class="bi bi-chevron-right"></i></a>
                    <a runat="server" href="~/ClientProfile.aspx"><i class="bi bi-person-gear"></i>Update your details<i class="bi bi-chevron-right"></i></a>
                    <a runat="server" href="~/Help.aspx"><i class="bi bi-question-circle"></i>Read the FAQ<i class="bi bi-chevron-right"></i></a>
                    <a runat="server" href="~/Contact.aspx"><i class="bi bi-headset"></i>Contact support<i class="bi bi-chevron-right"></i></a>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
