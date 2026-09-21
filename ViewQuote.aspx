<%@ Page Title="Quotes Management & Archive" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewQuote.aspx.cs" Inherits="GiantGroupM3.ViewQuote" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main aria-labelledby="title" class="container py-4">
        <h2 id="title" class="mb-4">Quotes Management & Archive</h2>

        <!-- Notification Message -->
        <div class="mb-3">
            <asp:Label runat="server" ID="lblMessage" CssClass="fw-bold text-success" />
        </div>

        <!-- Clean Quotations Table -->
        <div class="table-responsive mb-4">
            <asp:GridView runat="server" ID="gvQuotes" AutoGenerateColumns="False" DataKeyNames="QuoteID"
                AllowPaging="True" PageSize="10" OnPageIndexChanging="gvQuotes_PageIndexChanging"
                OnSelectedIndexChanged="gvQuotes_SelectedIndexChanged"
                CssClass="table table-hover align-middle bg-white border" SelectedRowStyle-CssClass="table-light">
                <HeaderStyle CssClass="table-light text-secondary border-bottom" />
                <Columns>
                    <asp:BoundField DataField="QuoteID" HeaderText="Quote ID" />
                    <asp:BoundField DataField="jobRequestID" HeaderText="Job Request ID" />
                    <asp:BoundField DataField="Name" HeaderText="Client Name" />
                    <asp:BoundField DataField="dateIssued" HeaderText="Date Issued" DataFormatString="{0:yyyy-MM-dd}" />
                    <asp:BoundField DataField="expiryDate" HeaderText="Expiry Date" DataFormatString="{0:yyyy-MM-dd}" />
                    <asp:BoundField DataField="amount" HeaderText="Amount (ZAR)" DataFormatString="{0:C}" />
                    <asp:BoundField DataField="quoteStatus" HeaderText="Status" />
                    <asp:TemplateField HeaderText="Document">
                        <ItemTemplate>
                            <asp:HyperLink runat="server" NavigateUrl='<%# Eval("filePath") %>' Text="View File" Target="_blank" CssClass="btn btn-sm btn-outline-secondary" Visible='<%# Eval("filePath") != DBNull.Value && !string.IsNullOrEmpty(Eval("filePath") as string) %>' />
                            <asp:Label runat="server" Text="N/A" CssClass="text-muted" Visible='<%# Eval("filePath") == DBNull.Value || string.IsNullOrEmpty(Eval("filePath") as string) %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:CommandField ShowSelectButton="True" SelectText="Edit" ControlStyle-CssClass="btn btn-sm btn-link text-primary p-0" />
                </Columns>
            </asp:GridView>
        </div>

        <!-- Edit Quote Panel (Clean inline form) -->
        <asp:Panel runat="server" ID="pnlEditQuote" Visible="false" class="p-4 border rounded bg-light mb-4">
            <h5 class="text-dark mb-3">Update Selected Quotation</h5>
            <asp:HiddenField runat="server" ID="hfQuoteID" />

            <div class="row">
                <div class="col-md-4 mb-3">
                    <asp:Label runat="server" AssociatedControlID="txtExpiryDate" CssClass="form-label">Expiry Date:</asp:Label>
                    <asp:TextBox runat="server" ID="txtExpiryDate" CssClass="form-control" TextMode="Date" />
                </div>

                <div class="col-md-4 mb-3">
                    <asp:Label runat="server" AssociatedControlID="txtAmount" CssClass="form-label">Amount (ZAR):</asp:Label>
                    <asp:TextBox runat="server" ID="txtAmount" CssClass="form-control" TextMode="Number" step="0.01" />
                </div>

                <div class="col-md-4 mb-3">
                    <asp:Label runat="server" AssociatedControlID="ddlQuoteStatus" CssClass="form-label">Quote Status:</asp:Label>
                    <asp:DropDownList runat="server" ID="ddlQuoteStatus" CssClass="form-select">
                        <asp:ListItem Text="Draft" Value="Draft" />
                        <asp:ListItem Text="Sent" Value="Sent" />
                        <asp:ListItem Text="Accepted" Value="Accepted" />
                        <asp:ListItem Text="Rejected" Value="Rejected" />
                        <asp:ListItem Text="Expired" Value="Expired" />
                    </asp:DropDownList>
                </div>
            </div>

            <div class="mt-2">
                <asp:Button runat="server" ID="btnUpdate" Text="Save Changes" CssClass="btn btn-dark" OnClick="btnUpdate_Click" />
            </div>
        </asp:Panel>
    </main>
</asp:Content>