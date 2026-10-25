<%@ Page Title="My Service Requests" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewRequests.aspx.cs" Inherits="GiantGroupM3.Requests.ViewRequests" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .history-wrap { max-width: 1200px; margin: 2rem auto; }
        .history-table { width: 100%; }
        .history-table th, .history-table td { vertical-align: top; padding: .75rem; }
        .history-table th { white-space: nowrap; }
        @media (max-width: 767px) { .history-table { min-width: 850px; } .history-scroll { overflow-x: auto; } }
    </style>

    <div class="history-wrap px-3">
        <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
            <div>
                <h1>My Service Requests</h1>
                <p class="text-muted mb-0">Requests submitted through your client account.</p>
            </div>
            <a href="~/Requests/SubmitRequest.aspx" runat="server" class="btn btn-success">Submit a new request</a>
        </div>

        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
            We could not load your request history just now. Please refresh the page or contact support if the problem continues.
        </asp:Panel>

        <div class="history-scroll">
            <asp:GridView ID="gvRequests" runat="server" AutoGenerateColumns="false" CssClass="table table-striped table-hover history-table" GridLines="None" EmptyDataText="You have not submitted any service requests yet." UseAccessibleHeader="true">
                <Columns>
                    <asp:BoundField DataField="jobRequestID" HeaderText="Request #" />
                    <asp:BoundField DataField="dateRecieved" HeaderText="Date submitted" DataFormatString="{0:dd MMM yyyy}" />
                    <asp:BoundField DataField="siteAddress" HeaderText="Site address" />
                    <asp:BoundField DataField="JobTypes" HeaderText="Requested work" />
                    <asp:BoundField DataField="urgencyLevel" HeaderText="Urgency" />
                    <asp:BoundField DataField="RequestStatus" HeaderText="Status" />
                    <asp:BoundField DataField="siteEvaluationDate" HeaderText="Site evaluation date" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="Not scheduled" />
                </Columns>
            </asp:GridView>
        </div>
        <p class="text-muted small">A blank site evaluation date means an administrator has not scheduled the evaluation yet.</p>
        <a href="~/ClientPortal.aspx" runat="server" class="btn btn-outline-secondary">Back to Client Portal</a>
    </div>
</asp:Content>
