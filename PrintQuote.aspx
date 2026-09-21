<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PrintQuote.aspx.cs" Inherits="GiantGroupM3.PrintQuote" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Quotation - The Giant Group</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <style>
        body { background-color: #f8f9fa; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
        .quote-container { max-width: 850px; margin: 40px auto; background: #fff; box-shadow: 0 0 15px rgba(0,0,0,0.1); border-radius: 8px; overflow: hidden; }
        .sidebar-brand { background-color: #1a1a1a; color: #fff; padding: 40px 30px; }
        .brand-title { font-size: 1.5rem; font-weight: bold; letter-spacing: 1px; margin-bottom: 25px; }
        .brand-details, .recipient-details { font-size: 0.85rem; color: #ccc; line-height: 1.6; }
        .section-heading { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 2px; color: #88f; margin-top: 30px; margin-bottom: 10px; font-weight: bold; }
        .main-content { padding: 40px; }
        .quote-header { border-bottom: 2px solid #2d6a4f; padding-bottom: 20px; margin-bottom: 30px; }
        .quote-title { font-size: 1.8rem; font-weight: bold; color: #2d6a4f; letter-spacing: 2px; }
        .table-custom th { background-color: #1a1a1a !important; color: #fff !important; font-size: 0.9rem; }
        .table-custom td { font-size: 0.9rem; vertical-align: middle; }
        .banking-box { background-color: #f1f3f5; padding: 15px; border-radius: 6px; font-size: 0.8rem; color: #333; margin-top: 30px; }
        .footer-quote { font-style: italic; color: #2d6a4f; font-size: 0.85rem; margin-top: 25px; }
        @media print {
            body { background: #fff; }
            .no-print { display: none !important; }
            .quote-container { box-shadow: none; margin: 0; width: 100%; max-width: 100%; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <!-- Action Toolbar -->
        <div class="text-center my-4 no-print">
            <asp:Button ID="btnPrint" runat="server" Text="🖨️ Print / Save as PDF" CssClass="btn btn-dark px-4 py-2 fw-semibold me-2" OnClientClick="window.print(); return false;" />
            <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="~/RequestQuote.aspx" CssClass="btn btn-outline-success px-4 py-2 fw-semibold">← Back to Request</asp:HyperLink>
        </div>

        <!-- Quote Document Card -->
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
                    <asp:Label ID="lblRecipientName" runat="server" Font-Bold="true" /><br />
                    <asp:Label ID="lblRecipientAddress" runat="server" Text="8 Cliff Rd, Berea" />
                </div>
            </div>

            <!-- Right Main Area -->
            <div class="col-md-8 main-content">
                <div class="quote-header">
                    <div class="quote-title">— QUOTATION —</div>
                </div>

                <div class="text-end mb-3">
                    <div style="font-size: 0.85rem;" class="text-muted">Date: <asp:Label ID="lblQuoteDate" runat="server" /></div>
                    <div style="font-size: 0.9rem; font-weight: bold; color: #2d6a4f;"><asp:Label ID="lblQuoteNo" runat="server" Text="Quotation NO. GT – 39" /></div>
                </div>

                <!-- Cost Breakdown Item Table matching website breakdown -->
                <table class="table table-bordered table-custom">
                    <thead>
                        <tr>
                            <th>Description</th>
                            <th class="text-end" style="width: 130px;">Price</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td><asp:Label ID="lblServiceDescription" runat="server" Text="Services Rendered" /></td>
                            <td class="text-end"><asp:Label ID="lblServicesSubtotal" runat="server" Text="R 0,00" /></td>
                        </tr>
                        <tr>
                            <td>Estimated Travel / Fuel Fee & Site Assessment</td>
                            <td class="text-end"><asp:Label ID="lblTravelFee" runat="server" Text="R 0,00" /></td>
                        </tr>
                        <tr>
                            <td>All cuttings to be removed to nearest municipal dump site</td>
                            <td class="text-end">-</td>
                        </tr>
                        <tr>
                            <td class="text-end fw-bold">Subtotal (Incl. Travel)</td>
                            <td class="text-end fw-bold"><asp:Label ID="lblSubtotalInclTravel" runat="server" Text="R 0,00" /></td>
                        </tr>
                        <tr>
                            <td class="text-end fw-bold">VAT (15%)</td>
                            <td class="text-end fw-bold"><asp:Label ID="lblVat" runat="server" Text="R 0,00" /></td>
                        </tr>
                        <tr class="table-success" style="background-color: #2d6a4f !important; color: white !important;">
                            <td class="text-end text-white fw-bold">TOTAL (Incl. VAT)</td>
                            <td class="text-end text-white fw-bold"><asp:Label ID="lblTotalAmount" runat="server" Text="R 0,00" /></td>
                        </tr>
                    </tbody>
                </table>

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
    </form>
</body>
</html>