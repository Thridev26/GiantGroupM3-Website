<%@ Page Title="View Quotes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewQuote.aspx.cs" Inherits="GiantGroupM3.ViewQuote" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-4">
        
        <div class="d-flex justify-content-between align-items-center mb-4">
            <div>
                <h2 class="fw-bold text-dark mb-1">Quotes Management & Archive</h2>
                <p class="text-muted small mb-0">Review, manage, and track client quotations.</p>
            </div>
        </div>

        <asp:Label ID="lblMessage" runat="server" CssClass="fw-bold text-success mb-3 d-block" />
        
        <asp:Panel ID="pnlEmptyState" runat="server" Visible="false" CssClass="text-center py-5 px-4 my-4 bg-white border rounded-4 shadow-sm">
            <div class="mb-3">
                <span class="display-4">📂</span>
            </div>
            <h4 class="fw-bold text-secondary mb-2">No Quotations Available</h4>
            <p class="text-muted mb-0 mx-auto" style="max-width: 480px;">
                There are currently no active quotations associated with your account. Once your job requests have been evaluated, quotes will appear here.
            </p>
        </asp:Panel>

        <div class="card shadow-sm border-0 rounded-4 overflow-hidden mb-4" runat="server" id="divTableContainer">
            <div class="table-responsive m-0">
               <asp:GridView ID="gvQuotes" runat="server" AutoGenerateColumns="False" 
    DataKeyNames="QuoteID" 
    OnSelectedIndexChanged="gvQuotes_SelectedIndexChanged" 
    OnPageIndexChanging="gvQuotes_PageIndexChanging" 
    CssClass="table table-hover table-striped align-middle mb-0" 
    AllowPaging="True" PageSize="8">
    
    <HeaderStyle CssClass="table-light text-uppercase fs-7 text-secondary border-bottom" />
    
    <PagerTemplate>
        <div class="d-flex justify-content-between align-items-center px-3 py-3 bg-white border-top">
            <span class="text-muted small">
                Page <strong class="text-dark"><%# gvQuotes.PageIndex + 1 %></strong> of <strong class="text-dark"><%# gvQuotes.PageCount %></strong>
            </span>
            <div class="d-flex gap-2">
                <asp:Button ID="btnPrev" runat="server" CommandName="Page" CommandArgument="Prev" 
                    CssClass="btn btn-sm btn-outline-secondary px-3 fw-semibold" Text="&larr; Previous" 
                    Enabled='<%# gvQuotes.PageIndex > 0 %>' />
                <asp:Button ID="btnNext" runat="server" CommandName="Page" CommandArgument="Next" 
                    CssClass="btn btn-sm btn-outline-secondary px-3 fw-semibold" Text="Next &rarr;" 
                    Enabled='<%# gvQuotes.PageIndex < gvQuotes.PageCount - 1 %>' />
            </div>
        </div>
    </PagerTemplate>

    <Columns>
        <asp:CommandField ShowSelectButton="True" SelectText="Select/Edit" ControlStyle-CssClass="btn btn-sm btn-outline-success fw-semibold" />
        
        <asp:TemplateField HeaderText="Print / View">
            <ItemTemplate>
                <asp:HyperLink ID="lnkPrintQuote" runat="server" 
                    NavigateUrl='<%# Eval("jobRequestID", "PrintQuote.aspx?jobRequestID={0}") %>' 
                    CssClass="btn btn-sm btn-dark text-nowrap" Target="_blank">
                    🖨️ Print
                </asp:HyperLink>
            </ItemTemplate>
        </asp:TemplateField>

        <%-- Hidden columns --%>
        <asp:BoundField DataField="QuoteID" HeaderText="Quote ID" Visible="False" />
        <asp:BoundField DataField="jobRequestID" HeaderText="Job Request ID" Visible="False" />

        <asp:BoundField DataField="Name" HeaderText="Client Name" />
        <asp:BoundField DataField="dateIssued" HeaderText="Date Issued" DataFormatString="{0:yyyy-MM-dd}" />
        <asp:BoundField DataField="expiryDate" HeaderText="Expiry Date" DataFormatString="{0:yyyy-MM-dd}" />
        <asp:BoundField DataField="amount" HeaderText="Amount (ZAR)" DataFormatString="{0:C}" />
        <asp:BoundField DataField="quoteStatus" HeaderText="Status" />
    </Columns>
</asp:GridView>
            </div>
        </div>

        <asp:Panel ID="pnlEditQuote" runat="server" Visible="false" CssClass="card border-0 shadow-sm rounded-4 p-4 mt-4 bg-white">
            <asp:HiddenField ID="hfQuoteID" runat="server" />

            <%-- Management Edit Panel --%>
            <asp:Panel ID="pnlManagementControls" runat="server">
                <h4 class="text-success fw-bold mb-3">Edit Quote Details</h4>
                <div class="row">
                    <div class="col-md-4 mb-3">
                        <label class="form-label fw-bold small text-muted">Expiry Date:</label>
                        <asp:TextBox ID="txtExpiryDate" runat="server" TextMode="Date" CssClass="form-control" />
                    </div>
                    <div class="col-md-4 mb-3">
                        <label class="form-label fw-bold small text-muted">Amount:</label>
                        <asp:TextBox ID="txtAmount" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-4 mb-3">
                        <label class="form-label fw-bold small text-muted">Quote Status:</label>
                        <asp:DropDownList ID="ddlQuoteStatus" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Pending" Value="Pending" />
                            <asp:ListItem Text="Sent" Value="Sent" />
                            <asp:ListItem Text="Accepted" Value="Accepted" />
                            <asp:ListItem Text="Declined" Value="Declined" />
                            <asp:ListItem Text="Rejected" Value="Rejected" />
                        </asp:DropDownList>
                    </div>
                </div>
                <div class="d-flex gap-2">
                    <asp:Button ID="btnUpdate" runat="server" Text="Update Quote" OnClick="btnUpdate_Click" CssClass="btn btn-success px-4 fw-bold" />
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="btn btn-outline-secondary px-4 fw-bold" />
                </div>
            </asp:Panel>

            <%-- Client Response Panel --%>
            <asp:Panel ID="pnlClientResponse" runat="server" Visible="false">
                <h4 class="text-success fw-bold mb-3">Respond to Quote</h4>
                <p class="text-muted small">Please review your selected quote details above. You may accept or reject this quotation.</p>
                
                <div class="d-flex gap-3">
                    <asp:Button ID="btnAcceptQuote" runat="server" Text="Accept Quote" OnClick="btnAcceptQuote_Click" CssClass="btn btn-success px-4 fw-bold" />
                    <asp:Button ID="btnRejectQuote" runat="server" Text="Reject Quote" OnClick="btnRejectQuote_Click" CssClass="btn btn-outline-danger px-4 fw-bold" />
                    <asp:Button ID="btnCancelResponse" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="btn btn-outline-secondary px-4 fw-bold" />
                </div>
            </asp:Panel>
     
        </asp:Panel>
    </div>
</asp:Content>