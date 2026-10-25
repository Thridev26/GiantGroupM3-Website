<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PrintQuote.aspx.cs" Inherits="GiantGroupM3.PrintQuote" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <title>Quotation - The Giant Group</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <style>
        body { background-color: #f8f9fa; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
        .quote-container { max-width: 850px; margin: 0 auto 40px auto; background: #fff; box-shadow: 0 0 15px rgba(0,0,0,0.1); border-radius: 8px; overflow: hidden; }
        .sidebar-brand { background-color: #1a1a1a; color: #fff; padding: 40px 30px; }
        .brand-title { font-size: 1.5rem; font-weight: bold; letter-spacing: 1px; margin-bottom: 25px; }
        .brand-details, .recipient-details { font-size: 0.85rem; color: #ccc; line-height: 1.6; }
        .recipient-details strong { color: #fff; }
        .section-heading { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 2px; color: #95d5b2; margin-top: 30px; margin-bottom: 10px; font-weight: bold; }
        .main-content { padding: 40px; }
        .quote-header { border-bottom: 2px solid #2d6a4f; padding-bottom: 20px; margin-bottom: 30px; }
        .quote-title { font-size: 1.8rem; font-weight: bold; color: #2d6a4f; letter-spacing: 2px; }
        .table-custom th { background-color: #1a1a1a !important; color: #fff !important; font-size: 0.9rem; }
        .table-custom td { font-size: 0.9rem; vertical-align: middle; }
        .banking-box { background-color: #f1f3f5; padding: 15px; border-radius: 6px; font-size: 0.8rem; color: #333; margin-top: 30px; }
        .footer-quote { font-style: italic; color: #2d6a4f; font-size: 0.85rem; margin-top: 25px; }
        .terms-note { font-size: 0.75rem; color: #6c757d; margin-top: 14px; }
        .valid-expired { color: #b02a37 !important; }

        /* Ensure dark/green blocks actually print (browsers drop backgrounds by default) */
        .sidebar-brand, .table-custom th, .total-row {
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
        }

        @page { size: A4; margin: 10mm; }

        @media print {
            body { background: #fff; }
            .no-print { display: none !important; }
            .quote-container { box-shadow: none; margin: 0; width: 100%; max-width: 100%; border-radius: 0; }
            .sidebar-brand { padding: 28px 22px; }
            .main-content { padding: 28px; }
            tr { page-break-inside: avoid; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <!-- Action Toolbar -->
        <div class="text-center my-4 no-print">
            <asp:Button ID="btnPrint" runat="server" Text="🖨️ Print / Save as PDF" CssClass="btn btn-dark px-4 py-2 fw-semibold me-2" OnClientClick="window.print(); return false;" />
            <asp:HyperLink ID="lnkQuotes" runat="server" NavigateUrl="~/ViewQuote.aspx" CssClass="btn btn-outline-secondary px-4 py-2 fw-semibold me-2">All Quotes</asp:HyperLink>
            <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="~/RequestQuote.aspx" CssClass="btn btn-outline-success px-4 py-2 fw-semibold">← Back to Request</asp:HyperLink>
            <div class="mt-3">
                <asp:Label ID="lblStatus" runat="server" Visible="false" />
            </div>
        </div>

        <!-- Not found / no access -->
        <asp:Panel ID="pnlNotFound" runat="server" Visible="false" CssClass="container no-print" style="max-width: 640px;">
            <div class="text-center bg-white border rounded-4 shadow-sm py-5 px-4">
                <div class="display-5 mb-3">📄</div>
                <h4 class="fw-bold text-secondary mb-2">Quotation unavailable</h4>
                <p class="text-muted mb-0">
                    <asp:Label ID="lblNotFound" runat="server" />
                </p>
            </div>
        </asp:Panel>

        <!-- Quote Document Card -->
        <asp:Panel ID="pnlQuote" runat="server">
            <div class="quote-container row g-0">
                <!-- Left Sidebar -->
                <div class="col-md-4 sidebar-brand">
                    <div class="brand-title">THE GIANT GROUP</div>
                    <div class="brand-details">
                        7 Baumann rd, Head Office<br />
                        Durban, Queensburgh Industrial<br />
                        info@gianttreefelling.co.za<br />
                        084 833 1373
                    </div>

                    <div class="section-heading">Recipient</div>
                    <div class="recipient-details">
                        <strong><asp:Label ID="lblRecipientName" runat="server" /></strong><br />
                        <asp:Label ID="lblRecipientAddress" runat="server" />
                    </div>
                </div>

                <!-- Right Main Area -->
                <div class="col-md-8 main-content">
                    <div class="quote-header">
                        <div class="quote-title">— QUOTATION —</div>
                    </div>

                    <div class="text-end mb-3">
                        <div style="font-size: 0.85rem;" class="text-muted">Date: <asp:Label ID="lblQuoteDate" runat="server" /></div>
                        <div style="font-size: 0.85rem;" class="text-muted">Valid until: <asp:Label ID="lblValidUntil" runat="server" Text="-" /></div>
                        <div style="font-size: 0.9rem; font-weight: bold; color: #2d6a4f;"><asp:Label ID="lblQuoteNo" runat="server" /></div>
                    </div>

                    <!-- Cost Breakdown Item Table matching website breakdown -->
                    <table class="table table-bordered table-custom">
                        <thead>
                            <tr>
                                <th>Description</th>
                                <th class="text-center" style="width: 130px;">Qty</th>
                                <th class="text-end" style="width: 130px;">Price</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptLines" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td>
                                            <%#: Eval("Description") %>
                                            <div class="small text-muted"><%#: Eval("Detail") %></div>
                                        </td>
                                        <td class="text-center"><%#: Eval("Qty") %></td>
                                        <td class="text-end"><%#: Eval("Amount") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                            <tr id="rowServicesSubtotal" runat="server">
                                <td colspan="2" class="text-end fw-bold">Services Subtotal</td>
                                <td class="text-end fw-bold"><asp:Label ID="lblServicesSubtotal" runat="server" Text="R 0,00" /></td>
                            </tr>
                            <tr>
                                <td colspan="2">Estimated Travel / Fuel Fee &amp; Site Assessment</td>
                                <td class="text-end"><asp:Label ID="lblTravelFee" runat="server" Text="R 0,00" /></td>
                            </tr>
                            <tr>
                                <td colspan="2">All cuttings to be removed to nearest municipal dump site</td>
                                <td class="text-end">-</td>
                            </tr>
                            <tr>
                                <td colspan="2" class="text-end fw-bold">Subtotal (Incl. Travel)</td>
                                <td class="text-end fw-bold"><asp:Label ID="lblSubtotalInclTravel" runat="server" Text="R 0,00" /></td>
                            </tr>
                            <tr>
                                <td colspan="2" class="text-end fw-bold">VAT (15%)</td>
                                <td class="text-end fw-bold"><asp:Label ID="lblVat" runat="server" Text="R 0,00" /></td>
                            </tr>
                            <tr class="total-row" style="background-color: #2d6a4f !important; color: white !important;">
                                <td colspan="2" class="text-end text-white fw-bold" style="background-color: #2d6a4f; color: #fff;">TOTAL (Incl. VAT)</td>
                                <td class="text-end text-white fw-bold" style="background-color: #2d6a4f; color: #fff;"><asp:Label ID="lblTotalAmount" runat="server" Text="R 0,00" /></td>
                            </tr>
                        </tbody>
                    </table>

                    <div class="terms-note">
                        All prices are in South African Rand (ZAR) and include VAT at 15%. This quotation is valid until the date shown above.
                    </div>

                    <!-- Banking Details Box -->
                    <div class="banking-box">
                        <strong>BANKING DETAILS</strong><br />
                        Account Holder: Emmans Transport cc t/a TheGiantGroup | Account No: 250898500<br />
                        Standard Bank, Cheque account.<br />
                        Reg No: 2008/169586/23. Vat No: 4200259861.<br />
                        TheGiantGroup, CK No: 2008/169586/23.
                    </div>

                    <div class="d-flex justify-content-between align-items-center mt-4">
                        <div class="footer-quote">"Pursuing excellence in every endeavor."</div>
                        <div class="text-end" style="font-size: 0.8rem; font-weight: bold;">
                            Shaphan Pillay<br />
                            <span style="font-weight: normal;">The Giant Group</span><br />
                            CEO
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </form>
</body>
</html>
