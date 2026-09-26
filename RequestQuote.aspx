<%@ Page Title="Request Quote" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RequestQuote.aspx.cs" Inherits="GiantGroupM3.RequestQuote" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        :root {
            --primary-color: #2d6a4f;
            --primary-dark: #1b4332;
            --accent-color: #52b788;
            --bg-light: #f8f9fa;
            --card-bg: #ffffff;
            --border-color: #e9ecef;
            --text-dark: #212529;
            --text-muted: #6c757d;
        }

        .quote-container {
            max-width: 900px;
            margin: 2rem auto;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            color: var(--text-dark);
        }

        .page-header {
            margin-bottom: 2rem;
            border-bottom: 2px solid var(--border-color);
            padding-bottom: 1rem;
        }

        .page-header h2 {
            color: var(--primary-dark);
            font-weight: 700;
            margin-bottom: 0.25rem;
        }

        .card-custom {
            background: var(--card-bg);
            border: 1px solid var(--border-color);
            border-radius: 10px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.03);
            margin-bottom: 1.5rem;
            overflow: hidden;
        }

        .card-header-custom {
            background-color: var(--primary-color);
            color: #ffffff;
            padding: 0.85rem 1.25rem;
            font-weight: 600;
            font-size: 1rem;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .card-body-custom {
            padding: 1.25rem;
        }

        /* Modernized Form Controls */
        .form-label-custom {
            font-size: 0.85rem;
            font-weight: 600;
            color: var(--text-muted);
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 0.35rem;
        }

        .form-control-custom {
            background-color: var(--bg-light);
            border: 1px solid #ced4da;
            border-radius: 6px;
            padding: 0.5rem 0.75rem;
            font-weight: 600;
            color: var(--text-dark);
            width: 100%;
        }

        .form-control-custom:read-only {
            background-color: #e9ecef;
        }

        /* Custom Table Styling */
        .table-custom {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 0;
        }

        .table-custom th {
            background-color: #f1f3f5;
            color: var(--text-muted);
            font-size: 0.8rem;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            padding: 0.75rem 1rem;
            border-bottom: 2px solid var(--border-color);
            text-align: left;
        }

        .table-custom td {
            padding: 0.85rem 1rem;
            border-bottom: 1px solid var(--border-color);
            vertical-align: middle;
        }

        /* Travel Breakdown Display */
        .travel-summary-card {
            background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
            border: 1px solid #dee2e6;
            border-radius: 8px;
            padding: 1.25rem;
        }

        .travel-total-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding-bottom: 0.75rem;
            border-bottom: 1px dashed #ced4da;
            margin-bottom: 0.75rem;
        }

        .travel-total-label {
            font-size: 1.05rem;
            font-weight: 600;
            color: var(--primary-dark);
        }

        .travel-total-amount {
            font-size: 1.5rem;
            font-weight: 700;
            color: var(--primary-color);
        }

        .travel-metrics-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 0.75rem;
            margin-top: 0.5rem;
        }

        .travel-metric-item {
            background: #ffffff;
            border: 1px solid var(--border-color);
            border-radius: 6px;
            padding: 0.5rem 0.75rem;
            font-size: 0.85rem;
        }

        .travel-metric-label {
            color: var(--text-muted);
            display: block;
            font-size: 0.75rem;
            text-transform: uppercase;
        }

        .travel-metric-val {
            font-weight: 600;
            color: var(--text-dark);
        }

        /* Cost Breakdown Financials */
        .breakdown-card {
            background-color: #fafbfc;
            border: 1px solid var(--border-color);
            border-radius: 8px;
            padding: 1.25rem;
        }

        .breakdown-line {
            display: flex;
            justify-content: space-between;
            padding: 0.4rem 0;
            font-size: 0.95rem;
        }

        .breakdown-line.divider {
            border-top: 1px solid var(--border-color);
            margin-top: 0.5rem;
            padding-top: 0.75rem;
        }

        .breakdown-line.total {
            border-top: 2px solid var(--primary-color);
            margin-top: 0.5rem;
            padding-top: 0.75rem;
            font-size: 1.2rem;
            font-weight: 700;
            color: var(--primary-dark);
        }

        /* Action Buttons */
        .btn-primary-custom {
            background-color: var(--primary-color);
            color: white;
            border: none;
            padding: 0.75rem 1.5rem;
            border-radius: 6px;
            font-weight: 600;
            width: 100%;
            transition: background 0.2s ease-in-out;
        }

        .btn-primary-custom:hover {
            background-color: var(--primary-dark);
            color: white;
        }

        .btn-secondary-custom {
            background-color: transparent;
            color: var(--text-muted);
            border: 1px solid #ced4da;
            padding: 0.6rem 1.5rem;
            border-radius: 6px;
            font-weight: 600;
            width: 100%;
            margin-top: 0.5rem;
            transition: all 0.2s ease-in-out;
        }

        .btn-secondary-custom:hover {
            background-color: #e9ecef;
            color: var(--text-dark);
        }
    </style>

    <div class="quote-container">
        <!-- Page Header -->
        <div class="page-header">
            <h2>Request a Quote</h2>
            <p class="text-muted mb-0">Select your evaluated job request below to automatically factor in site coordinates, travel fees, and VAT.</p>
        </div>

        <!-- Alert Notification Panel -->
        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-info rounded-3 mb-4">
            <asp:Label ID="lblMessage" runat="server" />
        </asp:Panel>

       <!-- Section 1: Job Selection -->
