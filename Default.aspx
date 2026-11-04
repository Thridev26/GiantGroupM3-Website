<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GiantGroupM3._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        /* ================= HERO SLIDESHOW ================= */
        #hero {
            --interval: 5000ms; --fade: 1300ms;
            --sl-h: clamp(560px, calc(100vh - 120px), 720px);
            --sl-green: var(--giant-green-accent, #2d6a4f);
            --sl-lime: var(--giant-emerald, #52b788);
            --sl-forest: var(--giant-green-dark, #0f2d1e);
            position: relative; height: var(--sl-h); overflow: hidden;
            border-radius: 24px; background: var(--sl-forest);
            box-shadow: 0 18px 44px rgba(15, 45, 30, .28);
        }
        #hero .slide {
            position: absolute; inset: 0; display: flex; align-items: center;
            padding: clamp(20px, 4vw, 56px) clamp(20px, 4vw, 56px) 76px;
            opacity: 0; filter: blur(8px); transform: scale(1.04); pointer-events: none;
            transition: opacity var(--fade) ease, filter var(--fade) ease, transform var(--fade) ease;
        }
        #hero .slide.active { opacity: 1; filter: none; transform: none; z-index: 2; pointer-events: auto; }
        #hero .dark { background: var(--sl-forest); color: #fff; }
 
        /* slide 1: title + cover photo */
        #hero .grid { width: 100%; max-width: 1280px; margin: 0 auto; display: grid; grid-template-columns: 1fr 1fr; gap: clamp(20px, 4vw, 56px); align-items: center; }
        #hero .eyebrow { font-family: var(--gg-display, inherit); font-weight: 700; font-size: clamp(.72rem, .9vw, .85rem); letter-spacing: .12em; text-transform: uppercase; color: var(--sl-lime); margin: 0 0 12px; }
        #hero h1 { margin: 0 0 14px; font-family: var(--gg-display, inherit); font-weight: 700; line-height: 1.06; letter-spacing: -.02em; color: inherit; font-size: clamp(2.2rem, 5.2vw, 4rem); text-transform: uppercase; }
        #hero .sl-lead { font-size: clamp(.95rem, 1.35vw, 1.2rem); line-height: 1.55; color: #b7d3c3; margin: 0 0 22px; max-width: 36ch; font-weight: 400; }
 
        /* photos */
        #hero .ph { position: relative; border-radius: 20px; overflow: hidden; background: #1b4332; box-shadow: 0 0 0 2px rgba(82, 183, 136, .35), 0 24px 50px rgba(0, 0, 0, .5); aspect-ratio: 3 / 2; }
        #hero .ph.tall { aspect-ratio: 3 / 4; }
        #hero .ph.sq { aspect-ratio: 1; max-width: 420px; justify-self: end; width: 100%; }
        #hero .ph img { width: 100%; height: 100%; object-fit: cover; display: block; transform: scale(1.02); }
        #hero .slide.active .ph img { animation: slKb calc(var(--interval) + var(--fade) + 500ms) ease-out forwards; }
        #hero .slide.active .ph.b img { --kx: 1.5%; }
        @keyframes slKb { to { transform: scale(1.14) translate(var(--kx, -1.5%), -1%); } }
        #hero .ph.missing img { display: none; }
        #hero .ph.missing::after { content: attr(data-label); position: absolute; inset: 0; display: grid; place-items: center; color: #8a8a8a; font-weight: 700; font-size: 1.1rem; }
 
        /* photo slides: caption on top, photos below */
        #hero .slide.pics { flex-direction: column; justify-content: center; align-items: center; }
        #hero .sl-cap { text-align: center; max-width: 640px; margin: 0 auto 18px; }
        #hero .sl-cap b { display: block; font-family: var(--gg-display, inherit); font-weight: 700; font-size: clamp(1.15rem, 2vw, 1.6rem); letter-spacing: -.01em; }
        #hero .sl-cap span { display: block; margin-top: 4px; font-size: clamp(.85rem, 1.1vw, 1rem); color: #b7d3c3; }
        #hero .pair { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; width: 100%; max-width: calc((var(--sl-h) - 200px) * 1.5 + 14px); }
        #hero .ph.land { width: 100%; max-width: calc((var(--sl-h) - 200px) * 1.5); }
 
        /* last slide: logo + actions */
        #hero .center { width: 100%; text-align: center; max-width: 720px; margin: 0 auto; }
        #hero .sl-logo { display: inline-flex; align-items: center; justify-content: center; background: #fff; border-radius: 20px; padding: 16px 32px; margin: 0 auto 28px; box-shadow: 0 10px 30px rgba(0, 0, 0, .35); }
        #hero .sl-logo img { display: block; height: 110px; width: auto; max-width: 100%; }
        #hero .sl-actions { display: flex; flex-wrap: wrap; gap: .75rem; justify-content: center; }
        #hero .sl-btn { display: inline-flex; align-items: center; padding: .8rem 1.6rem; border-radius: 999px; font-weight: 700; font-size: 1rem; text-decoration: none; border: 1.5px solid transparent; transition: background-color .15s ease, transform .15s ease; }
        #hero .sl-btn:hover { transform: translateY(-1px); }
        #hero .sl-btn-light { background: #fff; color: #1b4332; }
        #hero .sl-btn-light:hover { background: #e9f5f0; color: #1b4332; }
        #hero .sl-btn-ghost { background: transparent; color: #fff; border-color: rgba(255, 255, 255, .55); }
        #hero .sl-btn-ghost:hover { background: rgba(255, 255, 255, .12); color: #fff; }
 
        /* entrance choreography */
        #hero [data-in] { opacity: 0; transform: translateY(18px); }
        #hero .slide.active [data-in] { animation: slUp 800ms cubic-bezier(.2, .7, .2, 1) calc(350ms + var(--d, 0) * 130ms) forwards; }
        @keyframes slUp { to { opacity: 1; transform: none; } }
 
        /* controls (bottom centre, clear of the floating chat buttons) */
        #hero .ctrl { position: absolute; left: 50%; transform: translateX(-50%); bottom: 16px; z-index: 6; display: flex; gap: 6px; padding: 5px; border-radius: 999px; background: rgba(10, 31, 20, .78); }
        #hero .ctrl button { width: 36px; height: 36px; border-radius: 50%; border: 0; background: transparent; color: #fff; font-size: 15px; cursor: pointer; }
        #hero .ctrl button:hover { background: rgba(255, 255, 255, .18); }
        #hero .ctrl button:focus-visible { outline: 2px solid var(--sl-lime); }
        #hero .bar { position: absolute; left: 0; bottom: 0; width: 100%; height: 4px; z-index: 6; background: rgba(255, 255, 255, .12); }
        #hero .bar i { display: block; width: 0; height: 100%; background: var(--sl-lime); }
        #hero .bar i.run { animation: slFill var(--interval) linear forwards; }
        #hero.paused .bar i { animation-play-state: paused; }
        @keyframes slFill { to { width: 100%; } }
 
        @media (max-width: 820px) {
            #hero { --sl-h: 640px; border-radius: 18px; }
            #hero .slide { align-items: flex-start; overflow-y: auto; padding-bottom: 84px; }
            #hero .slide.final, #hero .slide.pics { align-items: center; }
            #hero .grid { grid-template-columns: 1fr; }
            #hero .ph.sq { justify-self: center; }
            #hero .sl-logo img { height: 84px; }
        }
        @media (prefers-reduced-motion: reduce) {
            #hero .slide { filter: none; transform: none; transition: none; }
            #hero .slide.active .ph img { animation: none; }
            #hero [data-in], #hero .slide.active [data-in] { opacity: 1; transform: none; animation: none; }
        }
 
        

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
            
            
            .gg-about { padding: 1.5rem; }
        }
        
    </style>

    <div class="container my-4">
        <!-- 1. HERO SLIDESHOW -->
        <div id="hero" class="mb-5" data-aos="zoom-in" aria-roledescription="carousel" aria-label="The Giant Group presentation">
 
            <!-- 1: title + cover photo -->
            <div class="slide dark"><div class="grid">
                <div class="copy">
                    <p class="eyebrow" data-in="true" style="--d:0">Professional Arborist &amp; Site Services</p>
                    <h1 data-in="true " style="--d:1">The Giant Group</h1>
                    <p class="sl-lead" data-in="true" style="--d:2">Precision tree care, high-risk felling, stump removal &amp; hazard management</p>
                </div>
                <div class="ph sq" data-photo="1" data-alt="Giant Treefelling branded trailer and work vehicles" data-in="true" style="--d:1"></div>
            </div></div>
 
            <!-- 2: photos with caption -->
            <div class="slide dark pics">
                <div class="sl-cap" data-in="true" style="--d:0"><b>High-Risk Aerial Felling</b><span>Expert climbing and rigging for tall trees and tight spaces.</span></div>
                <div class="pair" data-in="true" style="--d:1"><div class="ph tall" data-photo="2" data-alt="Climber on a tall tree trunk holding up his helmet, chainsaw at his side"></div><div class="ph tall b" data-photo="3" data-alt="Ladder leaning against a tall tree being taken down in sections"></div></div>
            </div>
 
            <!-- 3: photos with caption -->
            <div class="slide dark pics">
                <div class="sl-cap" data-in ="true" style="--d:0"><b>Complete Stump &amp; Root Removal</b><span>Clean ground, ready for building or replanting.</span></div>
                <div class="pair" data-in="true" style="--d:1"><div class="ph tall" data-photo="4" data-alt="Crew digging out the roots of a large stump beside a building"></div><div class="ph tall b" data-photo="5" data-alt="A freshly cut tree stump surrounded by sawdust"></div></div>
            </div>
 
            <!-- 4: photo with caption -->
            <div class="slide dark pics">
                <div class="sl-cap" data-in="true" style="--d:0"><b>The Team Behind Every Job</b><span>Experienced crews, ready for emergency call-outs.</span></div>
                <div class="ph land" data-photo="6" data-alt="Two Giant Group team members smiling as they carry a log" data-in="true" style="--d:1"></div>
            </div>
 
            <!-- 5 (last): logo and calls to action only -->
            <div class="slide dark final"><div class="center">
                <div class="sl-logo" data-in="true" style="--d:0">
                    <img runat="server" src="~/Content/The Giant Group Logo .png" alt="The Giant Group" />
                </div>
                <div class="sl-actions" data-in="true" style="--d:1">
                    <% if (IsClient) { %>
                    <a runat="server" href="~/Requests/SubmitRequest.aspx" class="sl-btn sl-btn-light">Submit Job Request</a>
                    <% } else if (IsStaff) { %>
                    <a runat="server" href="~/Calendar.aspx" class="sl-btn sl-btn-ghost">View Schedule</a>
                    <% } else { %>
                    <a runat="server" href="~/Contact.aspx" class="sl-btn sl-btn-light">Get a free quote &rarr;</a>
                    <a runat="server" href="~/Account/Register.aspx" class="sl-btn sl-btn-ghost">Register</a>
                    <a runat="server" href="~/Account/Login.aspx" class="sl-btn sl-btn-ghost">Log in</a>
                    <% } %>
                </div>
            </div></div>
 
            <div class="bar"><i id="heroBar"></i></div>
            <div class="ctrl">
                <button type="button" id="heroPrev" aria-label="Previous slide">&#8592;</button>
                <button type="button" id="heroPlay" aria-label="Pause slideshow">&#10074;&#10074;</button>
                <button type="button" id="heroNext" aria-label="Next slide">&#8594;</button>
            </div>
        </div>
        
