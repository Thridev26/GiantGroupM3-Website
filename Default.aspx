<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GiantGroupM3._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container my-5">
        
        <!-- 1. HIGH-IMPACT HERO BANNER -->
        <div class="p-5 mb-5 rounded-4 shadow text-center text-white position-relative overflow-hidden" style="background: linear-gradient(135deg, #2d6a4f 0%, #1b4332 100%); border: 1px solid rgba(255,255,255,0.1);" data-aos="zoom-in">
            <div class="py-4">
                <div class="mb-4 bg-white p-3 rounded-3 d-inline-block shadow-sm">
                    <img runat="server" src="~/Content/Alt Logo .png" alt="Giant Property Maintenance - Leading Titan" class="img-fluid" style="max-height: 140px; width: auto;" />
                </div>
                <h1 class="text-white fw-bold mt-3 mb-2" style="font-size: 2.2rem;">Welcome to The Giant Group</h1>
                <p class="text-white-50 lead col-lg-8 mx-auto mb-4">
                    South Africa’s premier titan in professional tree felling, hazardous vegetation management, and commercial property maintenance.
                </p>
                <div>
                    <% if (IsClient) { %>
                        <a runat="server" href="~/Requests/SubmitRequest.aspx" class="btn btn-light text-success fw-bold btn-lg px-4 me-2 shadow-sm">Submit Job Request</a>
                    <% } else if (IsStaff) { %>
                        <a runat="server" href="~/Calendar.aspx" class="btn btn-outline-light btn-lg px-4 me-2">View Schedule</a>
                    <% } else { %>
                        <button type="button" class="btn btn-light text-success fw-bold btn-lg px-4 shadow-sm" data-bs-toggle="modal" data-bs-target="#authPromptModal">
                            Log In to Access Portal
                        </button>
                    <% } %>
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

        <!-- 3.1 COMPANY INFORMATION & TREE-FELLING EXPERTISE -->
        <div class="my-5 py-4 bg-white rounded-4 border p-5 shadow-sm" data-aos="fade-up">
            <div class="row align-items-center">
                <div class="col-lg-6 mb-4 mb-lg-0">
                    <h3 class="fw-bold text-dark mb-3">Specialized Tree Felling & Estate Maintenance</h3>
                    <p class="text-secondary">
                        The Giant Group delivers heavy-duty tree-felling solutions, site clearing, and hazardous tree dismantling for commercial estates, municipal facilities, and private properties. 
                    </p>
                    <p class="text-secondary mb-4">
                        Our integrated digital tracking platforms ensure total accountability from initial site quotation to final debris removal and safety sign-off.
                    </p>
                    <div class="text-muted small fst-italic">🔒 Sign in above to manage service requests, review active job schedules, or access administrative workflows.</div>
                </div>
                <div class="col-lg-6 text-center">
                    <div class="p-4 bg-light rounded-4 border">
                        <h5 class="fw-bold text-success mb-2">Core Operational Standards</h5>
                        <ul class="list-unstyled text-start text-secondary mb-0">
                            <li class="mb-2">✔️ Certified Hazard Risk Assessments</li>
                            <li class="mb-2">✔️ Fully Insured Rigging & Felling Crews</li>
                            <li class="mb-2">✔️ Real-time Digital Job Tracking Portal</li>
                            <li>✔️ Transparent Enterprise Quotation Pipeline</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- 3.2 CLIENT FEEDBACK & TESTIMONIALS -->
        <div class="my-5 py-4" data-aos="fade-up">
            <div class="text-center mb-4">
                <h3 class="fw-bold text-dark">What Our Clients Say</h3>
                <p class="text-muted">Direct feedback from property directors and municipal supervisors.</p>
            </div>
            <div class="row g-4 text-start">
                <div class="col-md-4">
                    <div class="card h-100 shadow-sm border-0 p-4 rounded-4">
                        <div class="card-body">
                            <div class="text-warning mb-2">⭐⭐⭐⭐</div>
                            <p class="card-text fst-italic">"The Giant Group handled our complex hazardous tree removal seamlessly. Their asset tracking and rapid response teams are top-tier!"</p>
                            <h6 class="fw-bold mt-3 mb-0">— Marcus Vance</h6>
                            <small class="text-muted">Operations Director, Greenfield Estates</small>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card h-100 shadow-sm border-0 p-4 rounded-4">
                        <div class="card-body">
                            <div class="text-warning mb-2">⭐⭐⭐⭐⭐</div>
                            <p class="card-text fst-italic">"Professional, safety-first, and incredibly well-organized. Their digital job tracking portal gives us total peace of mind."</p>
                            <h6 class="fw-bold mt-3 mb-0">— Sarah Jenkins</h6>
                            <small class="text-muted">Municipal Facilities Manager</small>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card h-100 shadow-sm border-0 p-4 rounded-4">
                        <div class="card-body">
                            <div class="text-warning mb-2">⭐⭐⭐⭐</div>
                            <p class="card-text fst-italic">"Outstanding turnaround times on quotes and site photo updates. Truly a cutting-edge team for tree-felling management."</p>
                            <h6 class="fw-bold mt-3 mb-0">— David Naidoo</h6>
                            <small class="text-muted">Head of Infrastructure, Titan Civil</small>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- 4. CORE SYSTEM MODULES GRID (Staff Levels 4+ Only) -->
        <asp:Panel runat="server" ID="OperationsHubPanel" Visible="false">
            <div class="row my-5">
                <div class="col-12 text-center mb-4" data-aos="fade-up">
                    <h2 class="fw-bold text-dark">Management & Operations Hub</h2>
                    <p class="text-muted">Select a module below to manage workflows, tracking, and reports.</p>
                </div>
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
                <div class="col-md-4 mb-4" data-aos="fade-up" data-aos-delay="200">
                    <div class="card h-100 shadow-sm border-0 p-2">
                        <div class="card-body">
                            <div class="mb-3 text-success fs-2">👥</div>
                            <h3 class="card-title h5 fw-bold">Client Portals</h3>
                            <p class="card-text text-secondary">Add, update, and manage all organizational client information in one place.</p>
                            <a runat="server" href="~/Clients/ViewClients.aspx" class="stretched-link text-success text-decoration-none fw-bold">Client Portal &rarr;</a>
                        </div>
                    </div>
                </div>
                <div class="col-md-4 mb-4" data-aos="fade-up" data-aos-delay="300">
                    <div class="card h-100 shadow-sm border-0 p-2">
                        <div class="card-body">
                            <div class="mb-3 text-success fs-2">💰</div>
                            <h3 class="card-title h5 fw-bold">Quotations & Invoicing</h3>
                            <p class="card-text text-secondary">Generate quotes, track payment processing, and streamline financial records.</p>
                            <a runat="server" href="~/Quotes/CreateQuote.aspx" class="stretched-link text-success text-decoration-none fw-bold">Quotations Portal &rarr;</a>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <!-- 5. CLIENT QUICK ACTIONS (Logged-In Clients Only) -->
        <asp:Panel runat="server" ID="ClientHubPanel" Visible="false">
            <div class="row my-5">
                <div class="col-12 text-center mb-4" data-aos="fade-up">
                    <h2 class="fw-bold text-dark">Client Portal Quick Actions</h2>
                    <p class="text-muted">Manage your active service requests and company account details.</p>
                </div>
                <div class="col-md-6 mb-4" data-aos="fade-up" data-aos-delay="100">
                    <div class="card h-100 shadow-sm border-0 p-3">
                        <div class="card-body">
                            <div class="mb-3 text-success fs-2">📝</div>
                            <h3 class="card-title h5 fw-bold">Submit New Request</h3>
                            <p class="card-text text-secondary">Request tree felling, site clearing, or hazard assessment for your property.</p>
                            <a runat="server" href="~/Requests/SubmitRequest.aspx" class="stretched-link text-success text-decoration-none fw-bold">New Request &rarr;</a>
                        </div>
                    </div>
                </div>
                <div class="col-md-6 mb-4" data-aos="fade-up" data-aos-delay="200">
                    <div class="card h-100 shadow-sm border-0 p-3">
                        <div class="card-body">
                            <div class="mb-3 text-success fs-2">📋</div>
                            <h3 class="card-title h5 fw-bold">View Request History</h3>
                            <p class="card-text text-secondary">Check current status updates, quotes, and job completion reports.</p>
                            <a runat="server" href="~/Requests/ViewRequests.aspx" class="stretched-link text-success text-decoration-none fw-bold">View History &rarr;</a>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <!-- 6. GUEST PROMPT PANEL (Visible only when logged out) -->
        <asp:Panel runat="server" ID="GuestPromptPanel" Visible="true">
            <div class="my-5 py-5 text-center bg-white rounded-4 border shadow-sm" data-aos="fade-up">
                <div class="fs-1 mb-3">🌲</div>
                <h3 class="fw-bold text-dark mb-2">Discover The Giant Group Experience</h3>
                <p class="text-muted col-lg-7 mx-auto mb-4">
                    Log in to submit custom tree-felling job requests, track active property maintenance schedules, or access secure corporate operations portals.
                </p>
                <button type="button" class="btn btn-success px-5 py-2 fw-semibold shadow-sm" data-bs-toggle="modal" data-bs-target="#authPromptModal">
                    Log In / Register Now
                </button>
            </div>
        </asp:Panel>
    </div>

    <!-- Reusable Authentication Prompt Modal -->
    <div class="modal fade" id="authPromptModal" tabindex="-1" aria-labelledby="authPromptModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow-lg rounded-4">
                <div class="modal-header bg-success text-white">
                    <h5 class="modal-title fw-bold" id="authPromptModalLabel">Authentication Required</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body text-center p-4">
                    <div class="fs-1 mb-3">🔒</div>
                    <h4 class="fw-bold text-dark mb-2">Access The Giant Group Portal</h4>
                    <p class="text-muted mb-4">Please log in to submit job requests, view quotes, or access staff operations.</p>
                    <div class="d-grid gap-2">
                        <a runat="server" href="~/Account/Login.aspx" class="btn btn-success btn-lg">Log In to Your Account</a>
                        <a runat="server" href="~/Account/Register.aspx" class="btn btn-outline-secondary btn-lg">Create a New Client Account</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>