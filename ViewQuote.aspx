<%@ Page Title="View Quotes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewQuote.aspx.cs" Inherits="GiantGroupM3.ViewQuote" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .stat-card { border-left: 4px solid #2d6a4f !important; }
        .stat-value { font-size: 1.6rem; font-weight: 700; color: #1b4332; line-height: 1.1; }
        .stat-label { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.5px; color: #6c757d; font-weight: 600; }
        .detail-label { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.5px; color: #6c757d; font-weight: 600; display: block; }
        .detail-value { font-weight: 600; color: #212529; }
        .quote-grid th { white-space: nowrap; font-size: 0.78rem; }
        .quote-grid td { vertical-align: middle; }
    </style>

    <div class="container py-4">

        <div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
            <div>
                <h2 class="fw-bold text-dark mb-1">Quotes Management &amp; Archive</h2>
                <p class="text-muted small mb-0">Review, manage, and track client quotations.</p>
            </div>
            <div>
                <asp:Label ID="lblStatusFilter" runat="server" AssociatedControlID="ddlStatusFilter" CssClass="form-label small fw-bold text-muted me-2 mb-0" Text="Show:" />
                <asp:DropDownList ID="ddlStatusFilter" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlStatusFilter_SelectedIndexChanged"
                    CssClass="form-select form-select-sm d-inline-block w-auto" />
            </div>
        </div>

        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-success rounded-3 mb-3" role="alert">
            <asp:Label ID="lblMessage" runat="server" CssClass="fw-bold" />
        </asp:Panel>

        <asp:Panel ID="pnlEmptyState" runat="server" Visible="false" CssClass="text-center py-5 px-4 my-4 bg-white border rounded-4 shadow-sm">
            <div class="mb-3">
                <span class="display-4">📂</span>
            </div>
            <h4 class="fw-bold text-secondary mb-2">No Quotations Available</h4>
            <p class="text-muted mb-3 mx-auto" style="max-width: 480px;">
                There are currently no active quotations associated with your account. Once your job requests have been evaluated, quotes will appear here.
            </p>
            <a runat="server" href="~/RequestQuote.aspx" class="btn btn-success px-4 fw-semibold">Request a Quote</a>
        </asp:Panel>

        <!-- Summary cards -->
        <asp:Panel ID="pnlStats" runat="server" CssClass="row g-3 mb-4">
            <div class="col-6 col-lg-3">
                <div class="card border-0 shadow-sm rounded-4 stat-card h-100"><div class="card-body py-3">
                    <div class="stat-label">Total quotes</div>
                    <div class="stat-value"><asp:Label ID="lblStatTotal" runat="server" Text="0" /></div>
                </div></div>
            </div>
            <div class="col-6 col-lg-3">
                <div class="card border-0 shadow-sm rounded-4 stat-card h-100"><div class="card-body py-3">
                    <div class="stat-label">Awaiting response</div>
                    <div class="stat-value"><asp:Label ID="lblStatAwaiting" runat="server" Text="0" /></div>
                </div></div>
            </div>
            <div class="col-6 col-lg-3">
                <div class="card border-0 shadow-sm rounded-4 stat-card h-100"><div class="card-body py-3">
                    <div class="stat-label">Accepted</div>
                    <div class="stat-value"><asp:Label ID="lblStatAccepted" runat="server" Text="0" /></div>
                </div></div>
            </div>
            <div class="col-6 col-lg-3">
                <div class="card border-0 shadow-sm rounded-4 stat-card h-100"><div class="card-body py-3">
                    <div class="stat-label">Accepted value</div>
                    <div class="stat-value"><asp:Label ID="lblStatValue" runat="server" Text="R 0,00" /></div>
                </div></div>
            </div>
        </asp:Panel>

        <div class="card shadow-sm border-0 rounded-4 overflow-hidden mb-4" runat="server" id="divTableContainer">
            <div class="table-responsive m-0">
                <asp:GridView ID="gvQuotes" runat="server" AutoGenerateColumns="False"
                    DataKeyNames="QuoteID"
                    OnSelectedIndexChanged="gvQuotes_SelectedIndexChanged"
                    OnPageIndexChanging="gvQuotes_PageIndexChanging"
                    CssClass="table table-hover table-striped align-middle mb-0 quote-grid"
                    GridLines="None"
                    AllowPaging="True" PageSize="8"
                    EmptyDataText="No quotes match this filter.">

                    <HeaderStyle CssClass="table-light text-uppercase fs-7 text-secondary border-bottom" />
                    <SelectedRowStyle CssClass="table-success" />
                    <EmptyDataRowStyle CssClass="text-center text-muted py-4" />

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
                        <asp:TemplateField HeaderText="Action">
                            <ItemTemplate>
                                <asp:LinkButton ID="lnkSelect" runat="server" CommandName="Select" CausesValidation="false"
                                    CssClass="btn btn-sm btn-outline-success fw-semibold text-nowrap"
                                    Text='<%# SelectButtonText() %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Print / View">
                            <ItemTemplate>
                                <asp:HyperLink ID="lnkPrintQuote" runat="server"
                                    NavigateUrl='<%# Eval("jobRequestID", "PrintQuote.aspx?jobRequestID={0}") %>'
                                    CssClass="btn btn-sm btn-dark text-nowrap" Target="_blank" rel="noopener">
                                    🖨️ Print
                                </asp:HyperLink>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Quote No.">
                            <ItemTemplate>
                                <span class="fw-semibold text-success text-nowrap"><%#: Eval("jobRequestID", "GT – {0}") %></span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="Name" HeaderText="Client Name" />
                        <asp:BoundField DataField="dateIssued" HeaderText="Date Issued" DataFormatString="{0:yyyy-MM-dd}" />

                        <asp:TemplateField HeaderText="Expiry Date">
                            <ItemTemplate>
                                <%# GetExpiryHtml(Eval("expiryDate"), Eval("quoteStatus")) %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="amount" HeaderText="Amount (ZAR)" DataFormatString="{0:C}" ItemStyle-CssClass="fw-semibold text-nowrap" />

                        <asp:TemplateField HeaderText="Status">
                            <ItemTemplate>
                                <span class='<%# GetStatusCss(Eval("quoteStatus")) %>'><%#: Eval("quoteStatus") %></span>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>

        <asp:Panel ID="pnlEditQuote" runat="server" Visible="false" CssClass="card border-0 shadow-sm rounded-4 p-4 mt-4 bg-white">

            <!-- Selected quote summary (read-only, shown to everyone) -->
            <div class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
                <h4 class="fw-bold text-dark mb-0">
                    <asp:Label ID="lblDetailTitle" runat="server" />
                </h4>
                <asp:HyperLink ID="lnkDetailPrint" runat="server" Target="_blank" rel="noopener"
                    CssClass="btn btn-sm btn-dark">🖨️ Print / Save as PDF</asp:HyperLink>
            </div>
            <div class="row g-3 mb-4 pb-3 border-bottom">
                <div class="col-6 col-md-3"><span class="detail-label">Client</span><span class="detail-value"><asp:Label ID="lblDetailClient" runat="server" /></span></div>
                <div class="col-6 col-md-3"><span class="detail-label">Date issued</span><span class="detail-value"><asp:Label ID="lblDetailIssued" runat="server" /></span></div>
                <div class="col-6 col-md-3"><span class="detail-label">Valid until</span><span class="detail-value"><asp:Label ID="lblDetailExpiry" runat="server" /></span></div>
                <div class="col-6 col-md-3"><span class="detail-label">Total (incl. VAT)</span><span class="detail-value"><asp:Label ID="lblDetailAmount" runat="server" /></span></div>
            </div>

            <%-- Management Edit Panel --%>
            <asp:Panel ID="pnlManagementControls" runat="server">
                <h5 class="text-success fw-bold mb-3">Edit Quote Details</h5>

                <asp:Panel ID="pnlEditError" runat="server" Visible="false" CssClass="alert alert-danger rounded-3 py-2" role="alert">
                    <asp:Label ID="lblEditError" runat="server" />
                </asp:Panel>

                <div class="row">
                    <div class="col-md-4 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtExpiryDate" CssClass="form-label fw-bold small text-muted" Text="Expiry Date:" />
                        <asp:TextBox ID="txtExpiryDate" runat="server" TextMode="Date" CssClass="form-control" />
                    </div>
                    <div class="col-md-4 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtAmount" CssClass="form-label fw-bold small text-muted" Text="Amount (ZAR, incl. VAT):" />
                        <asp:TextBox ID="txtAmount" runat="server" TextMode="Number" CssClass="form-control" step="0.01" min="0.01" />
                    </div>
                    <div class="col-md-4 mb-3">
                        <asp:Label runat="server" AssociatedControlID="ddlQuoteStatus" CssClass="form-label fw-bold small text-muted" Text="Quote Status:" />
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
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CausesValidation="false" CssClass="btn btn-outline-secondary px-4 fw-bold" />
                </div>
            </asp:Panel>

            <%-- Client Response Panel --%>
            <asp:Panel ID="pnlClientResponse" runat="server" Visible="false">
                <h5 class="text-success fw-bold mb-2">Respond to Quote</h5>
                <p class="text-muted small">Please review the quote details above. You may accept or reject this quotation. Your response cannot be changed afterwards.</p>

                <div class="d-flex gap-3 flex-wrap">
                    <asp:Button ID="btnAcceptQuote" runat="server" Text="Accept Quote" OnClick="btnAcceptQuote_Click"
                        OnClientClick="return confirm('Accept this quotation? This lets us schedule the work.');"
                        CssClass="btn btn-success px-4 fw-bold" />
                    <asp:Button ID="btnRejectQuote" runat="server" Text="Reject Quote" OnClick="btnRejectQuote_Click"
                        OnClientClick="return confirm('Reject this quotation? This will cancel the job request.');"
                        CssClass="btn btn-outline-danger px-4 fw-bold" />
                    <asp:Button ID="btnCancelResponse" runat="server" Text="Cancel" OnClick="btnCancel_Click" CausesValidation="false" CssClass="btn btn-outline-secondary px-4 fw-bold" />
                </div>
            </asp:Panel>

            <%-- Client: quote can no longer be answered --%>
            <asp:Panel ID="pnlClientReadOnly" runat="server" Visible="false">
                <div class="alert alert-secondary rounded-3 mb-3" role="status">
                    <asp:Label ID="lblClientReadOnly" runat="server" />
                </div>
                <asp:Button ID="btnCloseReadOnly" runat="server" Text="Close" OnClick="btnCancel_Click" CausesValidation="false" CssClass="btn btn-outline-secondary px-4 fw-bold" />
            </asp:Panel>

        </asp:Panel>
    </div>
</asp:Content>
