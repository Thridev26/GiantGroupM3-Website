<%@ Page Title="My Service Requests" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewRequests.aspx.cs" Inherits="GiantGroupM3.Requests.ViewRequests" %>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .history-wrap { max-width: 1200px; margin: 2rem auto; }
        .history-table { width: 100%; }
        .history-table th, .history-table td { vertical-align: top; padding: .75rem; }
        .history-table th { white-space: nowrap; }
        .photo-grid { display:flex; flex-wrap:wrap; gap:1rem; }
        .photo-card { width:170px; border:1px solid #d7dedb; border-radius:8px; padding:.5rem; }
        .photo-card img { width:100%; height:125px; object-fit:cover; border-radius:5px; }
        @media (max-width:767px) { .history-table { min-width:1000px; } .history-scroll { overflow-x:auto; } }
    </style>
    <div class="history-wrap px-3">
        <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
            <div><h1>My Service Requests</h1><p class="text-muted mb-0">Manage your requests and site photos.</p></div>
            <a href="~/Requests/SubmitRequest.aspx" runat="server" class="btn btn-success">Submit a new request</a>
        </div>
        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-danger">We couldn't load your request details. Please retry or contact support.</asp:Panel>
        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-info"><asp:Literal ID="litMessage" runat="server" Mode="Encode" /></asp:Panel>
        <div class="alert alert-info">Request details cannot be edited. To change a request, cancel it (if eligible) and submit a new one, or call Giant Group at <a href="tel:0848331373">084 833 1373</a>. Cancellation is only available for Pending / Evaluation date set requests with no evaluation date, or where the evaluation is after today.</div>
        <div class="history-scroll">
            <asp:GridView ID="gvRequests" runat="server" AutoGenerateColumns="false" CssClass="table table-striped table-hover history-table" GridLines="None" EmptyDataText="You have not submitted any service requests yet." UseAccessibleHeader="true" DataKeyNames="jobRequestID" OnRowCommand="gvRequests_RowCommand" OnRowDataBound="gvRequests_RowDataBound">
                <Columns>
                    <asp:BoundField DataField="jobRequestID" HeaderText="Request #" />
                    <asp:BoundField DataField="dateRecieved" HeaderText="Submitted" DataFormatString="{0:dd MMM yyyy}" />
                    <asp:BoundField DataField="siteAddress" HeaderText="Site address" />
                    <asp:BoundField DataField="JobTypes" HeaderText="Requested work" />
                    <asp:BoundField DataField="urgencyLevel" HeaderText="Urgency" />
                    <asp:BoundField DataField="RequestStatus" HeaderText="Status" />
                    <asp:BoundField DataField="siteEvaluationDate" HeaderText="Evaluation date" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="Not scheduled" />
                    <asp:TemplateField HeaderText="Actions"><ItemTemplate>
                        <asp:LinkButton ID="btnPhotos" runat="server" CssClass="btn btn-outline-primary btn-sm mb-1" CommandName="Photos" CommandArgument='<%# Eval("jobRequestID") %>' CausesValidation="false">Site photos</asp:LinkButton>
                        <asp:LinkButton ID="btnCancel" runat="server" CssClass="btn btn-outline-danger btn-sm mb-1" CommandName="CancelRequest" CommandArgument='<%# Eval("jobRequestID") %>' CausesValidation="false" OnClientClick="return confirm('Cancel this job request? This cannot be undone.');">Cancel request</asp:LinkButton>
                    </ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <asp:Panel ID="pnlPhotos" runat="server" Visible="false" CssClass="card p-3 mt-4">
            <div class="d-flex justify-content-between align-items-center"><h2 class="h4"><asp:Literal ID="litPhotoTitle" runat="server" Mode="Encode" /></h2><asp:LinkButton ID="btnClosePhotos" runat="server" OnClick="btnClosePhotos_Click" CssClass="btn btn-outline-secondary btn-sm" CausesValidation="false">Close</asp:LinkButton></div>
            <asp:HiddenField ID="hfRequestId" runat="server" />
            <asp:Panel ID="pnlUpload" runat="server" CssClass="mb-3">
                <p class="text-muted">Upload JPG or PNG site photos (BEFORE only, maximum 5 MB each).</p>
                <asp:FileUpload ID="fuPhoto" runat="server" accept=".jpg,.jpeg,.png,image/jpeg,image/png" CssClass="form-control mb-2" />
                <asp:Button ID="btnUpload" runat="server" CssClass="btn btn-success" Text="Upload photo" OnClick="btnUpload_Click" CausesValidation="false" />
            </asp:Panel>
            <asp:Panel ID="pnlReadOnly" runat="server" Visible="false" CssClass="alert alert-secondary">This request has been cancelled. You may view its photos, but cannot add or remove them.</asp:Panel>
            <div class="photo-grid"><asp:Repeater ID="rptPhotos" runat="server" OnItemCommand="rptPhotos_ItemCommand"><ItemTemplate>
                <div class="photo-card">
                    <a href='<%# PhotoUrl(Eval("photoID")) %>' target="_blank" rel="noopener"><img src='<%# PhotoUrl(Eval("photoID")) %>' alt="Site photo" loading="lazy" /></a>
                    <div class="small text-muted mt-2"><%#: Eval("uploadDate", "{0:dd MMM yyyy}") %> — BEFORE</div>
                    <asp:LinkButton ID="btnRemove" runat="server" CssClass="btn btn-outline-danger btn-sm mt-2" CommandName="RemovePhoto" CommandArgument='<%# Eval("photoID") %>' Visible='<%# Convert.ToBoolean(Eval("CanDelete")) %>' CausesValidation="false" OnClientClick="return confirm('Remove this photo?');">Remove</asp:LinkButton>
                </div>
            </ItemTemplate></asp:Repeater></div>
            <asp:Label ID="lblNoPhotos" runat="server" Visible="false" CssClass="text-muted" Text="No website-uploaded photos yet. Older desktop photos may not be accessible through the website." />
        </asp:Panel>
        <div class="mt-3"><a href="~/ClientPortal.aspx" runat="server" class="btn btn-outline-secondary">Back to Client Portal</a></div>
    </div>
</asp:Content>
