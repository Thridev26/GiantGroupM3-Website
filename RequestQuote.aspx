<%@ Page Title="Request a Quote" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RequestQuote.aspx.cs" Inherits="GiantGroupM3.RequestQuote" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center my-4">
        <div class="col-md-10 col-lg-9">
            <div class="card shadow-sm border-0 rounded-4 p-4 mb-4">
                <div class="card-body">
                    <h2 class="fw-bold mb-3 text-success">Request a Quote</h2>
                    <p class="text-muted small mb-4">Select your evaluated job request below to automatically factor in site coordinates, travel fees, and VAT.</p>

                    <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-success rounded-3 mb-4" role="alert">
                        <asp:Label ID="lblMessage" runat="server" />
                    </asp:Panel>

                    <!-- 1. Select Client's Job Request -->
                    <div class="mb-4">
                        <h5 class="fw-semibold mb-2">1. Select Your Job Request</h5>
                        <asp:GridView ID="gvJobRequests" runat="server" CssClass="table table-hover align-middle border" AutoGenerateColumns="False" DataKeyNames="jobRequestID" OnSelectedIndexChanged="gvJobRequests_SelectedIndexChanged" EmptyDataText="No job requests found.">
                            <Columns>
                                <asp:BoundField DataField="jobRequestID" HeaderText="Request ID" />
                                <asp:BoundField DataField="siteAddress" HeaderText="Site Address" />
                                <asp:BoundField DataField="urgencyLevel" HeaderText="Urgency" />
                                <asp:BoundField DataField="dateRecieved" HeaderText="Date Received" DataFormatString="{0:yyyy-MM-dd}" />
                                <asp:ButtonField CommandName="Select" Text="Select Request" ButtonType="Button" ControlStyle-CssClass="btn btn-sm btn-outline-success" />
                            </Columns>
                        </asp:GridView>
                    </div>

                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">Selected Job Request ID</label>
                            <asp:TextBox ID="txtSelectedJobId" runat="server" CssClass="form-control bg-light" ReadOnly="true" placeholder="Choose a request above" />
                        </div>
                        <div class="col-md-3">
                            <label class="form-label fw-semibold">Longitude</label>
                            <asp:TextBox ID="txtLongitude" runat="server" CssClass="form-control bg-light" ReadOnly="true" />
                        </div>
                        <div class="col-md-3">
                            <label class="form-label fw-semibold">Latitude</label>
                            <asp:TextBox ID="txtLatitude" runat="server" CssClass="form-control bg-light" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- Services in Selected Request -->
                    <div class="mb-4">
                        <h5 class="fw-semibold mb-2">Services in Selected Request</h5>
                        <asp:GridView ID="gvRequestItems" runat="server" AutoGenerateColumns="False" CssClass="table table-bordered">
                            <Columns>
                                <asp:BoundField DataField="JobTypeName" HeaderText="Job Type" />
                                <asp:BoundField DataField="JobRate" HeaderText="Standard Rate" DataFormatString="{0:C}" />
                                <asp:BoundField DataField="Quantity" HeaderText="Quantity / Measurement" />
                                <asp:BoundField DataField="LineTotal" HeaderText="Total" DataFormatString="{0:C}" />
                            </Columns>
                        </asp:GridView>
                    </div>

                    <!-- Financial Breakdown Summary Card -->
                    <div class="card bg-light border-0 p-3 mb-4 rounded-3">
                        <h6 class="fw-bold text-success mb-3">Cost Breakdown</h6>
                        <div class="d-flex justify-content-between mb-1">
                            <span>Services Subtotal:</span>
                            <asp:Label ID="lblSubtotal" runat="server" CssClass="fw-semibold" Text="R 0,00" />
                        </div>
                        <div class="d-flex justify-content-between mb-1">
                            <span>Estimated Travel / Fuel Fee:</span>
                            <asp:Label ID="lblTravelFee" runat="server" CssClass="fw-semibold" Text="R 0,00" />
                        </div>
                        <hr />
                        <div class="d-flex justify-content-between mb-1">
                            <span>Subtotal (Incl. Travel):</span>
                            <asp:Label ID="lblSubtotalWithTravel" runat="server" CssClass="fw-semibold" Text="R 0,00" />
                        </div>
                        <div class="d-flex justify-content-between mb-1">
                            <span>VAT (15%):</span>
                            <asp:Label ID="lblVat" runat="server" CssClass="fw-semibold" Text="R 0,00" />
                        </div>
                        <div class="d-flex justify-content-between mt-2 pt-2 border-top">
                            <span class="fw-bold fs-5">Total Amount (Incl. VAT):</span>
                            <asp:Label ID="lblTotalWithVat" runat="server" CssClass="fw-bold fs-5 text-success" Text="R 0,00" />
                        </div>
                    </div>

                    <div class="d-grid gap-2 mt-4">
                        <asp:Button ID="btnGenerateArbitraryQuote" runat="server" Text="Save Quote" CssClass="py-2 fw-bold text-white border-0 rounded-3" Style="background-color: #2d6a4f;" OnClick="btnGenerateArbitraryQuote_Click" />
                        
                        <!-- Button to view on-screen quote view/document -->
                        <asp:Button ID="btnViewPdfQuote" runat="server" Text="📄 View Quote Document" CssClass="btn btn-outline-success py-2 fw-semibold rounded-3" OnClick="btnViewPdfQuote_Click" Visible="false" />                      
                                            
                        <!-- Clean Cancel Button to reset the form/grid -->
                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="btn btn-outline-secondary py-2 fw-bold rounded-3" CausesValidation="false" />
                    </div>                    
                </div>
            </div>
        </div>
    </div>
</asp:Content>