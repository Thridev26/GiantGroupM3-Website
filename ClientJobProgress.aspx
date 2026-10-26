<%@ Page Title="Job Progress" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ClientJobProgress.aspx.cs" Inherits="GiantGroupM3.ClientJobProgress" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .progress-wrap { max-width: 1140px; margin:0 auto; }
        .progress-table { width:100%; border-collapse:collapse; }
        .progress-table th, .progress-table td { padding:.9rem 1rem; border-bottom:1px solid var(--gg-border); text-align:left; vertical-align:top; }
        .progress-table th { color:var(--gg-muted); font-size:.8rem; text-transform:uppercase; letter-spacing:.04em; }
        .progress-table tr:last-child td { border-bottom:0; }
        .progress-empty { text-align:center; padding:2rem 1rem; color:var(--gg-muted); }
        .progress-status { display:inline-block; padding:.28rem .65rem; border-radius:999px; background:var(--gg-surface-2); color:var(--gg-text); font-size:.82rem; font-weight:600; }
        .progress-note { color:var(--gg-muted); font-size:.9rem; }
        @media(max-width:760px) {
            .progress-table thead { display:none; }
            .progress-table, .progress-table tbody, .progress-table tr, .progress-table td { display:block; width:100%; }
            .progress-table tr { padding:.75rem 0; border-bottom:1px solid var(--gg-border); }
            .progress-table td { border:0; padding:.35rem 1rem; }
            .progress-table td:before { content:attr(data-label); display:block; font-size:.75rem; text-transform:uppercase; color:var(--gg-muted); margin-bottom:.15rem; }
        }
    </style>

    <div class="progress-wrap my-4 px-lg-2">
        <div class="gg-banner mb-4">
            <h1 class="mb-2">My Job Progress</h1>
            <p class="mb-0">View your scheduled jobs, dates, time slots and current statuses.</p>
        </div>

        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="gg-alert mb-3" role="alert">
            We couldn't load your job progress just now. Please refresh the page or contact support if the issue continues.
        </asp:Panel>

        <div class="gg-surface">
            <asp:Repeater ID="rptJobs" runat="server">
                <HeaderTemplate>
                    <div class="table-responsive">
                    <table class="progress-table">
                        <thead>
                            <tr>
                                <th>Job ID</th>
                                <th>Site address</th>
                                <th>Scheduled date</th>
                                <th>Scheduled time</th>
                                <th>Job status</th>
                            </tr>
                        </thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td data-label="Job ID">#<%#: Eval("JobID") %></td>
                        <td data-label="Site address"><%#: Eval("SiteAddress") %></td>
                        <td data-label="Scheduled date"><%#: Eval("JobDateDisplay") %></td>
                        <td data-label="Scheduled time"><%#: Eval("TimeSlotDisplay") %></td>
                        <td data-label="Job status"><span class="progress-status"><%#: Eval("JobStatus") %></span></td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                        </tbody>
                    </table>
                    </div>
                </FooterTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="progress-empty">
                <span class="gg-tile is-lg mb-3"><i class="bi bi-clipboard2-check"></i></span>
                <h2 class="h5">No current jobs found</h2>
                <p class="mb-3">You don't have any scheduled jobs linked to your account yet.</p>
                <a runat="server" href="~/Requests/SubmitRequest.aspx" class="gg-btn gg-btn-primary">Submit a request</a>
            </asp:Panel>
        </div>

        <p class="progress-note mt-3">Statuses shown here reflect the latest information saved by Giant Group. If you have questions about a status, please contact support.</p>
        <a runat="server" href="~/ClientPortal.aspx" class="gg-btn gg-btn-outline mt-2">Back to client portal</a>
    </div>
</asp:Content>
