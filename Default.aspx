<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GiantGroupM3._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        /* ================= HERO ================= */
        .gg-hero {
            position: relative; height: 560px; color: #fff; overflow: hidden;
            border-radius: 24px;
            background: linear-gradient(135deg, #1b4332 0%, #0f2d1e 100%);
            box-shadow: var(--gg-shadow-md);
        }
        /* Tree-ring cross-section: the one signature graphic on this page */
        .gg-hero::before {
            content: ""; position: absolute; inset: 0; pointer-events: none;
            background: repeating-radial-gradient(circle at 86% 52%, rgba(149,213,178,.16) 0 1.5px, transparent 1.5px 28px);
            -webkit-mask-image: radial-gradient(circle at 86% 52%, #000 0, transparent 62%);
                    mask-image: radial-gradient(circle at 86% 52%, #000 0, transparent 62%);
        }
        .gg-slide {
            position: absolute; inset: 0;
            opacity: 0; visibility: hidden;
            transition: opacity 1s ease, visibility 1s ease;
        }
        .gg-slide.active { opacity: 1; visibility: visible; }

        .gg-text-slide {
            display: flex; flex-direction: column;
            align-items: flex-start; justify-content: center;
            text-align: left; padding: 3rem 4.5rem;
        }
        .gg-text-slide > * { max-width: 640px; }
        .gg-text-slide h1 {
            font-family: var(--gg-display); font-weight: 600; letter-spacing: -.025em;
            font-size: clamp(2.4rem, 5.2vw, 4rem); line-height: 1.04;
        }
        .gg-text-slide p.lead { color: #d3e8dc !important; line-height: 1.65; font-size: 1.1rem; }
        .gg-hero-actions { display: flex; flex-wrap: wrap; gap: .75rem; }

        /* photo slides: whole photo shown, blurred copy fills the sides */
        .gg-photo-slide::before {
            content: ""; position: absolute; inset: -30px; z-index: 0;
            background-image: var(--photo);
            background-size: cover; background-position: center;
            filter: blur(28px) brightness(0.6);
        }
        .gg-photo-slide img {
            position: relative; z-index: 1;
            width: 100%; height: 100%; object-fit: contain;
        }
        .gg-photo-slide::after {
            content: ""; position: absolute; inset: 0; z-index: 1;
            background: linear-gradient(to top, rgba(8,24,16,.78), transparent 55%);
        }
        .gg-caption {
            position: absolute; left: 2.5rem; bottom: 3.4rem; z-index: 2;
            color: #fff; font-family: var(--gg-display); font-size: clamp(1.3rem, 2.6vw, 1.8rem);
            font-weight: 500; letter-spacing: -.01em;
            padding-left: 1rem; border-left: 3px solid var(--gg-500);
        }

        .gg-dots {
            position: absolute; bottom: 1.3rem; left: 50%;
            transform: translateX(-50%);
            display: flex; gap: 8px; z-index: 3;
        }
        .gg-dots button {
            width: 9px; height: 9px; border: none; padding: 0;
            border-radius: 999px; cursor: pointer;
            background: rgba(255,255,255,.4);
            transition: width .25s ease, background-color .25s ease;
        }
        .gg-dots button.active { background: #fff; width: 26px; }

        .gg-arrows { position: absolute; right: 1.5rem; bottom: 1rem; z-index: 3; display: flex; gap: .5rem; }
        .gg-arrows button {
            width: 40px; height: 40px; border-radius: 50%; cursor: pointer;
            border: 1px solid rgba(255,255,255,.3); background: rgba(255,255,255,.1); color: #fff;
            -webkit-backdrop-filter: blur(6px); backdrop-filter: blur(6px);
            display: inline-flex; align-items: center; justify-content: center;
            transition: background-color .15s ease;
        }
        .gg-arrows button:hover { background: rgba(255,255,255,.25); }

        /* ================= STATS ================= */
      .gg-stats {
            position: relative; z-index: 5; margin: 24px 1.5rem 0;
            display: grid; grid-template-columns: repeat(3, 1fr);
            padding: .5rem 0; box-shadow: var(--gg-shadow-md);
        }
        .gg-stat { display: flex; align-items: center; gap: 1rem; padding: 1.1rem 1.6rem; }
        .gg-stat + .gg-stat { border-left: 1px solid var(--gg-border); }
        .gg-stat strong { display: block; font-family: var(--gg-display); font-size: 1.9rem; font-weight: 600; line-height: 1; color: var(--gg-text); }
        .gg-stat span { font-size: .85rem; color: var(--gg-muted); font-weight: 500; }

        /* ================= SECTIONS ================= */
        .gg-section { margin-top: 5rem; }
        .gg-section-head { max-width: 640px; margin-bottom: 2rem; }
        .gg-section-head h2 { font-family: var(--gg-display); font-weight: 600; font-size: clamp(1.7rem, 3vw, 2.3rem); letter-spacing: -.02em; margin: .4rem 0 .6rem; color: var(--gg-text) !important; }
        .gg-section-head p { color: var(--gg-muted); margin: 0; line-height: 1.65; }
        .gg-section-head.is-center { text-align: center; margin-left: auto; margin-right: auto; }

        /* Services */
        .gg-service { padding: 1.6rem; height: 100%; transition: border-color .2s ease; }
        .gg-service:hover { border-color: var(--gg-500); }
        .gg-service h3 { font-size: 1.05rem; font-weight: 700; margin: 1.1rem 0 .5rem; color: var(--gg-text); }
        .gg-service p { font-size: .9rem; color: var(--gg-muted); margin: 0; line-height: 1.6; }

        /* Trusted by */
        .gg-trusted { padding: 2.25rem; background: var(--gg-surface-2); border: 1px solid var(--gg-border); border-radius: 20px; }
        .gg-trusted-logos { background: #fff; border-radius: 14px; padding: .75rem; text-align: center; border: 1px solid var(--gg-border); }

        /* Company info */
        .gg-about { padding: 2.5rem; border-radius: 20px; }
        .gg-about h2 { font-family: var(--gg-display); font-weight: 600; letter-spacing: -.02em; font-size: clamp(1.6rem, 2.8vw, 2.1rem); color: var(--gg-text) !important; }
        .gg-about p { color: var(--gg-muted); line-height: 1.7; }
        .gg-standards {
            background: linear-gradient(135deg, var(--gg-800), var(--gg-900)); color: #fff;
            border-radius: 16px; padding: 1.75rem; height: 100%;
        }
        .gg-standards h3 { font-family: var(--gg-display); font-weight: 600; font-size: 1.25rem; color: #fff; margin-bottom: 1rem; }
        .gg-standards ul { list-style: none; padding: 0; margin: 0; }
        .gg-standards li { display: flex; gap: .75rem; align-items: flex-start; padding: .7rem 0; border-top: 1px solid rgba(255,255,255,.12); color: #d6eadf; font-size: .95rem; }
        .gg-standards li:first-child { border-top: 0; }
        .gg-standards li i { color: var(--gg-500); font-size: 1.1rem; margin-top: .05rem; }

        /* Testimonials */
        .gg-quote { padding: 1.75rem; height: 100%; display: flex; flex-direction: column; }
        .gg-stars { color: #d69e2e; font-size: .95rem; letter-spacing: 2px; margin-bottom: .9rem; }
        .gg-quote blockquote { margin: 0 0 1.4rem; font-size: .98rem; line-height: 1.65; color: var(--gg-text); flex-grow: 1; }
        .gg-quote-by { display: flex; align-items: center; gap: .8rem; }
        .gg-avatar {
            width: 42px; height: 42px; border-radius: 50%; flex-shrink: 0;
            display: inline-flex; align-items: center; justify-content: center;
            background: var(--gg-accent-soft); color: var(--gg-accent); font-weight: 700; font-size: .85rem;
        }
        .gg-quote-by strong { display: block; font-size: .92rem; color: var(--gg-text); }
        .gg-quote-by small { color: var(--gg-muted); }

        /* Hub tiles (logged-in) */
        .gg-hub { display: flex; flex-direction: column; padding: 1.75rem; height: 100%; position: relative; transition: border-color .2s ease; }
        .gg-hub:hover { border-color: var(--gg-500); }
        .gg-hub h3 { font-size: 1.1rem; font-weight: 700; margin: 1.1rem 0 .4rem; color: var(--gg-text); }
        .gg-hub p { color: var(--gg-muted); font-size: .92rem; line-height: 1.6; flex-grow: 1; }
        .gg-hub a { font-weight: 600; font-size: .92rem; color: var(--gg-accent) !important; text-decoration: none; }

        /* Guest CTA band */
        .gg-cta {
            position: relative; overflow: hidden; color: #fff; text-align: center;
            padding: 3.5rem 1.5rem; border-radius: 24px;
            background: linear-gradient(135deg, var(--gg-800), var(--gg-900));
            box-shadow: var(--gg-shadow-md);
        }
        .gg-cta::before {
            content: ""; position: absolute; inset: 0; pointer-events: none;
            background: repeating-radial-gradient(circle at 50% 120%, rgba(149,213,178,.12) 0 1.5px, transparent 1.5px 26px);
        }
        .gg-cta > * { position: relative; }
        .gg-cta h2 { font-family: var(--gg-display); font-weight: 600; letter-spacing: -.02em; color: #fff; font-size: clamp(1.6rem, 3vw, 2.2rem); }
        .gg-cta p { color: #cfe5d9; }

        /* Modal */
        .gg-modal .modal-content { border: 0; border-radius: 20px; overflow: hidden; background: var(--gg-surface); color: var(--gg-text); }
        .gg-modal .modal-header { background: linear-gradient(135deg, var(--gg-700), var(--gg-800)); color: #fff; border: 0; padding: 1.1rem 1.5rem; }
        .gg-modal .modal-title { font-family: var(--gg-display); font-weight: 600; }
        .gg-modal h4 { font-family: var(--gg-display); font-weight: 600; color: var(--gg-text) !important; }

        @media (max-width: 991.98px) {
            .gg-stats { grid-template-columns: 1fr; margin: 16px .75rem 0; }
            .gg-stat + .gg-stat { border-left: 0; border-top: 1px solid var(--gg-border); }
            .gg-section { margin-top: 3.5rem; }
        }
        @media (max-width: 768px) {
            .gg-hero { height: 480px; border-radius: 18px; }
            .gg-text-slide { padding: 2rem 1.5rem 4rem; }
            .gg-text-slide p.lead { font-size: 1rem; }
            .gg-caption { left: 1.25rem; bottom: 4rem; }
            .gg-about { padding: 1.5rem; }
        }
        @media (prefers-reduced-motion: reduce) {
            .gg-slide { transition: none; }
        }
    </style>

    <div class="container my-4">

        <!-- 1. HERO SLIDESHOW -->
        <div id="hero" class="gg-hero text-white" data-aos="zoom-in">

            <div class="gg-slide gg-text-slide active">
                <h1 class="text-white mb-3">Welcome to The Giant Group</h1>
                <p class="lead mb-4">
                    South Africa’s premier titan in professional tree felling, hazardous vegetation management, and commercial property maintenance.
                </p>
                <div class="gg-hero-actions">
                    <% if (IsClient) { %>
                    <a runat="server" href="~/Requests/SubmitRequest.aspx" class="gg-btn gg-btn-light gg-btn-arrow">Submit Job Request</a>
                    <% } else if (IsStaff) { %>
                    <a runat="server" href="~/Calendar.aspx" class="gg-btn gg-btn-ghost-light gg-btn-arrow">View Schedule</a>
                    <% } else { %>
                    <a runat="server" href="~/Contact.aspx" class="gg-btn gg-btn-light gg-btn-arrow">Get a free quote</a>
                    <a runat="server" href="~/Account/Login.aspx" class="gg-btn gg-btn-ghost-light">Log in</a>
                    <% } %>
                </div>
            </div>

            <div class="gg-slide gg-photo-slide" style="--photo:url('<%= ResolveUrl("~/Content/Slideshow/team-crew.png") %>')">
                <img runat="server" src="~/Content/Slideshow/team-crew.png" alt="The Giant Group crew on site" />
                <span class="gg-caption">Our certified felling crew</span>
            </div>
            <div class="gg-slide gg-photo-slide" style="--photo:url('<%= ResolveUrl("~/Content/Slideshow/tree-felling.png") %>')">
                <img runat="server" src="~/Content/Slideshow/tree-felling.png" alt="Hazardous tree dismantling" />
                <span class="gg-caption">Hazardous tree dismantling</span>
            </div>
            <div class="gg-slide gg-photo-slide" style="--photo:url('<%= ResolveUrl("~/Content/Slideshow/site-clearing.png") %>')">
                <img runat="server" src="~/Content/Slideshow/site-clearing.png" alt="Site clearing" />
                <span class="gg-caption">Site clearing and vegetation management</span>
            </div>

            <div class="gg-dots" id="heroDots"></div>
            <div class="gg-arrows">
                <button type="button" id="heroPrev" aria-label="Previous slide"><i class="bi bi-chevron-left"></i></button>
                <button type="button" id="heroNext" aria-label="Next slide"><i class="bi bi-chevron-right"></i></button>
            </div>
        </div>

        <!-- 2. PERFORMANCE METRICS -->
        <div class="gg-stats gg-surface">
            <div class="gg-stat">
                <span class="gg-tile"><i class="bi bi-calendar2-check"></i></span>
                <div><strong>100%</strong><span>Schedule &amp; Tracking Accuracy</span></div>
            </div>
            <div class="gg-stat">
                <span class="gg-tile"><i class="bi bi-broadcast"></i></span>
                <div><strong>24/7</strong><span>Emergency Field Response</span></div>
            </div>
            <div class="gg-stat">
                <span class="gg-tile"><i class="bi bi-receipt"></i></span>
                <div><strong>Fast</strong><span>Quote &amp; Invoicing Pipeline</span></div>
            </div>
        </div>

        <!-- 2.1 OUR SERVICES -->
        <section class="gg-section" id="services" data-aos="fade-up">
            <div class="gg-section-head">
                <span class="gg-label"><i class="bi bi-tree"></i>What we do</span>
                <h2>Our Services</h2>
                <p>Professional tree and property care for estates, municipalities and homeowners.</p>
            </div>

            <div class="row g-3 g-lg-4">
                <div class="col-md-6 col-lg-3">
                    <div class="gg-service gg-surface">
                        <span class="gg-tile is-lg"><i class="bi bi-tree-fill"></i></span>
                        <h3>Tree Felling &amp; Dismantling</h3>
                        <p>Safe removal of hazardous and unwanted trees, including sectional dismantling where space is tight.</p>
                    </div>
                </div>
                <div class="col-md-6 col-lg-3">
                    <div class="gg-service gg-surface">
                        <span class="gg-tile is-lg"><i class="bi bi-record-circle"></i></span>
                        <h3>Stump Removal</h3>
                        <p>Stumps removed so your land is clear and ready to use again.</p>
                    </div>
                </div>
                <div class="col-md-6 col-lg-3">
                    <div class="gg-service gg-surface">
                        <span class="gg-tile is-lg"><i class="bi bi-truck"></i></span>
                        <h3>Site Clearing</h3>
                        <p>Vegetation management and debris removal for commercial estates and development sites.</p>
                    </div>
                </div>
                <div class="col-md-6 col-lg-3">
                    <div class="gg-service gg-surface">
                        <span class="gg-tile is-lg"><i class="bi bi-tools"></i></span>
                        <h3>Garden &amp; Property Maintenance</h3>
                        <p>Regular upkeep for gardens, estates and municipal grounds, so they stay safe and tidy.</p>
                    </div>
                </div>
            </div>

            <div class="mt-4">
                <a runat="server" href="~/Contact.aspx" class="gg-btn gg-btn-primary gg-btn-arrow">Get in touch for a quote</a>
            </div>
        </section>

        <!-- 3. TRUSTED ORGANISATIONS / PARTNERS -->
        <section class="gg-section" data-aos="fade-up">
            <div class="gg-trusted">
                <div class="gg-section-head is-center mb-4">
                    <h2>Trusted By Leading Organisations</h2>
                    <p>Proudly servicing major industry leaders, municipalities, and construction partners across the region.</p>
                </div>
                <div class="gg-trusted-logos">
                    <img runat="server" src="~/Content/Companies Worked With.png" alt="Organisations and Partners" class="img-fluid" style="max-height: 280px; width: auto;" />
                </div>
            </div>
        </section>

        <!-- 3.1 COMPANY INFORMATION & TREE-FELLING EXPERTISE -->
        <section class="gg-section" data-aos="fade-up">
            <div class="gg-about gg-surface">
                <div class="row align-items-stretch g-4">
                    <div class="col-lg-6">
                        <h2 class="mb-3">Specialized Tree Felling &amp; Estate Maintenance</h2>
                        <p>
                            The Giant Group delivers heavy-duty tree-felling solutions, site clearing, and hazardous tree dismantling for commercial estates, municipal facilities, and private properties.
                        </p>
                        <p class="mb-4">
                            Our integrated digital tracking platforms ensure total accountability from initial site quotation to final debris removal and safety sign-off.
                        </p>
                        <% if (!IsLoggedIn) { %>
                        <div class="gg-alert" style="background: var(--gg-accent-soft); color: var(--gg-accent);">
                            <i class="bi bi-lock"></i>
                            <span>Sign in above to manage service requests, review active job schedules, or access administrative workflows.</span>
                        </div>
                        <% } %>
                    </div>
                    <div class="col-lg-6">
                        <div class="gg-standards">
                            <h3>Core Operational Standards</h3>
                            <ul>
                                <li><i class="bi bi-patch-check-fill"></i>Certified Hazard Risk Assessments</li>
                                <li><i class="bi bi-patch-check-fill"></i>Fully Insured Rigging &amp; Felling Crews</li>
                                <li><i class="bi bi-patch-check-fill"></i>Real-time Digital Job Tracking Portal</li>
                                <li><i class="bi bi-patch-check-fill"></i>Transparent Enterprise Quotation Pipeline</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </section>

       <!-- 3.2 CLIENT FEEDBACK & TESTIMONIALS -->
        <section class="gg-section" data-aos="fade-up">
            <div class="gg-section-head">
                <span class="gg-label"><i class="bi bi-chat-quote"></i>Client feedback</span>
                <h2>What Our Clients Say</h2>
                <p>Direct feedback from property directors and municipal supervisors.</p>
            </div>
            
            <!-- Elfsight All-in-One Reviews Widget -->
            <script src="https://elfsightcdn.com/platform.js" async></script>
            <div class="elfsight-app-15e0cc56-65b6-42a5-8cb2-acb7c0ffdddc" data-elfsight-app-lazy></div>
        </section>

        <!-- 4. CORE SYSTEM MODULES GRID (Staff Levels 4+ Only) -->
        <asp:Panel runat="server" ID="OperationsHubPanel" Visible="false">
            <section class="gg-section" data-aos="fade-up">
                <div class="gg-section-head">
                    <span class="gg-label"><i class="bi bi-grid-1x2"></i>Staff tools</span>
                    <h2>Management &amp; Operations Hub</h2>
                    <p>Select a module below to manage workflows, tracking, and reports.</p>
                </div>
                <div class="row g-3 g-lg-4">
                    <div class="col-md-4">
                        <div class="gg-hub gg-surface">
                            <span class="gg-tile is-lg"><i class="bi bi-calendar3"></i></span>
                            <h3>Job Scheduling</h3>
                            <p>Manage calendars, track team timelines, and handle allocations seamlessly.</p>
                            <a runat="server" href="~/Calendar.aspx" class="stretched-link">Open Calendar<i class="bi bi-arrow-right ms-2"></i></a>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="gg-hub gg-surface">
                            <span class="gg-tile is-lg"><i class="bi bi-people"></i></span>
                            <h3>Client Portals</h3>
                            <p>Add, update, and manage all organizational client information in one place.</p>
                            <a runat="server" href="~/Clients/ViewClients.aspx" class="stretched-link">Client Portal<i class="bi bi-arrow-right ms-2"></i></a>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="gg-hub gg-surface">
                            <span class="gg-tile is-lg"><i class="bi bi-cash-coin"></i></span>
                            <h3>Quotations &amp; Invoicing</h3>
                            <p>Generate quotes, track payment processing, and streamline financial records.</p>
                            <a runat="server" href="~/Quotes/CreateQuote.aspx" class="stretched-link">Quotations Portal<i class="bi bi-arrow-right ms-2"></i></a>
                        </div>
                    </div>
                </div>
            </section>
        </asp:Panel>

        <!-- 5. CLIENT QUICK ACTIONS (Logged-In Clients Only) -->
        <asp:Panel runat="server" ID="ClientHubPanel" Visible="false">
            <section class="gg-section" data-aos="fade-up">
                <div class="gg-section-head">
                    <span class="gg-label"><i class="bi bi-lightning-charge"></i>Quick actions</span>
                    <h2>Client Portal Quick Actions</h2>
                    <p>Manage your active service requests and company account details.</p>
                </div>
                <div class="row g-3 g-lg-4">
                    <div class="col-md-6">
                        <div class="gg-hub gg-surface">
                            <span class="gg-tile is-lg"><i class="bi bi-pencil-square"></i></span>
                            <h3>Submit New Request</h3>
                            <p>Request tree felling, site clearing, or hazard assessment for your property.</p>
                            <a runat="server" href="~/Requests/SubmitRequest.aspx" class="stretched-link">New Request<i class="bi bi-arrow-right ms-2"></i></a>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="gg-hub gg-surface">
                            <span class="gg-tile is-lg"><i class="bi bi-clock-history"></i></span>
                            <h3>View Request History</h3>
                            <p>Check current status updates, quotes, and job completion reports.</p>
                            <a runat="server" href="~/Requests/ViewRequests.aspx" class="stretched-link">View History<i class="bi bi-arrow-right ms-2"></i></a>
                        </div>
                    </div>
                </div>
            </section>
        </asp:Panel>

        <!-- 6. GUEST PROMPT PANEL (Visible only when logged out) -->
        <asp:Panel runat="server" ID="GuestPromptPanel" Visible="true">
            <section class="gg-section" data-aos="fade-up">
                <div class="gg-cta">
                    <i class="bi bi-tree-fill fs-1 d-block mb-3" style="color: var(--gg-500);"></i>
                    <h2 class="mb-2">Discover The Giant Group Experience</h2>
                    <p class="col-lg-7 mx-auto mb-4">
                        Log in to submit custom tree-felling job requests, track active property maintenance schedules, or access secure corporate operations portals.
                    </p>
                    <button type="button" class="gg-btn gg-btn-light px-4" data-bs-toggle="modal" data-bs-target="#authPromptModal">
                        Log In / Register Now
                    </button>
                </div>
            </section>
        </asp:Panel>
    </div>

    <!-- Reusable Authentication Prompt Modal -->
    <div class="modal fade gg-modal" id="authPromptModal" tabindex="-1" aria-labelledby="authPromptModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content shadow-lg">
                <div class="modal-header">
                    <h5 class="modal-title" id="authPromptModalLabel">Authentication Required</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body text-center p-4">
                    <span class="gg-tile is-lg mb-3"><i class="bi bi-lock"></i></span>
                    <h4 class="mb-2">Access The Giant Group Portal</h4>
                    <p class="gg-muted mb-4">Please log in to submit job requests, view quotes, or access staff operations.</p>
                    <div class="d-grid gap-2">
                        <a runat="server" href="~/Account/Login.aspx" class="gg-btn gg-btn-primary py-3">Log In to Your Account</a>
                        <a runat="server" href="~/Account/Register.aspx" class="gg-btn gg-btn-outline py-3">Create a New Client Account</a>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        (function () {
            var slides = document.querySelectorAll('#hero .gg-slide');
            var dotsBox = document.getElementById('heroDots');
            var SLIDE_MS = 5000;    // time on each slide
            var CHAT_MS = 7000;
            var NOTICE_MS = 7000;   // how long the guest notice stays up
            var current = 0, timer, noticeTimer, noticeShown = false, chatOpened = false;

            function stored(key) { try { return sessionStorage.getItem(key); } catch (e) { return null; } }
            function store(key, val) { try { sessionStorage.setItem(key, val); } catch (e) { } }

            // dots
            slides.forEach(function (_, i) {
                var b = document.createElement('button');
                b.type = 'button';
                b.setAttribute('aria-label', 'Show slide ' + (i + 1));
                b.addEventListener('click', function () { show(i); restart(); });
                dotsBox.appendChild(b);
            });
            var dots = dotsBox.querySelectorAll('button');
            dots[0].classList.add('active');

            // guest notice card (lives in Site.Master)
            var notice = document.getElementById('guestNoticeCard');
            function showNotice() {
                if (!notice || noticeShown || stored('guestCardDismissed')) return;
                noticeShown = true;
                notice.style.display = 'block';
                noticeTimer = setTimeout(function () { notice.style.display = 'none'; }, NOTICE_MS);
            }

            // Oak chat: open once per visit, on the last slide
            function openChatOnce() {
                if (!notice) return;   // guests only
                if (chatOpened || stored('oakAutoOpened')) return;
                chatOpened = true;
                store('oakAutoOpened', '1');
                notice.style.display = 'none';
                if (typeof openChatbot !== 'function') return;
                openChatbot();

                // Close it again after CHAT_MS, unless the visitor is using it
                var chatEl = document.getElementById('chatbotPopup');
                var engaged = false;
                if (chatEl) {
                    ['mouseenter', 'mousedown', 'touchstart', 'keydown', 'focusin'].forEach(function (ev) {
                        chatEl.addEventListener(ev, function () { engaged = true; });
                    });
                    setTimeout(function () {
                        if (!engaged) chatEl.style.display = 'none';
                    }, CHAT_MS);
                }
            }

            function show(i) {
                slides[current].classList.remove('active');
                dots[current].classList.remove('active');
                current = i;
                slides[current].classList.add('active');
                dots[current].classList.add('active');
                if (current === slides.length - 1) openChatOnce();
            }

            function next() {
                var wasFirst = (current === 0);
                show((current + 1) % slides.length);   // loops forever
                if (wasFirst) showNotice();            // notice on the first change
            }
            function prev() { show((current - 1 + slides.length) % slides.length); }
            function restart() { clearInterval(timer); timer = setInterval(next, SLIDE_MS); }
            restart();

            document.getElementById('heroNext').addEventListener('click', function () { next(); restart(); });
            document.getElementById('heroPrev').addEventListener('click', function () { prev(); restart(); });

            var hero = document.getElementById('hero');
            hero.addEventListener('mouseenter', function () { clearInterval(timer); });
            hero.addEventListener('mouseleave', restart);
        })();
    </script>
</asp:Content>