<asp:Panel ID="pnlJobSelection" runat="server">
    <div class="card-custom">
        <div class="card-header-custom">
            1. Select Your Job Request
        </div>
        <div class="card-body-custom">
            <asp:GridView ID="gvJobRequests" runat="server" AutoGenerateColumns="False" 
                DataKeyNames="jobRequestID" OnSelectedIndexChanged="gvJobRequests_SelectedIndexChanged" 
                CssClass="table-custom mb-3">
                <Columns>
                    <asp:BoundField DataField="jobRequestID" HeaderText="Request ID" />
                    <asp:BoundField DataField="siteAddress" HeaderText="Site Address" />
                    <asp:BoundField DataField="urgencyLevel" HeaderText="Urgency" />
                    <asp:BoundField DataField="dateRecieved" HeaderText="Date Received" DataFormatString="{0:yyyy-MM-dd}" />
                    <asp:CommandField ShowSelectButton="True" SelectText="Select Request" ButtonType="Button" ControlStyle-CssClass="btn btn-sm btn-outline-success fw-bold px-3" />
                </Columns>
            </asp:GridView>

            <!-- Layout displaying Site Address (Only visible when a request IS selected) -->
            <asp:Panel ID="pnlSelectionDetails" runat="server" Visible="false" CssClass="row g-3 mt-2 pt-3 border-top">
                <div class="col-md-3">
                    <div class="form-group">
                        <label class="form-label-custom d-block">Selected Request ID</label>
                        <asp:TextBox ID="txtSelectedJobId" runat="server" CssClass="form-control-custom" ReadOnly="true" />
                    </div>
                </div>
                <div class="col-md-9">
                    <div class="form-group">
                        <label class="form-label-custom d-block">Site Address</label>
                        <asp:TextBox ID="txtSiteAddress" runat="server" CssClass="form-control-custom" ReadOnly="true" />
                    </div>
                </div>
            </asp:Panel>

            <!-- Prompt shown BEFORE any request is selected -->
            <asp:Panel ID="pnlSelectionPrompt" runat="server" CssClass="text-center py-3 text-muted bg-light rounded-3 mt-2 border">
                <p class="mb-0 small fw-semibold">
                    <i class="bi bi-info-circle me-1"></i> Click <strong>"Select Request"</strong> on an item above to load the service breakdown and calculate logistics.
                </p>
            </asp:Panel>

            <!-- Hidden fields for coordinates -->
            <asp:HiddenField ID="hfLongitude" runat="server" />
            <asp:HiddenField ID="hfLatitude" runat="server" />
        </div>
    </div>
