<%@ Page Title="Manage Site Evaluations" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ManageRequests.aspx.cs" Inherits="GiantGroupM3.ManageRequests" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .sr-wrap { max-width: 1180px; margin: 2rem auto; padding: 0 1rem 2rem; }
        .sr-heading { margin-bottom: 1.25rem; }
        .sr-heading h1 { color: #174d2b; font-weight: 750; margin-bottom: .4rem; }
        .sr-card { background: #fff; border: 1px solid #dce8df; border-radius: 14px; padding: 1.25rem; box-shadow: 0 5px 20px rgba(20,60,35,.05); }
        .sr-table { width: 100%; border-collapse: collapse; }
        .sr-table th { background: #174d2b; color: #fff; padding: .85rem .75rem; text-align: left; }
        .sr-table td { padding: .8rem .75rem; border-bottom: 1px solid #e5ece7; vertical-align: middle; }
        .sr-table tr:hover td { background: #f5faf6; }
        .sr-open { display: inline-block; padding: .45rem .8rem; background: #176b36; color: #fff !important; border-radius: 7px; text-decoration: none !important; font-weight: 650; white-space: nowrap; }
        .sr-open:hover { background: #10542a; }
        .sr-message { display:block; padding:.75rem 1rem; margin-bottom:1rem; border-radius:8px; background:#edf7ef; color:#174d2b; }
        .sr-empty { padding: 2rem 1rem; text-align: center; color: #627166; }
        @media(max-width:800px) { .sr-table { min-width:850px; } .sr-overflow { overflow-x:auto; } }
    </style>

    <div class="sr-wrap">
        <div class="sr-heading">
            <div class="text-muted mb-2">Operations / Site evaluations</div>
            <h1>Manage Requests</h1>
            <p class="text-muted">Open a request with a site evaluation date to capture its job details and measurements. The date is set by the Systems Manager and cannot be changed here.</p>
        </div>

        <asp:Label ID="lblMessage" runat="server" Visible="false" CssClass="sr-message" />

        <div class="sr-card">
            <div class="row g-2 align-items-end mb-4">
                <div class="col-md-8">
                    <label for="txtSearch" class="form-label">Search by request number, client or site address</label>
                    <asp:TextBox ID="txtSearch" runat="server" ClientIDMode="Static" CssClass="form-control" placeholder="e.g. 104, Smith or Durban" />
                </div>
                <div class="col-md-4 d-flex gap-2">
                    <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-success" OnClick="btnSearch_Click" />
                    <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-outline-secondary" CausesValidation="false" OnClick="btnClear_Click" />
                </div>
            </div>

            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="sr-empty">
                <h3>No requests with an evaluation date were found.</h3>
                <p>Requests appear here after the Systems Manager sets their site evaluation date.</p>
            </asp:Panel>
            <div class="sr-overflow">
                <asp:GridView ID="gvRequests" runat="server" AutoGenerateColumns="false" CssClass="sr-table" GridLines="None" EmptyDataText="No requests found." DataKeyNames="jobRequestID">
                    <Columns>
                        <asp:BoundField DataField="jobRequestID" HeaderText="Request #" />
                        <asp:BoundField DataField="clientName" HeaderText="Client" />
                        <asp:BoundField DataField="siteAddress" HeaderText="Site address" />
                        <asp:BoundField DataField="requestedItems" HeaderText="Requested items" />
                        <asp:BoundField DataField="siteEvaluationDate" HeaderText="Evaluation date" DataFormatString="{0:dd MMM yyyy}" />
                        <asp:BoundField DataField="status" HeaderText="Status" />
                        <asp:TemplateField HeaderText="Action">
                            <ItemTemplate>
                                <asp:HyperLink ID="lnkOpenRequest" runat="server" CssClass="sr-open" Text="Open request"
                                    NavigateUrl='<%# "~/Requests/EvaluateRequest.aspx?id=" + Eval("jobRequestID") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataRowStyle CssClass="sr-empty" />
                </asp:GridView>
            </div>
        </div>
    </div>
</asp:Content>
