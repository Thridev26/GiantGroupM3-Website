<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GiantGroupM3._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
     <style>
    .gg-hero {
        height: 520px;
        background: linear-gradient(135deg, #2d6a4f 0%, #1b4332 100%);
        border: 1px solid rgba(255,255,255,0.1);
    }
    .gg-slide {
        position: absolute; inset: 0;
        opacity: 0; visibility: hidden;
        transition: opacity 1s ease, visibility 1s ease;
    }
    .gg-slide.active { opacity: 1; visibility: visible; }

    .gg-text-slide {
        display: flex; flex-direction: column;
        align-items: center; justify-content: center;
        text-align: center; padding: 2rem 3rem;
    }
    .gg-text-slide h1 { font-size: 3rem; }

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
        background: linear-gradient(to top, rgba(0,0,0,0.65), transparent 55%);
    }
    .gg-caption {
        position: absolute; left: 2rem; bottom: 3rem; z-index: 2;
        color: #fff; font-size: 1.3rem; font-weight: 500;
    }

    .gg-dots {
        position: absolute; bottom: 1.2rem; left: 50%;
        transform: translateX(-50%);
        display: flex; gap: 8px; z-index: 3;
    }
    .gg-dots button {
        width: 10px; height: 10px; border: none; padding: 0;
        border-radius: 50%; cursor: pointer;
        background: rgba(255,255,255,0.45);
    }
    .gg-dots button.active { background: #fff; }

    @media (max-width: 768px) {
        .gg-hero { height: 420px; }
        .gg-text-slide h1 { font-size: 2rem; }
        .gg-text-slide p { font-size: 1rem; }
    }
    @media (prefers-reduced-motion: reduce) {
        .gg-slide { transition: none; }
    }
</style>
    <div class="container my-5">
        <!-- 1. HERO SLIDESHOW -->
<div id="hero" class="gg-hero mb-5 rounded-4 shadow text-white position-relative overflow-hidden" data-aos="zoom-in">

    <div class="gg-slide gg-text-slide active">
        <h1 class="text-white fw-bold mb-3">Welcome to The Giant Group</h1>
        <p class="lead col-lg-8 mx-auto mb-4" style="color:#eaf5ef;">
            South Africa’s premier titan in professional tree felling, hazardous vegetation management, and commercial property maintenance.
        </p>
        <% if (IsClient) { %>
            <a runat="server" href="~/Requests/SubmitRequest.aspx" class="btn btn-light text-success fw-bold btn-lg px-4 shadow-sm">Submit Job Request</a>
        <% } else if (IsStaff) { %>
            <a runat="server" href="~/Calendar.aspx" class="btn btn-outline-light btn-lg px-4">View Schedule</a>
        <% } %>
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
</div>
            <!-- Slides 2+: team and work photos (change names/captions to your own) -->
            <div class="gg-slide gg-photo-slide">
                <img runat="server" src="~/Content/Slideshow/team-crew.png" alt="The Giant Group crew on site" />
                <span class="gg-caption">Our certified felling crew</span>
            </div>
            <div class="gg-slide gg-photo-slide">
               <img runat="server" src="~/Content/Slideshow/tree-felling.png" alt="Hazardous tree dismantling" />
                <span class="gg-caption">Hazardous tree dismantling</span>
            </div>
            <div class="gg-slide gg-photo-slide">
               <img runat="server" src="~/Content/Slideshow/site-clearing.png" alt="Site clearing" />
                <span class="gg-caption">Site clearing and vegetation management</span>
            </div>

            <div class="gg-dots" id="heroDots"></div>
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

                   <script>
                       (function () {
                           var slides = document.querySelectorAll('#hero .gg-slide');
                           var dotsBox = document.getElementById('heroDots');
                           var SLIDE_MS = 5000;    // time on each slide
                           var NOTICE_MS = 7000;   // how long the guest notice stays up
                           var current = 0, timer, noticeTimer, noticeShown = false, chatOpened = false;

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
                               if (!notice || noticeShown || sessionStorage.getItem('guestCardDismissed')) return;
                               noticeShown = true;
                               notice.style.display = 'block';
                               noticeTimer = setTimeout(function () { notice.style.display = 'none'; }, NOTICE_MS);
                           }

                           // Oak chat: open once per visit, on the last slide
                           function openChatOnce() {
                               if (!notice) return;   // only guests (the notice card only exists for logged-out visitors)
                               if (chatOpened || sessionStorage.getItem('oakAutoOpened')) return;
                               chatOpened = true;
                               sessionStorage.setItem('oakAutoOpened', '1');
                               notice.style.display = 'none';   // tidy the guest card out of the way
                               if (typeof openChatbot === 'function') openChatbot();
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
                           function restart() { clearInterval(timer); timer = setInterval(next, SLIDE_MS); }
                           restart();

                           var hero = document.getElementById('hero');
                           hero.addEventListener('mouseenter', function () { clearInterval(timer); });
                           hero.addEventListener('mouseleave', restart);
                       })();
</script>
</asp:Content>