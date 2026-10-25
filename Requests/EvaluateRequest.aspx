<%@ Page Title="Capture Site Evaluation" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="EvaluateRequest.aspx.cs" Inherits="GiantGroupM3.EvaluateRequest" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .ev-wrap { max-width: 1050px; margin: 2rem auto; padding: 0 1rem 2rem; }
        .ev-heading h1 { color: #174d2b; font-weight: 750; margin-bottom: .4rem; }
        .ev-card { background: #fff; border: 1px solid #dce8df; border-radius: 14px; padding: 1.25rem; margin-bottom: 1rem; box-shadow: 0 5px 20px rgba(20,60,35,.04); }
        .ev-card h2 { color: #174d2b; font-size: 1.15rem; font-weight: 700; margin-bottom: 1rem; }
        .ev-meta { display: grid; grid-template-columns: repeat(2,minmax(0,1fr)); gap: .8rem 1.5rem; }
        .ev-meta strong { display:block; font-size:.8rem; color:#637168; margin-bottom:.15rem; }
        .ev-meta span { overflow-wrap:anywhere; }
        .ev-item { border: 1px solid #dfe9e2; border-radius: 10px; padding: 1rem; margin: .8rem 0; background: #fbfdfb; }
        .ev-item-title { font-weight: 750; color:#174d2b; font-size:1.05rem; margin-bottom:.9rem; }
        .ev-field { display:grid; grid-template-columns:minmax(150px, 1fr) minmax(200px, 2fr) minmax(160px,1.2fr); gap:.75rem; align-items:center; margin-bottom:.8rem; }
        .ev-field label { margin:0; font-weight:600; }
        .ev-hint { color:#637168; font-size:.82rem; font-style:italic; }
        .ev-message { display:block; padding:.75rem 1rem; margin-bottom:1rem; border-radius:8px; background:#edf7ef; color:#174d2b; }
        .ev-actions { display:flex; flex-wrap:wrap; gap:.7rem; align-items:center; }
        @media(max-width:760px) { .ev-meta { grid-template-columns:1fr; } .ev-field { grid-template-columns:1fr; gap:.35rem; } }
    </style>

    <div class="ev-wrap">
        <div class="ev-heading mb-4">
            <div class="text-muted mb-2"><a href="~/Requests/ManageRequests.aspx" runat="server">Manage Requests</a> / Site evaluation</div>
            <h1>Capture Site Evaluation</h1>
            <p class="text-muted">Enter the measurements and details collected during the site visit. The scheduled evaluation date is read-only and is managed by the Systems Manager.</p>
        </div>

        <asp:Label ID="lblMessage" runat="server" Visible="false" CssClass="ev-message" />

        <asp:Panel ID="pnlRequest" runat="server" Visible="false">
            <div class="ev-card">
                <h2>Request information</h2>
                <div class="ev-meta">
                    <div><strong>Request number</strong><span><asp:Literal ID="litRequestID" runat="server" /></span></div>
                    <div><strong>Client</strong><span><asp:Literal ID="litClient" runat="server" /></span></div>
                    <div><strong>Site address</strong><span><asp:Literal ID="litAddress" runat="server" /></span></div>
                    <div><strong>Urgency</strong><span><asp:Literal ID="litUrgency" runat="server" /></span></div>
                    <div><strong>Site evaluation date (set by Systems Manager)</strong><span><asp:Literal ID="litEvaluationDate" runat="server" /></span></div>
                    <div><strong>Current status</strong><span><asp:Literal ID="litStatus" runat="server" /></span></div>
                </div>
            </div>

            <div class="ev-card">
                <h2>Requested items and measurements</h2>
                <p class="text-muted">The fields below are loaded from the JobDetail definitions for each requested item. Leave a field blank if no value is available yet. Click Capture to save entered values; you can return and update them later.</p>
                <asp:PlaceHolder ID="phDetails" runat="server" />
                <div class="ev-actions mt-3">
                    <asp:Button ID="btnCapture" runat="server" Text="Capture details" CssClass="btn btn-success" OnClick="btnCapture_Click" />
                    <asp:Button ID="btnSiteEvaluated" runat="server" Text="Site Evaluated" CssClass="btn btn-outline-success" OnClick="btnSiteEvaluated_Click" CausesValidation="false" OnClientClick="return confirm('Mark this request as Site Evaluated? This changes the request status.');" />
                    <a href="~/Requests/ManageRequests.aspx" runat="server" class="btn btn-outline-secondary">Back to requests</a>
                </div>
            </div>
        </asp:Panel>
    </div>
</asp:Content>