</asp:Panel>

        <!-- Dynamic Form Content Panel -->
        <asp:Panel ID="pnlFormContent" runat="server">
            
            <!-- Section 2: Requested Services Grid -->
            <div class="card-custom">
                <div class="card-header-custom">
                    2. Requested Services
                </div>
                <div class="card-body-custom p-0">
                    <asp:GridView ID="gvRequestItems" runat="server" AutoGenerateColumns="False" CssClass="table-custom">
                        <Columns>
                            <asp:BoundField DataField="JobTypeName" HeaderText="Service / Job Type" />
                            <asp:BoundField DataField="JobRate" HeaderText="Rate (ZAR)" DataFormatString="{0:C}" />
                            <asp:BoundField DataField="Quantity" HeaderText="Quantity / Value" />
                            <asp:BoundField DataField="UnitOfMeasure" HeaderText="Unit" />
                            <asp:BoundField DataField="LineTotal" HeaderText="Line Total" DataFormatString="{0:C}" />
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

            <!-- Section 3: Travel & Logistics Calculation -->
            <div class="card-custom">
                <div class="card-header-custom">
                    3. Travel & Logistics Calculation
                </div>
                <div class="card-body-custom">
                    <!-- Address & Distance Route Banner -->
                    <div class="p-3 mb-3 rounded-3" style="background-color: #f1f3f5; border: 1px solid #dee2e6;">
                        <div class="row align-items-center g-3">
                            <div class="col-md-5">
                                <span class="form-label-custom d-block text-muted">Business Origin</span>
                                <strong class="text-dark">7 Baumann Rd, Durban, Queensburgh Industrial</strong>
                            </div>
                            <div class="col-md-2 text-center">
                                <span class="badge bg-secondary px-3 py-2">
                                    ➔ <asp:Label ID="lblOneWayDistance" runat="server" Text="0.0 km" /> ➔
                                </span>
                                <small class="d-block text-muted mt-1" style="font-size: 0.75rem;">One-Way Distance</small>
                            </div>
                            <div class="col-md-5 text-end">
                                <span class="form-label-custom d-block text-muted">Client Site Location</span>
                                <strong class="text-dark"><asp:Label ID="lblDisplaySiteAddress" runat="server" Text="[No Address Selected]" /></strong>
                            </div>
                        </div>
                    </div>

                    <!-- Travel Fee Summary Card -->
                    <div class="travel-summary-card">
                        <div class="travel-total-row">
                            <span class="travel-total-label">Estimated Travel / Fuel Fee</span>
                            <asp:Label ID="lblTravelFee" runat="server" CssClass="travel-total-amount" Text="R 0,00" />
                        </div>
                        
                        <div class="travel-metrics-grid">
                            <div class="travel-metric-item">
                                <span class="travel-metric-label">Fee Breakdown</span>
                                <asp:Label ID="lblTravelBreakdown" runat="server" CssClass="travel-metric-val" Text="Base Call-out + Distance calculation" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Section 4: Cost Breakdown Summary -->
            <div class="card-custom">
                <div class="card-header-custom">
                    4. Financial Cost Summary
                </div>
                <div class="card-body-custom">
                    <div class="breakdown-card">
                        <div class="breakdown-line">
                            <span class="text-muted">Services Subtotal:</span>
                            <asp:Label ID="lblSubtotal" runat="server" Font-Bold="true" Text="R 0,00" />
                        </div>
                        <div class="breakdown-line">
                            <span class="text-muted">Estimated Travel / Fuel Fee:</span>
                            <asp:Label ID="lblTravelFeeSummary" runat="server" Font-Bold="true" Text="R 0,00" />
                        </div>
                        <div class="breakdown-line divider">
                            <span>Subtotal (Incl. Travel):</span>
                            <asp:Label ID="lblSubtotalWithTravel" runat="server" Font-Bold="true" Text="R 0,00" />
                        </div>
                        <div class="breakdown-line">
                            <span class="text-muted">VAT (15%):</span>
                            <asp:Label ID="lblVat" runat="server" Font-Bold="true" Text="R 0,00" />
                        </div>
                        <div class="breakdown-line total">
                            <span>Total Amount (Incl. VAT):</span>
                            <asp:Label ID="lblTotalWithVat" runat="server" Text="R 0,00" />
                        </div>
                    </div>

                    <!-- Action Buttons -->
                    <div class="mt-4">
                        <asp:Button ID="btnGenerateArbitraryQuote" runat="server" Text="Save & Generate Quote" 
                            CssClass="btn-primary-custom" OnClick="btnGenerateArbitraryQuote_Click" />
                        
                        <asp:Button ID="btnViewPdfQuote" runat="server" Text="View Quote PDF" 
                            CssClass="btn-primary-custom mt-2" OnClick="btnViewPdfQuote_Click" Visible="false" />

                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" 
                            CssClass="btn-secondary-custom" OnClick="btnCancel_Click" CausesValidation="false" />
                    </div>
                </div>
            </div>

        </asp:Panel>

        <!-- Cleaned Empty State Panel -->
        <asp:Panel ID="pnlEmptyState" runat="server" Visible="false" CssClass="card-custom text-center p-5">
            <div class="py-3">
                <div class="mb-3">
                    <svg xmlns="http://www.w3.org/2000/svg" width="60" height="60" fill="#6c757d" class="bi bi-file-earmark-x" viewBox="0 0 16 16">
                        <path d="M6.854 7.146a.5.5 0 1 0-.708.708L7.293 9l-1.147 1.146a.5.5 0 0 0 .708.708L8 9.707l1.146 1.147a.5.5 0 0 0 .708-.708L8.707 9l1.147-1.146a.5.5 0 0 0-.708-.708L8 8.293 6.854 7.146z"/>
                        <path d="M14 14V4.5L9.5 0H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2zM9.5 3A1.5 1.5 0 0 0 11 4.5h2V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h5.5v2z"/>
                    </svg>
                </div>
                <h4 class="fw-bold text-dark mb-2">No Pending Job Requests Found</h4>
                <p class="text-muted mb-4">There are currently no job requests eligible for quote generation.</p>
                <a href="JobRequest.aspx" class="btn btn-success px-4 py-2 fw-semibold">
                    Submit New Job Request
                </a>
            </div>
        </asp:Panel>
    </div>
</asp:Content>