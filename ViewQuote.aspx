<%@ Page Title="View Quotes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewQuote.aspx.cs" Inherits="GiantGroupM3.ViewQuote" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-4">
        <h2 class="mb-3">Quotes Management & Archive</h2>
        <hr />
        
        <asp:Label ID="lblMessage" runat="server" CssClass="fw-bold text-success mb-3 d-block" />
        
        <!-- Quotes GridView -->
        <div class="table-responsive">
           <asp:GridView ID="gvQuotes" runat="server" AutoGenerateColumns="False" 
    DataKeyNames="QuoteID" 
    OnSelectedIndexChanged="gvQuotes_SelectedIndexChanged" 
    OnPageIndexChanging="gvQuotes_PageIndexChanging" 
    CssClass="table table-striped table-bordered align-middle" AllowPaging="True">
    <Columns>
        <asp:CommandField ShowSelectButton="True" SelectText="Select/Edit" ControlStyle-CssClass="btn btn-sm btn-outline-success" />
        
        
        <asp:TemplateField HeaderText="Print / View Document">
            <ItemTemplate>
                <asp:HyperLink ID="lnkPrintQuote" runat="server" 
                    NavigateUrl='<%# Eval("jobRequestID", "PrintQuote.aspx?jobRequestID={0}") %>' 
                    CssClass="btn btn-sm btn-dark text-nowrap" Target="_blank">
                    🖨️ View & Print
                </asp:HyperLink>
            </ItemTemplate>
        </asp:TemplateField>

        <asp:BoundField DataField="QuoteID" HeaderText="Quote ID" />
        <asp:BoundField DataField="jobRequestID" HeaderText="Job Request ID" />
        <asp:BoundField DataField="Name" HeaderText="Client Name" />
        <asp:BoundField DataField="dateIssued" HeaderText="Date Issued" DataFormatString="{0:yyyy-MM-dd}" />
        <asp:BoundField DataField="expiryDate" HeaderText="Expiry Date" DataFormatString="{0:yyyy-MM-dd}" />
        <asp:BoundField DataField="amount" HeaderText="Amount (ZAR)" DataFormatString="{0:C}" />
        <asp:BoundField DataField="quoteStatus" HeaderText="Status" />
    </Columns>
</asp:GridView>
        </div>

        <br />

        <!-- Management Edit Panel / Client Response Panel -->
        <asp:Panel ID="pnlEditQuote" runat="server" Visible="false" CssClass="card p-4 shadow-sm mt-4 bg-light">
            <asp:HiddenField ID="hfQuoteID" runat="server" />

            <!-- MANAGEMENT VIEW -->
            <asp:Panel ID="pnlManagementControls" runat="server">
                <h4 class="text-success mb-3">Edit Quote Details</h4>
                <div class="mb-3">
                    <label class="form-label fw-bold">Expiry Date:</label>
                    <asp:TextBox ID="txtExpiryDate" runat="server" TextMode="Date" CssClass="form-control" />
                </div>
                <div class="mb-3">
                    <label class="form-label fw-bold">Amount:</label>
                    <asp:TextBox ID="txtAmount" runat="server" CssClass="form-control" />
                </div>
                <div class="mb-3">
                    <label class="form-label fw-bold">Quote Status:</label>
                    <asp:DropDownList ID="ddlQuoteStatus" runat="server" CssClass="form-select">
                        <asp:ListItem Text="Pending" Value="Pending" />
                        <asp:ListItem Text="Sent" Value="Sent" />
                        <asp:ListItem Text="Accepted" Value="Accepted" />
                        <asp:ListItem Text="Declined" Value="Declined" />
                        <asp:ListItem Text="Rejected" Value="Rejected" />
                    </asp:DropDownList>
                </div>
                
                <asp:Button ID="btnUpdate" runat="server" Text="Update Quote" OnClick="btnUpdate_Click" CssClass="btn btn-success" />
            </asp:Panel>

            <!-- CLIENT RESPONSE VIEW -->
            <asp:Panel ID="pnlClientResponse" runat="server" Visible="false">
                <h4 class="text-success mb-3">Respond to Quote</h4>
                <p class="text-muted small">Please review your selected quote details above. You may accept or reject this quotation.</p>
                
                <div class="d-flex gap-3">
                    <asp:Button ID="btnAcceptQuote" runat="server" Text="Accept Quote" OnClick="btnAcceptQuote_Click" CssClass="btn btn-success px-4 fw-bold" />
                    <asp:Button ID="btnRejectQuote" runat="server" Text="Reject Quote" OnClick="btnRejectQuote_Click" CssClass="btn btn-outline-danger px-4 fw-bold" />
                </div>
            </asp:Panel>

        </asp:Panel>
    </div>
</asp:Content>