<!-- ================= GALLERY SECTION ================= -->
<section class="container my-5">

    <div class="text-center mb-4">
        <span class="gg-label">
            <i class="bi bi-images"></i>
            OUR WORK
        </span>

        <h2 class="gg-display mt-2">
            See the Transformation
        </h2>

        <p class="gg-muted mx-auto"
           style="max-width: 600px;">
            Discover the difference professional tree
            felling and vegetation management can make.
            Explore real before-and-after transformations
            from our completed projects.
        </p>
    </div>

    <div class="row g-4 align-items-center">

        <div class="col-lg-6">
            <div class="gg-surface overflow-hidden">
                <img
                    src='<%= ResolveUrl("~/Content/The Giant Group Logo .png") %>'
                    alt="The Giant Group"
                    class="img-fluid w-100"
                    style="height: 300px; object-fit: contain;
                           padding: 30px;" />
            </div>
        </div>

        <div class="col-lg-6">
            <div class="gg-surface p-4 p-lg-5">

                <span class="gg-label">
                    <i class="bi bi-stars"></i>
                    QUALITY RESULTS
                </span>

                <h3 class="gg-display mt-3">
                    From Before to After
                </h3>

                <p class="gg-muted mt-3">
                    Every project tells a story of
                    transformation. See how our team
                    improves outdoor spaces through
                    professional, reliable work.
                </p>

                <a runat="server"
                   href="~/Gallery/Gallery.aspx"
                   class="gg-btn gg-btn-primary mt-3">
                    <i class="bi bi-camera"></i>
                    Explore Our Gallery
                </a>

            </div>
        </div>

    </div>

