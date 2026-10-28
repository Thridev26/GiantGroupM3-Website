
<%@ Page Title="My Payments" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Payments.aspx.cs" Inherits="GiantGroupM3.Invoicing.Payments" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .payments-wrap {
            max-width: 1200px;
            margin: 2rem auto;
        }

        .payments-scroll {
            overflow-x: auto;
        }

        .payments-table th,
        .payments-table td {
            vertical-align: middle;
            padding: .8rem;
        }

        .payments-note {
            border-left: 4px solid #6c757d;
        }

        .payment-amount {
            width: 140px;
            min-width: 120px;
        }

        @media (max-width: 767px) {
            .payments-table {
                min-width: 1050px;
            }
        }
    </style>

    <div class="payments-wrap px-3">
        <div class="mb-4">
            <h1>My Payments</h1>
            <p class="text-muted mb-0">
                View your job invoices, recorded payments, and remaining balances.
                You can pay the full outstanding amount or make a partial payment.
            </p>
        </div>

        <asp:Panel ID="pnlError" runat="server"
            Visible="false"
            CssClass="alert alert-danger"
            role="alert">
            We could not complete your request. Please check the amount and try again.
            If the problem continues, contact the front desk.
        </asp:Panel>

        <asp:Panel ID="pnlReviewNote" runat="server"
            Visible="false"
            CssClass="alert alert-warning"
            role="alert">
            One or more jobs have recorded payments greater than the quoted total.
            Those entries are flagged for account review; please contact the front desk.
        </asp:Panel>

        <asp:Panel ID="pnlPaymentMessage" runat="server"
            Visible="false"
            CssClass="alert alert-info"
            role="status">
            <asp:Label ID="lblPaymentMessage" runat="server" />
        </asp:Panel>

        <div class="alert alert-secondary payments-note" role="note">
            Select an amount to pay for each outstanding job. The minimum online payment
            is R5.00. Your payment is recorded as received only after PayFast confirms it.
        </div>

        <div class="payments-scroll">
            <asp:GridView ID="gvPayments" runat="server"
                AutoGenerateColumns="false"
                OnRowCommand="gvPayments_RowCommand"
                OnRowDataBound="gvPayments_RowDataBound"
                CssClass="table table-striped table-hover payments-table"
                GridLines="None"
                EmptyDataText="No job invoices were found for your account.">

                <Columns>
                    <asp:BoundField
                        DataField="jobID"
                        HeaderText="Job / Invoice Ref" />

                    <asp:BoundField
                        DataField="startDate"
                        HeaderText="Job Date"
                        DataFormatString="{0:dd MMM yyyy}"
                        NullDisplayText="Not scheduled" />

                    <asp:BoundField
                        DataField="jobStatus"
                        HeaderText="Job Status" />

                    <asp:BoundField
                        DataField="InvoiceTotal"
                        HeaderText="Final Amount (R)"
                        DataFormatString="{0:N2}" />

                    <asp:BoundField
                        DataField="TotalPaid"
                        HeaderText="Recorded Payments (R)"
                        DataFormatString="{0:N2}" />

                    <asp:TemplateField HeaderText="Outstanding (R)">
                        <ItemTemplate>
                            <asp:Label
                                ID="lblBalance"
                                runat="server"
                                Text='<%# FormatBalance(Eval("BalanceOutstanding")) %>' />
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Account Status">
                        <ItemTemplate>
                            <asp:Label
                                ID="lblAccountStatus"
                                runat="server"
                                Text='<%# AccountStatus(Eval("BalanceOutstanding")) %>' />
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Amount to Pay (R)">
                        <ItemTemplate>
                            <asp:TextBox
                                ID="txtPaymentAmount"
                                runat="server"
                                CssClass="form-control form-control-sm payment-amount"
                                Text='<%# FormatPaymentAmount(Eval("BalanceOutstanding")) %>'
                                TextMode="Number"
                                step="0.01"
                                min="5.00"
                                max='<%# FormatPaymentAmount(Eval("BalanceOutstanding")) %>'
                                Enabled='<%# CanPay(Eval("BalanceOutstanding")) %>'
                                aria-label="Amount to pay for this job" />

                            <asp:Label
                                ID="lblAmountHelp"
                                runat="server"
                                CssClass="small text-muted"
                                Text="R5.00 minimum"
                                Visible='<%# CanPay(Eval("BalanceOutstanding")) %>' />
                        </ItemTemplate>
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
