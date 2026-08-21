<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GiantGroupM3._Default" %>
 <asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<div class="container my-5">
        
        <!-- 1. HIGH-IMPACT HERO BANNER (Dark Gradient to Make the Logo Pop) -->
        <div class="p-5 mb-5 rounded-4 shadow text-center text-white" style="background: linear-gradient(135deg, #2d6a4f 0%, #1b4332 100%); border: 1px solid rgba(255,255,255,0.1);" data-aos="zoom-in">
            <div class="py-4">
                <div class="mb-4 bg-white p-3 rounded-3 d-inline-block shadow-sm">
                    <img runat="server" src="~/Content/Alt Logo .png" alt="Giant Property Maintenance - Leading Titan" class="img-fluid" style="max-height: 140px; width: auto;" />
                </div>
                <h1 class="text-white fw-bold mt-3 mb-2" style="font-size: 2.2rem;">Welcome to The Giant Group</h1>
                <p class="text-white-50 lead col-lg-8 mx-auto mb-4">
                    South Africa’s premier titan in property maintenance, asset allocation, and enterprise operations.
                </p>
                <div>
                    <a runat="server" href="~/Requests/SubmitRequest.aspx" class="btn btn-light text-success fw-bold btn-lg px-4 me-2 shadow-sm">Submit Job Request</a>
                    <a runat="server" href="~/Calendar.aspx" class="btn btn-outline-light btn-lg px-4">View Schedule</a>
                </div>
            </div>
        </div>

        <!-- 2. PERFORMANCE METRICS & STATISTICS -->
        <div class="row text-center mb-5">
            <div class="col-md-4 mb-3" data-aos="fade-up" data-aos-delay="100">
                <div class="p-4 border rounded-3 shadow-sm bg-white h-100 border-top border-success border-4">
                    <h2 class="display-4 fw-bold text-success">100%</h2>
                    <p class="text-muted mb-0 fw-semibold">Schedule & Tracking Accuracy</p>
                </div>
            </div>
            <div class="col-md-4 mb-3" data-aos="fade-up" data-aos-delay="200">
                <div class="p-4 border rounded-3 shadow-sm bg-white h-100 border-top border-success border-4">
                    <h2 class="display-4 fw-bold text-success">24/7</h2>
                    <p class="text-muted mb-0 fw-semibold">Emergency Field Response</p>
                </div>
            </div>
            <div class="col-md-4 mb-3" data-aos="fade-up" data-aos-delay="300">
                <div class="p-4 border rounded-3 shadow-sm bg-white h-100 border-top border-success border-4">
                    <h2 class="display-4 fw-bold text-success">Fast</h2>
                    <p class="text-muted mb-0 fw-semibold">Quote & Invoicing Pipeline</p>
                </div>
            </div>
        </div>

        <!-- 3. TRUSTED ORGANISATIONS / PARTNERS GRID -->
        <div class="my-5 py-4 bg-light rounded-4 border px-4 shadow-sm" data-aos="fade-up">
            <div class="text-center mb-4">
                <h3 class="fw-bold text-dark">Trusted By Leading Organisations</h3>
                <p class="text-muted">Proudly servicing major industry leaders, municipalities, and construction partners across the region.</p>
            </div>

            <div class="text-center">
                <img runat="server" src="~/Content/Companies Worked With.png" alt="Organisations and Partners" class="img-fluid rounded shadow-sm bg-white p-2" style="max-height: 280px; width: auto;" />
            </div>
        </div>

        <!-- 4. CORE SYSTEM MODULES GRID (Protected / Role-Aware) -->
        <asp:Panel runat="server" ID="OperationsHubPanel" Visible="false">
            <div class="row my-5">
                <div class="col-12 text-center mb-4" data-aos="fade-up">
                    <h2 class="fw-bold text-dark">Management & Operations Hub</h2>
                    <p class="text-muted">Select a module below to manage workflows, tracking, and reports.</p>
                </div>

                <!-- Card 1 -->
                <div class="col-md-4 mb-4" data-aos="fade-up" data-aos-delay="100">
                    <div class="card h-100 shadow-sm border-0 p-2">
                        <div class="card-body">
                            <div class="mb-3 text-success fs-2">📅</div>
                            <h3 class="card-title h5 fw-bold">Job Scheduling</h3>
                            <p class="card-text text-secondary">Manage calendars, track team timelines, and handle allocations seamlessly.</p>
                            <a runat="server" href="~/Calendar.aspx" class="stretched-link text-success text-decoration-none fw-bold">Open Calendar &rarr;</a>
                        </div>
                    </div>
                </div>

                <!-- Card 2 -->
                <div class="col-md-4 mb-4" data-aos="fade-up" data-aos-delay="200">
                    <div class="card h-100 shadow-sm border-0 p-2">
                        <div class="card-body">
                            <div class="mb-3 text-success fs-2">👥</div>
                            <h3 class="card-title h5 fw-bold">Client Portals</h3>
                            <p class="card-text text-secondary">Add, update, and manage all organizational client information in one place.</p>
                            <a runat="server" href="~/Clients/ViewClients.aspx" class="stretched-link text-success text-decoration-none fw-bold">View Clients &rarr;</a>
                        </div>
                    </div>
                </div>

                <!-- Card 3 -->
                <div class="col-md-4 mb-4" data-aos="fade-up" data-aos-delay="300">
                    <div class="card h-100 shadow-sm border-0 p-2">
                        <div class="card-body">
                            <div class="mb-3 text-success fs-2">💰</div>
                            <h3 class="card-title h5 fw-bold">Quotations & Invoicing</h3>
                            <p class="card-text text-secondary">Generate quotes, track payment processing, and streamline financial records.</p>
                            <a runat="server" href="~/Quotes/CreateQuote.aspx" class="stretched-link text-success text-decoration-none fw-bold">Create Quote &rarr;</a>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <!-- Optional: Alternative view or prompt for guests who aren't logged in -->
        <asp:Panel runat="server" ID="GuestPromptPanel" Visible="true">
            <div class="my-5 py-5 text-center bg-white rounded-4 border shadow-sm" data-aos="fade-up">
                <h3 class="fw-bold text-dark mb-2">Unlock Full Operational Access</h3>
                <p class="text-muted mb-4">Please log in with your client or staff account to access the management hubs and workflows.</p>
                <a runat="server" href="~/Account/Login" class="btn btn-dark px-4 py-2 fw-semibold me-2">Log In Now</a>
                <a runat="server" href="~/Account/Register" class="btn btn-outline-success px-4 py-2 fw-semibold">Create Account</a>
            </div>
        </asp:Panel>
    </div>

</asp:Content> 