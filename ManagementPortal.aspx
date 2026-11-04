<%@ Page Title="Management Portal" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ManagementPortal.aspx.cs" Inherits="GiantGroupM3.ManagementPortal" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .mp-wrap { max-width: 1140px; margin: 0 auto; padding-bottom: 1rem; }
        .mp-date { font-size: .9rem; color: #a9cdb9; margin-bottom: .5rem; font-weight: 500; }
        .mp-banner h1 { font-size: clamp(1.8rem, 3.6vw, 2.6rem); margin-bottom: .6rem; }
        .mp-banner p { max-width: 560px; margin: 0; line-height: 1.6; }
        .mp-role { display: inline-flex; align-items: center; gap: .45rem; font-size: .82rem; font-weight: 600; padding: .35rem .8rem; border-radius: 999px; background: rgba(255,255,255,.12); border: 1px solid rgba(255,255,255,.2); color: #fff; }
        .mp-level { display: inline-flex; align-items: center; gap: .5rem; padding: .55rem 1rem; border-radius: 12px; font-size: .88rem; font-weight: 600; background: rgba(255,255,255,.12); border: 1px solid rgba(255,255,255,.25); color: #fff; }

        /* Primary hub (calendar) */
        .mp-hub { display: flex; align-items: center; justify-content: space-between; gap: 1.5rem; padding: 1.75rem 2rem; }
        .mp-hub-main { display: flex; align-items: center; gap: 1.25rem; }
        .mp-hub h3 { margin: 0 0 .35rem; font-size: 1.15rem; font-weight: 700; color: var(--gg-text); }
        .mp-hub p { margin: 0; color: var(--gg-muted); font-size: .92rem; line-height: 1.6; max-width: 620px; }

        /* Module tiles */
        .mp-tile { display: flex; gap: 1rem; align-items: center; padding: 1.1rem 1.25rem; height: 100%; text-decoration: none !important; color: var(--gg-text) !important; transition: border-color .2s ease; }
        .mp-tile:hover { border-color: var(--gg-500); }
        .mp-tile strong { display: block; font-size: .98rem; }
        .mp-tile span.sub { font-size: .82rem; color: var(--gg-muted); }
        .mp-tile .chev { margin-left: auto; color: var(--gg-muted); font-size: .9rem; }

        /* Attention list */
        .mp-panel-head { display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.1rem; border-bottom: 1px solid var(--gg-border); }
        .mp-panel-head h3 { margin: 0; font-size: 1rem; font-weight: 700; color: var(--gg-text); }
        .mp-panel-head a { font-size: .85rem; font-weight: 600; color: var(--gg-accent); text-decoration: none; }
        .mp-empty { padding: 1.75rem 1.25rem; text-align: center; color: var(--gg-muted); font-size: .92rem; }

        @media (max-width: 767.98px) {
            .mp-hub { flex-direction: column; align-items: flex-start; padding: 1.5rem; }
        }
    </style>

    <div class="mp-wrap my-4 px-lg-2">

        <!-- Hero Banner -->
        <div class="gg-banner mp-banner mb-4 d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3">
            <div>
                <div class="mp-date"><asp:Literal ID="litToday" runat="server" /></div>
                <span class="mp-role mb-3"><i class="bi bi-shield-check"></i>Staff Operations Hub</span>
                <h1>Welcome back, <asp:Literal ID="litStaffName" runat="server" Mode="Encode">Manager</asp:Literal>!</h1>
                <p>View operational schedules, manage site timelines, and handle team allocations.</p>
            </div>
            <div class="flex-shrink-0">
                <span class="mp-level"><i class="bi bi-person-badge"></i>User Level: <asp:Literal ID="litUserLevel" runat="server">6</asp:Literal></span>
            </div>
        </div>

        <!-- Error notice (only shown if the database could not be reached) -->
        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="mb-4">
            <div class="gg-alert" role="alert">
                <i class="bi bi-exclamation-triangle"></i>
                <span>Some dashboard figures could not be loaded. Please refresh the page. If the problem continues, check the database connection.</span>
            </div>
        </asp:Panel>

        <!-- Metric Summary Cards -->
        <div class="row g-3 mb-5">
            <div class="col-sm-6 col-xl-3">
                <div class="gg-kpi gg-surface is-amber">
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="gg-kpi-label">Pending Requests</span>
                        <span class="gg-tile is-amber"><i class="bi bi-hourglass-split"></i></span>
                    </div>
                    <div class="gg-kpi-value"><asp:Literal ID="litPendingRequests" runat="server">0</asp:Literal></div>
                    <div class="gg-kpi-note" style="color: var(--gg-amber); font-weight: 600;">Needs quotation</div>
                </div>
            </div>

            <div class="col-sm-6 col-xl-3">
                <div class="gg-kpi gg-surface">
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="gg-kpi-label">Scheduled Jobs</span>
                        <span class="gg-tile"><i class="bi bi-gear-wide-connected"></i></span>
                    </div>
                    <div class="gg-kpi-value"><asp:Literal ID="litScheduledJobs" runat="server">0</asp:Literal></div>
                    <div class="gg-kpi-note" style="color: var(--gg-accent); font-weight: 600;">Active in field</div>
                </div>
            </div>

            <div class="col-sm-6 col-xl-3">
                <div class="gg-kpi gg-surface is-blue">
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="gg-kpi-label">Quotes Awaiting Approval</span>
                        <span class="gg-tile is-blue"><i class="bi bi-file-earmark-text"></i></span>
                    </div>
                    <div class="gg-kpi-value"><asp:Literal ID="litPendingQuotes" runat="server">0</asp:Literal></div>
                    <div class="gg-kpi-note" style="color: var(--gg-blue); font-weight: 600;">Sent to clients</div>
                </div>
            </div>

            <div class="col-sm-6 col-xl-3">
                <div class="gg-kpi gg-surface" style="--gg-500: var(--gg-muted);">
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="gg-kpi-label">Total Active Clients</span>
                        <span class="gg-tile" style="background: var(--gg-surface-2); color: var(--gg-muted);"><i class="bi bi-people"></i></span>
                    </div>
                    <div class="gg-kpi-value"><asp:Literal ID="litActiveClients" runat="server">0</asp:Literal></div>
                    <div class="gg-kpi-note">Registered accounts</div>
                </div>
            </div>
        </div>

        <h2 class="gg-section-title mb-3">Operations Control</h2>

        <!-- Primary Hub Feature Card -->
        <div class="mp-hub gg-surface mb-4">
            <div class="mp-hub-main">
                <span class="gg-tile is-lg"><i class="bi bi-calendar3"></i></span>
                <div>
                    <h3>Schedule &amp; Team Allocations</h3>
                    <p>Access active felling schedules, assign field crews, track team allocations, and oversee daily operational timelines across sites.</p>
                </div>
            </div>
            <div class="flex-shrink-0">
                <asp:HyperLink ID="lnkCalendar" runat="server" NavigateUrl="~/Calendar.aspx" CssClass="gg-btn gg-btn-primary gg-btn-arrow">Open Operational Calendar</asp:HyperLink>
            </div>
        </div>

        <div class="row g-4 mb-5">
            <!-- Requests needing a quote -->
            <div class="col-lg-5 order-lg-2">
                <div class="gg-surface">
                    <asp:Repeater ID="rptAttention" runat="server">
                        <HeaderTemplate>
                            <div class="mp-panel-head">
                                <h3>Waiting for a quote</h3>
                                <a runat="server" href="~/Requests/ManageRequests.aspx">Manage</a>
                            </div>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <a runat="server" href="~/Requests/ManageRequests.aspx" class="gg-row">
                                <div>
                                    <div class="gg-row-title">Request #<%#: Eval("jobRequestID") %></div>
                                    <div class="gg-row-sub">Open to review and prepare a quote</div>
                                </div>
                                <span class="gg-chip <%# StatusClass(Eval("status")) %>"><%#: Eval("status") %></span>
                            </a>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlAttentionEmpty" runat="server" Visible="false" CssClass="mp-empty">
                        <span class="gg-tile is-lg mb-3"><i class="bi bi-check2-all"></i></span>
                        <div>You're all caught up. No requests are waiting for a quote.</div>
                    </asp:Panel>
                </div>
            </div>

            <!-- Quick access (shown according to staff level) -->
            <div class="col-lg-7 order-lg-1">
                <div class="row g-3">
                    <% if (ShowQuotes) { %>
                    <div class="col-sm-6">
                        <a runat="server" href="~/ViewQuote.aspx" class="mp-tile gg-surface">
                            <span class="gg-tile is-blue"><i class="bi bi-file-earmark-text"></i></span>
                            <div><strong>Quotations</strong><span class="sub">Review and edit quotes</span></div>
                            <i class="bi bi-chevron-right chev"></i>
                        </a>
                    </div>
                    <% } %>
                    <% if (ShowAllocation) { %>
                    <div class="col-sm-6">
                        <a runat="server" href="~/ViewStaffAllocation.aspx" class="mp-tile gg-surface">
                            <span class="gg-tile"><i class="bi bi-person-badge"></i></span>
                            <div><strong>Staff allocation</strong><span class="sub">Who is on which job</span></div>
                            <i class="bi bi-chevron-right chev"></i>
                        </a>
                    </div>
                    <div class="col-sm-6">
                        <a runat="server" href="~/ViewAssetAllocation.aspx" class="mp-tile gg-surface">
                            <span class="gg-tile is-amber"><i class="bi bi-tools"></i></span>
                            <div><strong>Asset allocation</strong><span class="sub">Machines and equipment</span></div>
                            <i class="bi bi-chevron-right chev"></i>
                        </a>
                    </div>
                    <% } %>
                    <% if (ShowJobs) { %>
                    <div class="col-sm-6">
                        <a runat="server" href="~/Jobs/Progress.aspx" class="mp-tile gg-surface">
                            <span class="gg-tile"><i class="bi bi-graph-up-arrow"></i></span>
                            <div><strong>Job progress</strong><span class="sub">Track jobs in the field</span></div>
                            <i class="bi bi-chevron-right chev"></i>
                        </a>
                    </div>
                    <% } %>
                    <% if (ShowInvoicing) { %>
                    <div class="col-sm-6">
                        <a runat="server" href="~/Invoicing/Payments.aspx" class="mp-tile gg-surface">
                            <span class="gg-tile is-blue"><i class="bi bi-credit-card"></i></span>
                            <div><strong>Invoicing</strong><span class="sub">Invoices and payments</span></div>
                            <i class="bi bi-chevron-right chev"></i>
                        </a>
                    </div>
                    <% } %>
                    <% if (ShowReports) { %>
                    <div class="col-sm-6">
                        <a runat="server" href="~/reports/ReportAnalytics.aspx" class="mp-tile gg-surface">
                            <span class="gg-tile"><i class="bi bi-bar-chart-line"></i></span>
                            <div><strong>Reports</strong><span class="sub">Business performance</span></div>
                            <i class="bi bi-chevron-right chev"></i>
                        </a>
                    </div>
                    <% } %>
                </div>
            </div>
        </div>
    </div>
</asp:Content>