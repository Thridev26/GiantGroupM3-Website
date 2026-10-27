<%@ Page Title="My Payments" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Payments.aspx.cs" Inherits="GiantGroupM3.Invoicing.Payments" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .payments-wrap { max-width: 1200px; margin: 2rem auto; }
        .payments-scroll { overflow-x: auto; }
        .payments-table th, .payments-table td { vertical-align: middle; padding: .8rem; }
        .payments-note { border-left: 4px solid #6c757d; }
        @media (max-width: 767px) { .payments-table { min-width: 850px; } }
    </style>

    <div class="payments-wrap px-3">
        <div class="mb-4">
            <h1>My Payments</h1>
            <p class="text-muted mb-0">View the final quoted amount, payments recorded by Giant Group, and any remaining balance for your jobs.</p>
        </div>

        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
            We could not load your payment history. Please refresh the page or contact the front desk if the problem continues.
        </asp:Panel>

        <asp:Panel ID="pnlReviewNote" runat="server" Visible="false" CssClass="alert alert-warning" role="alert">
            One or more jobs have recorded payments greater than the quoted total. Those entries are flagged for account review; please contact the front desk.
        </asp:Panel>

        <div class="alert alert-secondary payments-note" role="note">
            This page currently displays existing payment records only. Online checkout is not enabled yet. A payment will only be recorded as received after the payment provider confirms it.
        </div>

        <div class="payments-scroll">
            <asp:GridView ID="gvPayments" runat="server" AutoGenerateColumns="false" OnRowCommand="gvPayments_RowCommand"
                CssClass="table table-striped table-hover payments-table" GridLines="None"
                EmptyDataText="No job invoices were found for your account." OnRowDataBound="gvPayments_RowDataBound">
                <Columns>
                    <asp:BoundField DataField="jobID" HeaderText="Job / Invoice Ref" />
                    <asp:BoundField DataField="startDate" HeaderText="Job Date" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="Not scheduled" />
                    <asp:BoundField DataField="jobStatus" HeaderText="Job Status" />
                    <asp:BoundField DataField="InvoiceTotal" HeaderText="Final Amount (R)" DataFormatString="{0:N2}" />
                    <asp:BoundField DataField="TotalPaid" HeaderText="Recorded Payments (R)" DataFormatString="{0:N2}" />
                    <asp:TemplateField HeaderText="Outstanding (R)">
                        <ItemTemplate><asp:Label ID="lblBalance" runat="server" Text='<%# FormatBalance(Eval("BalanceOutstanding")) %>' /></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Account Status">
                        <ItemTemplate><asp:Label ID="lblAccountStatus" runat="server" Text='<%# AccountStatus(Eval("BalanceOutstanding")) %>' /></ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Action">
    <ItemTemplate>
        <asp:Button
            ID="btnPayNow"
            runat="server"
            Text="Pay Now"
            CssClass="btn btn-primary btn-sm"
            CommandName="PayNow"
            CommandArgument='<%# Eval("jobID") %>'
            Visible='<%# CanPay(Eval("BalanceOutstanding")) %>'
            CausesValidation="false" />
    </ItemTemplate>
</asp:TemplateField>

                </Columns>
            </asp:GridView>
        </div>
    </div>
</asp:Content>
