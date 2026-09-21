<%@ Page Title="Request Quote" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RequestQuote.aspx.cs" Inherits="GiantGroupM3.RequestQuote" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main aria-labelledby="title" class="container py-4">
        <h2 id="title" class="mb-3">Generate Preliminary Project Quote</h2>
        <p class="text-muted">Select an active job request from your list below to review site details, item quantities, calculated travel costs based on coordinates, and VAT.</p>

        <hr class="mb-4" />

        <!-- Message Alert Panel -->
        <asp:Panel runat="server" ID="pnlMessage" Visible="false" role="alert">
            <asp:Label runat="server" ID="lblMessage" />
        </asp:Panel>

        <!-- Hidden tracking fields for selected IDs and coordinates -->
        <div style="display:none;">
            <asp:TextBox runat="server" ID="txtSelectedJobId" />
            <asp:TextBox runat="server" ID="txtLatitude" />
            <asp:TextBox runat="server" ID="txtLongitude" />
        </div>

        <div class="row">
            <!-- Left Column: Job Request Selection & Breakdown -->
            <div class="col-lg-7 mb-4">
                <div class="card shadow-sm mb-4">
                    <div class="card-header bg-primary text-white">
                        <h5 class="mb-0">Your Active Job Requests</h5>
                    </div>
                    <div class="card-body">
                        <asp:GridView runat="server" ID="gvJobRequests" AutoGenerateColumns="false" DataKeyNames="jobRequestID" 
                            CssClass="table table-hover table-bordered align-middle" 
                            OnSelectedIndexChanged="gvJobRequests_SelectedIndexChanged" SelectedRowStyle-CssClass="table-active">
                            <Columns>
                                <asp:BoundField DataField="jobRequestID" HeaderText="ID" />
                                <asp:BoundField DataField="siteAddress" HeaderText="Site Address" />
                                <asp:BoundField DataField="urgencyLevel" HeaderText="Urgency" />
                                <asp:BoundField DataField="dateRecieved" HeaderText="Date Received" DataFormatString="{0:yyyy-MM-dd}" />
                                <asp:BoundField DataField="status" HeaderText="Status" />
                                <asp:CommandField ShowSelectButton="True" SelectText="Select" ControlStyle-CssClass="btn btn-sm btn-outline-primary" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>

                <div class="card shadow-sm">
                    <div class="card-header bg-secondary text-white">
                        <h5 class="mb-0">Selected Job Request Items & Line Totals</h5>
                    </div>
                    <div class="card-body">
                        <asp:GridView runat="server" ID="gvRequestItems" AutoGenerateColumns="false" 
                            CssClass="table table-striped table-bordered align-middle">
                            <Columns>
                                <asp:BoundField DataField="JobTypeName" HeaderText="Service / Item Type" />
                                <asp:BoundField DataField="JobRate" HeaderText="Rate (R)" DataFormatString="{0:C}" />
                                <asp:BoundField DataField="Quantity" HeaderText="Quantity / Measure" />
                                <asp:BoundField DataField="LineTotal" HeaderText="Line Total (R)" DataFormatString="{0:C}" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>

            <!-- Right Column: Cost Calculations & Summary Panel -->
            <div class="col-lg-5">
                <div class="card shadow-sm mb-4">
                    <div class="card-header bg-dark text-white">
                        <h5 class="mb-0">Quote Cost Breakdown</h5>
                    </div>
                    <div class="card-body">
                        <ul class="list-group list-group-flush mb-3">
                            <li class="list-group-item d-flex justify-content-between align-items-center">
                                Services Subtotal:
                                <asp:Label runat="server" ID="lblSubtotal" CssClass="fw-bold" Text="R 0.00" />
                            </li>
                            <li class="list-group-item d-flex justify-content-between align-items-center">
                                Calculated Travel Fee:
                                <asp:Label runat="server" ID="lblTravelFee" CssClass="fw-bold" Text="R 0.00" />
                            </li>
                            <li class="list-group-item d-flex justify-content-between align-items-center bg-light">
                                <strong>Subtotal with Travel:</strong>
                                <asp:Label runat="server" ID="lblSubtotalWithTravel" CssClass="fw-bold" Text="R 0.00" />
                            </li>
                            <li class="list-group-item d-flex justify-content-between align-items-center">
                                VAT (15%):
                                <asp:Label runat="server" ID="lblVat" CssClass="fw-bold" Text="R 0.00" />
                            </li>
                            <li class="list-group-item d-flex justify-content-between align-items-center text-success fs-5">
                                <strong>Total (incl. VAT):</strong>
                                <asp:Label runat="server" ID="lblTotalWithVat" CssClass="fw-bold" Text="R 0.00" />
                            </li>
                        </ul>

                        <div class="d-grid gap-2">
                            <asp:Button runat="server" ID="btnGenerateArbitraryQuote" Text="Accept Preview & Generate Quote" 
                                CssClass="btn btn-success btn-lg" OnClick="btnGenerateArbitraryQuote_Click" />
                            
                            <asp:Button runat="server" ID="btnViewPdfQuote" Text="View PDF Document" 
                                CssClass="btn btn-outline-primary" Visible="false" OnClick="btnViewPdfQuote_Click" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </main>
</asp:Content>