</section>
<!-- ================= END GALLERY SECTION ================= -->


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
    <style>
        /* Widget Container Header */
        .elf-widget-container {
            background: #f8faf9;
            border-radius: 16px;
            padding: 2rem 1.5rem;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
            color: #222;
        }
        .elf-tab-bar {
            display: flex;
            align-items: center;
            gap: 1.5rem;
            border-bottom: 1px solid #e2e8e4;
            padding-bottom: 0.75rem;
            margin-bottom: 1.5rem;
        }
        .elf-tab {
            font-weight: 600;
            font-size: 0.95rem;
            color: #555;
            text-decoration: none;
            display: flex;
            align-items: center;
            gap: 0.4rem;
            padding-bottom: 0.75rem;
            margin-bottom: -0.75rem;
        }
        .elf-tab.active {
            color: #111;
            border-bottom: 2px solid #111;
        }

        /* Overall Rating Header Bar */
        .elf-rating-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 1.75rem;
        }
        .elf-rating-title {
            font-size: 1.1rem;
            font-weight: 700;
            margin: 0 0 0.25rem 0;
            color: #111;
        }
        .elf-rating-score {
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }
        .elf-rating-score strong {
            font-size: 1.4rem;
            font-weight: 700;
        }
        .elf-stars-gold {
            color: #ffb400;
            font-size: 1.1rem;
            letter-spacing: 1px;
        }
        .elf-count {
            color: #777;
            font-size: 0.9rem;
        }
        .elf-btn-write {
            background-color: #0066ff;
            color: #fff !important;
            font-weight: 600;
            font-size: 0.9rem;
            padding: 0.55rem 1.25rem;
            border-radius: 99px;
            text-decoration: none;
            transition: background 0.2s ease;
        }
        .elf-btn-write:hover {
            background-color: #0052cc;
        }

        /* Review Cards Grid & Horizontal Scroll */
        .elf-cards-wrapper {
            position: relative;
        }
        .elf-cards-row {
            display: flex;
            gap: 1.25rem;
            overflow-x: auto;
            scroll-behavior: smooth;
            padding-bottom: 1rem;
            -ms-overflow-style: none;
            scrollbar-width: none;
        }
        .elf-cards-row::-webkit-scrollbar { display: none; }

        .elf-card {
            background: #ffffff;
            border-radius: 12px;
            padding: 1.25rem;
            min-width: 280px;
            max-width: 320px;
            flex: 0 0 auto;
            box-shadow: 0 2px 8px rgba(0,0,0,0.04);
            display: flex;
            flex-direction: column;
            justify-content: space-between;
        }
        .elf-user-info {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            margin-bottom: 0.75rem;
        }
        .elf-avatar-wrapper {
            position: relative;
            width: 42px;
            height: 42px;
            flex-shrink: 0;
        }
        .elf-avatar-circle {
            width: 100%;
            height: 100%;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            color: #fff;
            font-weight: 600;
            font-size: 1rem;
            text-transform: lowercase;
        }
        .elf-google-badge {
            position: absolute;
            bottom: -2px;
            right: -2px;
            width: 18px;
            height: 18px;
            background: #fff;
            border-radius: 50%;
            box-shadow: 0 1px 3px rgba(0,0,0,0.2);
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .elf-author-name {
            font-weight: 700;
            font-size: 0.95rem;
            color: #111;
            display: flex;
            align-items: center;
            gap: 0.3rem;
            line-height: 1.2;
        }
        .elf-verified-check {
            color: #0066ff;
            font-size: 0.85rem;
        }
        .elf-time {
            font-size: 0.8rem;
            color: #888;
        }
        .elf-card-stars {
            color: #ffb400;
            font-size: 0.95rem;
            margin-bottom: 0.6rem;
        }
        .elf-text {
            font-size: 0.9rem;
            color: #333;
            line-height: 1.45;
            margin: 0;
            word-break: break-word;
        }
        .elf-read-more {
            color: #0066ff;
            font-weight: 600;
            cursor: pointer;
            font-size: 0.85rem;
            display: inline-block;
            margin-top: 0.4rem;
            text-decoration: none;
        }

        /* Navigation Arrow Button */
        .elf-nav-arrow {
            position: absolute;
            right: -15px;
            top: 45%;
            transform: translateY(-50%);
            width: 40px;
            height: 40px;
            border-radius: 50%;
            background: #fff;
            box-shadow: 0 3px 10px rgba(0,0,0,0.15);
            border: none;
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            z-index: 5;
            color: #333;
            transition: transform 0.2s ease;
        }
        .elf-nav-arrow:hover {
            transform: translateY(-50%) scale(1.05);
        }
    </style>

    <div class="elf-widget-container">
        <!-- Tab Bar -->
        <div class="elf-tab-bar">
            <span class="elf-tab active">All Reviews <strong>3.9</strong></span>
            <span class="elf-tab">
                <svg width="16" height="16" viewBox="0 0 24 24"><path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.665-5.17 3.665-9.17z"/><path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.29v3.15C3.26 21.3 7.31 24 12 24z"/><path fill="#FBBC05" d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.29C.47 8.21 0 10.05 0 12s.47 3.79 1.29 5.42l3.99-3.15z"/><path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.31 0 3.26 2.7 1.29 6.58l3.99 3.15c.95-2.83 3.6-4.98 6.72-4.98z"/></svg>
                Google <strong>3.9</strong>
            </span>
        </div>

        <!-- Overall Rating Header Bar -->
        <div class="elf-rating-header">
            <div>
                <h3 class="elf-rating-title">Overall Rating</h3>
                <div class="elf-rating-score">
                    <strong>3.9</strong>
                    <div class="elf-stars-gold">
                        <i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star"></i>
                    </div>
                    <span class="elf-count">(23)</span>
                </div>
            </div>
            <a href="https://maps.google.com" target="_blank" class="elf-btn-write">Write a Review</a>
        </div>

        <!-- Review Cards Grid -->
        <div class="elf-cards-wrapper">
            <div class="elf-cards-row" id="elfCardsRow">
                
                <!-- Review 1: margaret serra -->
                <div class="elf-card">
                    <div>
                        <div class="elf-user-info">
                            <div class="elf-avatar-wrapper">
                                <div class="elf-avatar-circle" style="background-color: #e05638;">m</div>
                                <div class="elf-google-badge">
                                    <svg width="11" height="11" viewBox="0 0 24 24"><path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.665-5.17 3.665-9.17z"/><path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.29v3.15C3.26 21.3 7.31 24 12 24z"/><path fill="#FBBC05" d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.29C.47 8.21 0 10.05 0 12s.47 3.79 1.29 5.42l3.99-3.15z"/><path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.31 0 3.26 2.7 1.29 6.58l3.99 3.15c.95-2.83 3.6-4.98 6.72-4.98z"/></svg>
                                </div>
                            </div>
                            <div>
                                <div class="elf-author-name">
                                    margaret serra
                                    <i class="bi bi-patch-check-fill elf-verified-check"></i>
                                </div>
                                <div class="elf-time">1 year ago</div>
                            </div>
                        </div>
                        <div class="elf-card-stars">
                            <i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i>
                        </div>
                        <p class="elf-text">
                            Giant Treefelling has helped me so much. I appreciate everything they have done for me. A special thanks to Preggie for his.
                        </p>
                    </div>
                </div>

                <!-- Review 2: joel veeran -->
                <div class="elf-card">
                    <div>
                        <div class="elf-user-info">
                            <div class="elf-avatar-wrapper">
                                <div class="elf-avatar-circle" style="background-color: #14a0a6;">j</div>
                                <div class="elf-google-badge">
                                    <svg width="11" height="11" viewBox="0 0 24 24"><path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.665-5.17 3.665-9.17z"/><path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.29v3.15C3.26 21.3 7.31 24 12 24z"/><path fill="#FBBC05" d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.29C.47 8.21 0 10.05 0 12s.47 3.79 1.29 5.42l3.99-3.15z"/><path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.31 0 3.26 2.7 1.29 6.58l3.99 3.15c.95-2.83 3.6-4.98 6.72-4.98z"/></svg>
                                </div>
                            </div>
                            <div>
                                <div class="elf-author-name">
                                    joel veeran
                                    <i class="bi bi-patch-check-fill elf-verified-check"></i>
                                </div>
                                <div class="elf-time">2 years ago</div>
                            </div>
                        </div>
                        <div class="elf-card-stars">
                            <i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i>
                        </div>
                        <p class="elf-text">
                            Excellent Service and Pricing. Thank you!
                        </p>
                    </div>
                </div>

                <!-- Review 3: Ethan Dhavaraj -->
                <div class="elf-card">
                    <div>
                        <div class="elf-user-info">
                            <div class="elf-avatar-wrapper">
                                <div class="elf-avatar-circle" style="background-color: #e94e77;">e</div>
                                <div class="elf-google-badge">
                                    <svg width="11" height="11" viewBox="0 0 24 24"><path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.665-5.17 3.665-9.17z"/><path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.29v3.15C3.26 21.3 7.31 24 12 24z"/><path fill="#FBBC05" d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.29C.47 8.21 0 10.05 0 12s.47 3.79 1.29 5.42l3.99-3.15z"/><path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.31 0 3.26 2.7 1.29 6.58l3.99 3.15c.95-2.83 3.6-4.98 6.72-4.98z"/></svg>
                                </div>
                            </div>
                            <div>
                                <div class="elf-author-name">
                                    Ethan Dhavaraj
                                    <i class="bi bi-patch-check-fill elf-verified-check"></i>
                                </div>
                                <div class="elf-time">2 years ago</div>
                            </div>
                        </div>
                        <div class="elf-card-stars">
                            <i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i>
                        </div>
                        <p class="elf-text">
                            <span class="elf-text-short">Excellent Service from the Giant Treefelling team. The office staff were extremely helpful when communicating...</span>
                            <span class="elf-text-full d-none">Excellent Service from the Giant Treefelling team. The office staff were extremely helpful when communicating and arranging the quote and getting the staff on site. The ground staff worked professionally and made sure everything was done perfectly. Will definitely be calling them back for all my outdoor needs!</span>
                            <a href="javascript:void(0)" class="elf-read-more" onclick="toggleReadMore(this)">Read more</a>
                        </p>
                    </div>
                </div>

                <!-- Review 4: Aran Jeewanlall -->
                <div class="elf-card">
                    <div>
                        <div class="elf-user-info">
                            <div class="elf-avatar-wrapper">
                                <div class="elf-avatar-circle" style="background-color: #589e35;">a</div>
                                <div class="elf-google-badge">
                                    <svg width="11" height="11" viewBox="0 0 24 24"><path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.665-5.17 3.665-9.17z"/><path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.29v3.15C3.26 21.3 7.31 24 12 24z"/><path fill="#FBBC05" d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.29C.47 8.21 0 10.05 0 12s.47 3.79 1.29 5.42l3.99-3.15z"/><path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.31 0 3.26 2.7 1.29 6.58l3.99 3.15c.95-2.83 3.6-4.98 6.72-4.98z"/></svg>
                                </div>
                            </div>
                            <div>
                                <div class="elf-author-name">
                                    Aran Jeewanlall
                                    <i class="bi bi-patch-check-fill elf-verified-check"></i>
                                </div>
                                <div class="elf-time">2 years ago</div>
                            </div>
                        </div>
                        <div class="elf-card-stars">
                            <i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i>
                        </div>
                        <p class="elf-text">
                            <span class="elf-text-short">Excellent Service , Great people !!! Company that is efficient and reliable. Definitely my number 1 tree felling company</span>
                            <span class="elf-text-full d-none">Excellent Service , Great people !!! Company that is efficient and reliable. Definitely my number 1 tree felling company</span>
                            <a href="javascript:void(0)" class="elf-read-more" onclick="toggleReadMore(this)">Read more</a>
                        </p>
                    </div>
                </div>

            </div>
            <!-- Scroll Arrow Button -->
            <button class="elf-nav-arrow" type="button" onclick="scrollElfRight()">
                <i class="bi bi-chevron-right"></i>
            </button>
        </div>
    </div>

    <!-- Small Script for Expandable Text and Carousel Scrolling -->
    <script>
        function scrollElfRight() {
            var row = document.getElementById('elfCardsRow');
            if (row) row.scrollBy({ left: 300, behavior: 'smooth' });
        }
        function toggleReadMore(btn) {
            var parent = btn.parentElement;
            var shortTxt = parent.querySelector('.elf-text-short');
            var fullTxt = parent.querySelector('.elf-text-full');
            if (shortTxt && fullTxt) {
                shortTxt.classList.add('d-none');
                fullTxt.classList.remove('d-none');
                btn.style.display = 'none';
            }
        }
    </script>
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
         var hero = document.getElementById('hero');
         if (!hero) return;

         /* photos for data-photo 1..6 */
         var PHOTOS = [
                '<%= ResolveUrl("~/Content/Slideshow/slide-photo-1.jpg") %>',
                '<%= ResolveUrl("~/Content/Slideshow/slide-photo-2.jpg") %>',
                '<%= ResolveUrl("~/Content/Slideshow/slide-photo-3.jpg") %>',
                '<%= ResolveUrl("~/Content/Slideshow/slide-photo-4.jpg") %>',
                '<%= ResolveUrl("~/Content/Slideshow/slide-photo-5.jpg") %>',
                '<%= ResolveUrl("~/Content/Slideshow/slide-photo-6.jpg") %>'
         ];
         var INTERVAL = 5000;    // ms per slide (keep in sync with --interval in the CSS)
         var NOTICE_MS = 5500;   // how long the guest notice stays up
         var CHAT_MS = 7000;     // how long Oak stays open on its own

         hero.querySelectorAll('[data-photo]').forEach(function (box) {
             var n = +box.dataset.photo, img = new Image();
             img.alt = box.dataset.alt || ''; img.src = PHOTOS[n - 1];
             box.dataset.label = 'Photo ' + n;
             img.onerror = function () { box.classList.add('missing'); };
             box.appendChild(img);
         });

         var bar = document.getElementById('heroBar');
         var playBtn = document.getElementById('heroPlay');
         var slides = Array.prototype.slice.call(hero.querySelectorAll('.slide'));
         var index = -1, timer = null, playing = true;

         /* guest notice card (lives in Site.Master; only exists for guests) */
         var notice = document.getElementById('guestNoticeCard');
         var noticeShown = false;
         function showNotice() {
             if (!notice || noticeShown || sessionStorage.getItem('guestCardDismissed')) return;
             noticeShown = true;
             notice.style.display = 'block';
             setTimeout(function () { notice.style.display = 'none'; }, NOTICE_MS);
         }

         /* Oak: opens once per session on the last slide, closes itself unless the visitor uses it */
         var chatOpened = false;
         function openChatOnce() {
             if (!notice) return;   // guests only
             if (chatOpened || sessionStorage.getItem('oakAutoOpened')) return;
             chatOpened = true;
             sessionStorage.setItem('oakAutoOpened', '1');
             notice.style.display = 'none';
             if (typeof openChatbot !== 'function') return;
             openChatbot();
             var chatEl = document.getElementById('chatbotPopup');
             var engaged = false;
             if (chatEl) {
                 ['mouseenter', 'mousedown', 'touchstart', 'keydown', 'focusin'].forEach(function (ev) {
                     chatEl.addEventListener(ev, function() { engaged = true; });
                 });
                 setTimeout(function() {
                     // Only close Oak automatically if the visitor hasn't interacted
                     if (!engaged) {
                         chatEl.style.display = 'none';
                     }

                     // Show Contact Us tooltip regardless of interaction
                     var contactTooltip = document.getElementById('contactTooltip');

                     if (contactTooltip) {
                         setTimeout(function() {
                             contactTooltip.classList.add('show');

                             // Hide tooltip after 5 seconds
                             setTimeout(function() {
                                 contactTooltip.classList.remove('show');
                             }, 5000);

                         }, 1000);
                     }

                 }, CHAT_MS);
             }
         }

         function go(n) {
             var prev = index;
             if (slides[index]) slides[index].classList.remove('active');
             index = (n + slides.length) % slides.length;
             var cur = slides[index];
             void cur.offsetWidth; cur.classList.add('active'); cur.scrollTop = 0;

             var last = (index === slides.length - 1);   // stop on the last slide so the buttons stay put
             bar.classList.remove('run'); void bar.offsetWidth;
             if (playing && !last) bar.classList.add('run');
             clearTimeout(timer);
             if (playing && !last) timer = setTimeout(function () { go(index + 1); }, INTERVAL);

             if (prev === 0 && index === 1) showNotice();
             if (last) openChatOnce();
         }
         function setPlaying(p) {
             playing = p; hero.classList.toggle('paused', !p);
             playBtn.innerHTML = p ? '&#10074;&#10074;' : '&#9654;';
             playBtn.setAttribute('aria-label', p ? 'Pause slideshow' : 'Play slideshow');
             go(index);
         }
         document.getElementById('heroNext').onclick = function () { go(index + 1); };
         document.getElementById('heroPrev').onclick = function () { go(index - 1); };
         playBtn.onclick = function () { setPlaying(!playing); };
         document.addEventListener('keydown', function (e) {
             var t = e.target;   // don't steal the arrow keys while typing (chat box, search, forms)
             if (t && (/^(INPUT|TEXTAREA|SELECT)$/.test(t.tagName) || t.isContentEditable)) return;
             if (e.key === 'ArrowRight') go(index + 1);
             if (e.key === 'ArrowLeft') go(index - 1);
         });
         go(0);
     })();
 </script>
</asp:Content>
