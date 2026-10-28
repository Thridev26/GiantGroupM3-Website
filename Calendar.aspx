<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Calendar.aspx.cs" Inherits="GiantGroupM3.CalendarPage" MasterPageFile="~/Site.master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManagerProxy ID="ScriptManagerProxy1" runat="server" />

    <!-- FullCalendar (same version the site already uses) -->
    <link href="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.10/index.global.min.css" rel="stylesheet" />
    <script src="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.10/index.global.min.js"></script>

    <style>
        .cal-page { font-family: var(--gg-sans); color: var(--gg-text); }
        .cal-page h1, .cal-page h2, .cal-page h3 { font-family: var(--gg-display); letter-spacing: -.01em; }
        .cal-wrap { max-width: 1400px; margin: 0 auto; padding: 1.5rem 1.25rem 3rem; }
        .cal-wrap > * { animation: cal-rise .55s cubic-bezier(.2,.7,.2,1) both; }
        .cal-wrap > :nth-child(2) { animation-delay: .06s; }
        .cal-wrap > :nth-child(3) { animation-delay: .12s; }
        .cal-wrap > :nth-child(4) { animation-delay: .18s; }
        @keyframes cal-rise { from { opacity: 0; transform: translateY(14px); } to { opacity: 1; transform: none; } }

        /* ---------- hero ---------- */
        .cal-hero {
            position: relative; overflow: hidden; border-radius: 28px; padding: 2.1rem 2.3rem; color: #fff;
            background:
                radial-gradient(900px 340px at 92% -30%, rgba(255,255,255,.22), transparent 60%),
                linear-gradient(135deg, #2d6a4f 0%, #1d4a37 58%, #112e22 100%);
            box-shadow: var(--gg-shadow-md);
            display: grid; grid-template-columns: 1.25fr .85fr; gap: 1.75rem; align-items: center;
        }
        .cal-hero::after {
            content: ""; position: absolute; right: -90px; bottom: -120px; width: 320px; height: 320px;
            border-radius: 50%; background: rgba(255,255,255,.06); pointer-events: none;
        }
        .cal-kicker { text-transform: uppercase; letter-spacing: .16em; font-size: .72rem; font-weight: 700; opacity: .8; }
        .cal-hero h1 { color: #fff; font-size: clamp(1.9rem, 3.6vw, 2.8rem); font-weight: 600; margin: .4rem 0 .5rem; }
        .cal-hero .cal-sub { opacity: .92; font-size: 1.02rem; margin: 0; max-width: 42ch; }
        .cal-role { display: inline-flex; align-items: center; gap: .4rem; margin-top: 1rem; padding: .3rem .8rem;
                    border-radius: 999px; background: rgba(255,255,255,.14); font-size: .8rem; font-weight: 600; }
        .next-card {
            position: relative; z-index: 1; background: rgba(255,255,255,.12); border: 1px solid rgba(255,255,255,.24);
            backdrop-filter: blur(12px); -webkit-backdrop-filter: blur(12px); border-radius: 22px; padding: 1.3rem 1.35rem;
        }
        .next-when { display: inline-block; font-size: .74rem; font-weight: 700; text-transform: uppercase; letter-spacing: .12em;
                     background: #fff; color: #1d4a37; padding: .25rem .65rem; border-radius: 999px; }
        .next-title { font-family: var(--gg-display); font-size: 1.35rem; font-weight: 600; margin: .6rem 0 .15rem; line-height: 1.2; }
        .next-meta { opacity: .9; font-size: .92rem; }
        .next-empty { opacity: .9; }
        .hero-btn { border: 0; border-radius: 999px; padding: .5rem 1rem; font-weight: 600; font-size: .88rem;
                    background: #fff; color: #1d4a37; text-decoration: none; display: inline-flex; gap: .4rem; align-items: center; }
        .hero-btn.ghost { background: rgba(255,255,255,.14); color: #fff; border: 1px solid rgba(255,255,255,.3); }
        .hero-btn:hover { color: #1d4a37; filter: brightness(1.05); }
        .hero-btn.ghost:hover { color: #fff; }

        /* ---------- stats ---------- */
        .cal-stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; margin-top: 1.25rem; }
        .stat {
            background: var(--gg-surface); border: 1px solid var(--gg-border); border-radius: 20px;
            padding: 1.05rem 1.15rem; box-shadow: var(--gg-shadow-sm); display: flex; gap: .9rem; align-items: center;
            transition: transform .22s ease, box-shadow .22s ease;
        }
        .stat:hover { transform: translateY(-3px); box-shadow: var(--gg-shadow-md); }
        .stat-ico { width: 46px; height: 46px; border-radius: 15px; display: grid; place-items: center; font-size: 1.2rem; flex: 0 0 auto; }
        .stat-num { font-family: var(--gg-display); font-size: 1.85rem; font-weight: 600; line-height: 1; font-variant-numeric: tabular-nums; }
        .stat-lbl { color: var(--gg-muted); font-size: .8rem; font-weight: 600; margin-top: .25rem; }
        .ico-green { background: var(--gg-accent-soft); color: var(--gg-accent); }
        .ico-amber { background: var(--gg-amber-soft); color: var(--gg-amber); }
        .ico-blue  { background: var(--gg-blue-soft);  color: var(--gg-blue); }
        .ico-violet{ background: rgba(123,79,160,.14); color: #7b4fa0; }
        .skeleton { color: transparent !important; background: linear-gradient(90deg, var(--gg-border) 25%, var(--gg-surface-2, #eef3f0) 50%, var(--gg-border) 75%);
                    background-size: 200% 100%; animation: cal-shimmer 1.2s infinite; border-radius: 8px; }
        @keyframes cal-shimmer { to { background-position: -200% 0; } }

        /* ---------- toolbar ---------- */
        .cal-toolbar { display: flex; flex-wrap: wrap; gap: .6rem; align-items: center; margin: 1.5rem 0 1rem; }
        .seg { display: inline-flex; background: var(--gg-surface); border: 1px solid var(--gg-border); border-radius: 999px; padding: 4px; }
        .seg button { border: 0; background: transparent; color: var(--gg-muted); border-radius: 999px; padding: .42rem 1rem; font-weight: 600; font-size: .88rem; transition: background .2s, color .2s; }
        .seg button.on { background: var(--gg-accent); color: #fff; box-shadow: 0 6px 16px -8px var(--gg-accent); }
        .team-select { border-radius: 999px; border: 1px solid var(--gg-border); background: var(--gg-surface); color: var(--gg-text);
                       padding: .45rem .9rem; font-size: .88rem; font-weight: 600; }
        .chip { border: 1px solid var(--gg-border); background: var(--gg-surface); color: var(--gg-muted); border-radius: 999px;
                padding: .38rem .85rem; font-size: .84rem; font-weight: 600; display: inline-flex; align-items: center; gap: .4rem;
                transition: all .2s; }
        .chip .dot { width: 9px; height: 9px; border-radius: 50%; display: inline-block; }
        .chip.on { color: var(--gg-text); border-color: var(--gg-text); }
        .chip:not(.on) { opacity: .55; }
        .chip-status.on { background: var(--gg-accent-soft); }
        .search { position: relative; margin-left: auto; min-width: 220px; }
        .search input { width: 100%; border-radius: 999px; border: 1px solid var(--gg-border); background: var(--gg-surface); color: var(--gg-text);
                        padding: .48rem .95rem .48rem 2.3rem; font-size: .88rem; }
        .search input:focus { outline: 2px solid var(--gg-accent); outline-offset: 1px; border-color: transparent; }
        .search i { position: absolute; left: .9rem; top: 50%; transform: translateY(-50%); color: var(--gg-muted); }
        .btn-soft { border-radius: 999px; border: 1px solid var(--gg-border); background: var(--gg-surface); color: var(--gg-text);
                    padding: .45rem .95rem; font-size: .88rem; font-weight: 600; display: inline-flex; gap: .4rem; align-items: center; text-decoration: none; }
        .btn-soft:hover { border-color: var(--gg-accent); color: var(--gg-accent); }

        /* ---------- layout ---------- */
        .cal-grid { display: grid; grid-template-columns: minmax(0, 1fr) 370px; gap: 1.25rem; align-items: start; }
        .cal-card { background: var(--gg-surface); border: 1px solid var(--gg-border); border-radius: 24px; padding: 1.25rem; box-shadow: var(--gg-shadow-sm); }
        .cal-side { display: flex; flex-direction: column; gap: 1rem; position: sticky; top: 92px; }
        .side-card { background: var(--gg-surface); border: 1px solid var(--gg-border); border-radius: 22px; padding: 1.1rem 1.15rem; box-shadow: var(--gg-shadow-sm); }
        .side-card h3 { font-size: 1.12rem; margin: 0 0 .8rem; display: flex; align-items: center; justify-content: space-between; gap: .5rem; }
        .side-card h3 small { font-family: var(--gg-sans); font-size: .78rem; color: var(--gg-muted); font-weight: 600; }

        /* ---------- FullCalendar theming ---------- */
        .fc {
            --fc-border-color: var(--gg-border); --fc-today-bg-color: var(--gg-accent-soft);
            --fc-page-bg-color: transparent; --fc-neutral-text-color: var(--gg-muted);
            --fc-list-event-hover-bg-color: var(--gg-accent-soft); --fc-highlight-color: var(--gg-accent-soft);
            font-family: var(--gg-sans); color: var(--gg-text);
        }
        .fc .fc-toolbar-title { font-family: var(--gg-display); font-size: 1.55rem; font-weight: 600; }
        .fc .fc-button-primary { background: var(--gg-surface); border: 1px solid var(--gg-border); color: var(--gg-text);
                                 border-radius: 999px; font-weight: 600; text-transform: capitalize; box-shadow: none; padding: .42rem .9rem; }
        .fc .fc-button-primary:hover, .fc .fc-button-primary:not(:disabled).fc-button-active { background: var(--gg-accent); border-color: var(--gg-accent); color: #fff; }
        .fc .fc-button-primary:focus { box-shadow: 0 0 0 2px var(--gg-accent-soft); }
        .fc .fc-daygrid-day-number { font-weight: 600; padding: .45rem .6rem; }
        .fc .fc-day-today .fc-daygrid-day-number { background: var(--gg-accent); color: #fff; border-radius: 999px; margin: .3rem; padding: .15rem .55rem; }
        .fc .fc-daygrid-day { cursor: pointer; transition: background .15s; }
        .fc .fc-daygrid-day:hover { background: color-mix(in srgb, var(--gg-accent-soft) 60%, transparent); }
        .fc .fc-daygrid-day.is-selected { box-shadow: inset 0 0 0 2px var(--gg-accent); }
        .fc .fc-col-header-cell-cushion { font-weight: 700; color: var(--gg-muted); text-transform: uppercase; font-size: .72rem; letter-spacing: .08em; padding: .6rem 0; }
        .fc .fc-daygrid-day-frame { min-height: 112px; }
        .fc .fc-more-link { color: var(--gg-accent); font-weight: 700; font-size: .78rem; }
        .fc .fc-event { border: 0 !important; background: transparent !important; cursor: pointer; }
        .fc .fc-list-event:hover td { background: var(--gg-accent-soft); }
        .fc-list-event-title a { color: var(--gg-text) !important; font-weight: 600; }
        .ev-chip { display: flex; align-items: center; gap: .4rem; padding: .3rem .55rem; border-radius: 10px; color: #fff;
                   font-size: .77rem; font-weight: 600; overflow: hidden; box-shadow: 0 4px 10px -6px rgba(0,0,0,.45);
                   transition: transform .15s ease, box-shadow .15s ease; margin: 1px 2px; }
        .fc-event:hover .ev-chip { transform: translateY(-1px); box-shadow: 0 8px 16px -8px rgba(0,0,0,.5); }
        .ev-chip i { flex: 0 0 auto; font-size: .85rem; }
        .ev-text { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .ev-job-notstarted .ev-chip { background: linear-gradient(135deg, #3b82c4, #2b6cb0); }
        .ev-job-inprogress .ev-chip { background: linear-gradient(135deg, #d6902c, #a8650f); }
        .ev-job-completed  .ev-chip { background: linear-gradient(135deg, #5f9e7f, #3d7a5c); }
        .ev-evaluation     .ev-chip { background: linear-gradient(135deg, #9a6bbd, #7b4fa0); }
        .fc-list-event-dot { border-color: var(--gg-accent) !important; }

        /* ---------- side panels ---------- */
        .day-item { display: flex; gap: .8rem; padding: .75rem .8rem; border-radius: 16px; border: 1px solid var(--gg-border);
                    margin-bottom: .6rem; cursor: pointer; transition: transform .18s ease, border-color .18s ease, box-shadow .18s ease; }
        .day-item:last-child { margin-bottom: 0; }
        .day-item:hover { transform: translateX(3px); border-color: var(--gg-accent); box-shadow: var(--gg-shadow-sm); }
        .day-bar { width: 4px; border-radius: 4px; flex: 0 0 4px; }
        .day-main { min-width: 0; flex: 1 1 auto; }
        .day-main .t { font-weight: 700; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .day-main .m { color: var(--gg-muted); font-size: .8rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .pill { font-size: .68rem; font-weight: 700; padding: .18rem .55rem; border-radius: 999px; white-space: nowrap; }
        .pill-notstarted { background: var(--gg-blue-soft); color: var(--gg-blue); }
        .pill-inprogress { background: var(--gg-amber-soft); color: var(--gg-amber); }
        .pill-completed  { background: var(--gg-accent-soft); color: var(--gg-accent); }
        .pill-evaluation { background: rgba(123,79,160,.14); color: #7b4fa0; }
        .empty { text-align: center; color: var(--gg-muted); padding: 1.4rem .5rem; }
        .empty i { font-size: 2rem; display: block; margin-bottom: .45rem; color: var(--gg-accent); opacity: .9; }
        .up-row { display: flex; gap: .75rem; align-items: center; padding: .55rem .4rem; border-radius: 12px; cursor: pointer; transition: background .15s; }
        .up-row:hover { background: var(--gg-accent-soft); }
        .up-date { width: 52px; text-align: center; border-radius: 12px; border: 1px solid var(--gg-border); padding: .3rem 0; flex: 0 0 auto; }
        .up-date b { display: block; font-family: var(--gg-display); font-size: 1.25rem; line-height: 1; }
        .up-date span { font-size: .66rem; text-transform: uppercase; letter-spacing: .08em; color: var(--gg-muted); font-weight: 700; }
        .legend-row { display: flex; flex-wrap: wrap; gap: .5rem .9rem; font-size: .8rem; color: var(--gg-muted); }
        .legend-row span { display: inline-flex; align-items: center; gap: .4rem; }
        .legend-row i { width: 10px; height: 10px; border-radius: 3px; display: inline-block; }

        /* ---------- event modal ---------- */
        .ev-modal .modal-content { background: var(--gg-surface); color: var(--gg-text); border: 1px solid var(--gg-border);
                                   border-radius: 26px; box-shadow: var(--gg-shadow-md); overflow: hidden; }
        .ev-head { padding: 1.4rem 1.5rem 1.1rem; color: #fff; background: linear-gradient(135deg, #2d6a4f, #1d4a37); }
        .ev-head.k-evaluation { background: linear-gradient(135deg, #9a6bbd, #6c4590); }
        .ev-head.k-inprogress { background: linear-gradient(135deg, #d6902c, #9a5f10); }
        .ev-head.k-completed  { background: linear-gradient(135deg, #5f9e7f, #33654b); }
        .ev-head.k-notstarted { background: linear-gradient(135deg, #3b82c4, #2b6cb0); }
        .ev-head h4 { color: #fff; font-size: 1.4rem; margin: .5rem 0 .2rem; }
        .ev-head .when { opacity: .92; font-size: .9rem; }
        .ev-body { padding: 1.2rem 1.5rem; }
        .info-row { display: flex; gap: .75rem; padding: .6rem 0; border-bottom: 1px dashed var(--gg-border); }
        .info-row:last-child { border-bottom: 0; }
        .info-row > i { color: var(--gg-accent); font-size: 1.05rem; margin-top: .1rem; width: 18px; text-align: center; }
        .info-row .k { color: var(--gg-muted); font-size: .75rem; font-weight: 700; text-transform: uppercase; letter-spacing: .08em; }
        .info-row .v { font-weight: 600; word-break: break-word; }
        .ev-actions { display: flex; flex-wrap: wrap; gap: .5rem; padding: 0 1.5rem 1.4rem; }
        .ev-actions .btn-soft { flex: 1 1 auto; justify-content: center; }
        .ev-actions .btn-go { background: var(--gg-accent); color: #fff; border-color: var(--gg-accent); }
        .ev-actions .btn-go:hover { color: #fff; filter: brightness(1.08); }

        /* ---------- toast ---------- */
        .cal-toast { position: fixed; left: 50%; bottom: 24px; transform: translate(-50%, 20px); opacity: 0; z-index: 2000;
                     background: var(--gg-text); color: var(--gg-bg); padding: .7rem 1.1rem; border-radius: 14px;
                     font-weight: 600; font-size: .9rem; box-shadow: var(--gg-shadow-md); transition: all .3s ease; }
        .cal-toast.show { opacity: 1; transform: translate(-50%, 0); }
        .cal-toast.err { background: #9b2c2c; color: #fff; }

        /* ---------- responsive ---------- */
        @media (max-width: 1150px) {
            .cal-grid { grid-template-columns: 1fr; }
            .cal-side { position: static; flex-direction: row; flex-wrap: wrap; }
            .cal-side > .side-card { flex: 1 1 320px; }
        }
        @media (max-width: 900px) {
            .cal-hero { grid-template-columns: 1fr; padding: 1.6rem; }
            .cal-stats { grid-template-columns: repeat(2, 1fr); }
            .search { margin-left: 0; width: 100%; }
        }
        @media (max-width: 560px) {
            .fc .fc-toolbar { flex-direction: column; gap: .5rem; }
            .fc .fc-daygrid-day-frame { min-height: 70px; }
            .ev-chip .ev-text { font-size: .7rem; }
        }
        @media (prefers-reduced-motion: reduce) {
            .cal-wrap > *, .stat, .day-item, .chip, .seg button { animation: none !important; transition: none !important; }
        }
    </style>

    <div class="cal-page">
    <div class="cal-wrap">

        <!-- ================= Hero: personal greeting + next up ================= -->
        <section class="cal-hero">
            <div>
                <div class="cal-kicker">My schedule</div>
                <h1 id="cal-greet" data-name="<%: FirstName %>">Hello, <%: FirstName %>.</h1>
                <p class="cal-sub" id="cal-sub">Loading your week…</p>
                <% if (!string.IsNullOrEmpty(RoleName)) { %>
                <span class="cal-role"><i class="bi bi-person-badge"></i><%: RoleName %></span>
                <% } %>
            </div>
            <div class="next-card" id="next-card">
                <div class="next-when">Next up</div>
                <div class="next-title skeleton" style="min-height:1.6rem;width:80%;">Loading</div>
                <div class="next-meta skeleton" style="min-height:1rem;width:60%;">Loading</div>
            </div>
        </section>

        <!-- ================= Stats ================= -->
        <section class="cal-stats" id="cal-stats">
            <div class="stat">
                <div class="stat-ico ico-blue"><i class="bi bi-calendar-week"></i></div>
                <div>
                    <div class="stat-num" id="st-week">–</div>
                    <div class="stat-lbl" id="st-week-lbl">Jobs this week</div>
                </div>
            </div>
            <div class="stat">
                <div class="stat-ico ico-green"><i class="bi bi-clock-history"></i></div>
                <div>
                    <div class="stat-num" id="st-hours">–</div>
                    <div class="stat-lbl">Hours booked this week</div>
                </div>
            </div>
            <div class="stat">
                <div class="stat-ico ico-amber"><i class="bi bi-tools"></i></div>
                <div>
                    <div class="stat-num" id="st-progress">–</div>
                    <div class="stat-lbl">In progress now</div>
                </div>
            </div>
            <div class="stat" id="st-eval-card" <%= CanSeeTeam ? "" : "style=\"display:none\"" %>>
                <div class="stat-ico ico-violet"><i class="bi bi-clipboard2-check"></i></div>
                <div>
                    <div class="stat-num" id="st-eval">–</div>
                    <div class="stat-lbl">Site evaluations this week</div>
                </div>
            </div>
        </section>

        <!-- ================= Toolbar ================= -->
        <div class="cal-toolbar">
            <div class="seg" role="tablist" aria-label="Whose schedule">
                <button type="button" class="on" data-scope="me">My schedule</button>
                <% if (CanSeeTeam) { %><button type="button" data-scope="team">Whole team</button><% } %>
            </div>
            <% if (CanSeeTeam) { %>
            <select id="team-select" class="team-select" style="display:none" aria-label="Filter by crew member"></select>
            <% } %>

            <button type="button" class="chip on" data-kind="job"><span class="dot" style="background:#2b6cb0"></span>Jobs</button>
            <% if (CanSeeTeam) { %>
            <button type="button" class="chip on" data-kind="evaluation"><span class="dot" style="background:#7b4fa0"></span>Site evaluations</button>
            <% } %>

            <div class="seg" id="status-seg" aria-label="Filter by job status">
                <button type="button" class="on" data-status="all">All</button>
                <button type="button" data-status="notstarted">Not started</button>
                <button type="button" data-status="inprogress">In progress</button>
                <button type="button" data-status="completed">Completed</button>
            </div>

            <div class="search">
                <i class="bi bi-search"></i>
                <input id="cal-search" type="search" placeholder="Search client, address, job #…" autocomplete="off" />
            </div>
            <button type="button" class="btn-soft" id="btn-ics" title="Download your schedule for your phone calendar">
                <i class="bi bi-phone"></i>Add to my phone
            </button>
        </div>

        <!-- ================= Calendar + side panels ================= -->
        <div class="cal-grid">
            <div class="cal-card">
                <div id="calendar"></div>
            </div>

            <aside class="cal-side">
                <div class="side-card">
                    <h3><span id="day-title">Today</span><small id="day-count"></small></h3>
                    <div id="day-list"></div>
                </div>

                <div class="side-card">
                    <h3>Coming up <small>next 14 days</small></h3>
                    <div id="upcoming-list"></div>
                </div>

                <div class="side-card">
                    <div class="legend-row">
                        <span><i style="background:#2b6cb0"></i>Not started</span>
                        <span><i style="background:#a8650f"></i>In progress</span>
                        <span><i style="background:#3d7a5c"></i>Completed</span>
                        <% if (CanSeeTeam) { %><span><i style="background:#7b4fa0"></i>Site evaluation</span><% } %>
                    </div>
                </div>
            </aside>
        </div>
    </div>
    </div>

    <!-- ================= Event detail modal ================= -->
    <div class="modal fade ev-modal" id="ev-modal" tabindex="-1" aria-labelledby="ev-title" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered modal-lg">
            <div class="modal-content">
                <div class="ev-head" id="ev-head">
                    <div class="d-flex justify-content-between align-items-start gap-2">
                        <span class="pill" id="ev-pill"></span>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <h4 id="ev-title"></h4>
                    <div class="when" id="ev-when"></div>
                </div>
                <div class="ev-body">
                    <div class="info-row"><i class="bi bi-person"></i><div><div class="k">Client</div><div class="v" id="ev-client"></div></div></div>
                    <div class="info-row" id="ev-phone-row"><i class="bi bi-telephone"></i><div><div class="k">Phone</div><div class="v"><a id="ev-phone" href="#"></a></div></div></div>
                    <div class="info-row"><i class="bi bi-geo-alt"></i><div><div class="k">Site</div><div class="v" id="ev-address"></div></div></div>
                    <div class="info-row"><i class="bi bi-list-check"></i><div><div class="k">Work requested</div><div class="v" id="ev-items"></div></div></div>
                    <div class="info-row" id="ev-crew-row"><i class="bi bi-people"></i><div><div class="k">Crew</div><div class="v" id="ev-crew"></div></div></div>
                    <div class="info-row" id="ev-hours-row"><i class="bi bi-hourglass-split"></i><div><div class="k">Hours</div><div class="v" id="ev-hours"></div></div></div>
                </div>
                <div class="ev-actions">
                    <a class="btn-soft btn-go" id="ev-dir" href="#" target="_blank" rel="noopener"><i class="bi bi-signpost-2"></i>Directions</a>
                    <a class="btn-soft" id="ev-call" href="#"><i class="bi bi-telephone"></i>Call client</a>
                    <a class="btn-soft" id="ev-request" href="#"><i class="bi bi-clipboard-data"></i>Request record</a>
                    <a class="btn-soft" id="ev-open" href="#"><i class="bi bi-box-arrow-up-right"></i>Open</a>
                </div>
            </div>
        </div>
    </div>

    <script>
    (function () {
        'use strict';

        var CFG = <%= ConfigJson %>;
        var API = CFG.apiUrl;
        var CAN_TEAM = !!CFG.canSeeTeam;

        var state = {
            scope: 'me',
            staffId: 0,
            kinds: { job: true, evaluation: true },
            status: 'all',
            query: '',
            selected: ymd(new Date()),
            known: {},            // id -> event (everything fetched so far)
            cache: {},            // range key -> events
            calendar: null
        };

        var COLORS = { notstarted: '#2b6cb0', inprogress: '#a8650f', completed: '#3d7a5c', evaluation: '#7b4fa0' };

        // ---------- helpers ----------
        function $(id) { return document.getElementById(id); }
        function esc(s) {
            return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
                return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
            });
        }
        function pad(n) { return (n < 10 ? '0' : '') + n; }
        function ymd(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()); }
        function parseDay(s) { var p = s.split('-'); return new Date(+p[0], +p[1] - 1, +p[2]); }
        function todayStr() { return ymd(new Date()); }
        function daysFrom(s) {
            var t = new Date(); t.setHours(0, 0, 0, 0);
            return Math.round((parseDay(s) - t) / 86400000);
        }
        function plural(n, word) { return n + ' ' + word + (n === 1 ? '' : 's'); }
        function toast(msg, isErr) {
            var t = document.createElement('div');
            t.className = 'cal-toast' + (isErr ? ' err' : '');
            t.textContent = msg;
            document.body.appendChild(t);
            requestAnimationFrame(function () { t.classList.add('show'); });
            setTimeout(function () {
                t.classList.remove('show');
                setTimeout(function () { t.remove(); }, 300);
            }, 3200);
        }
        function countUp(el, to, decimals) {
            if (!el) return;
            var from = 0, start = null, dur = 700;
            el.classList.remove('skeleton');
            function step(ts) {
                if (!start) start = ts;
                var p = Math.min((ts - start) / dur, 1);
                var eased = 1 - Math.pow(1 - p, 3);
                var v = from + (to - from) * eased;
                el.textContent = decimals ? v.toFixed(decimals) : Math.round(v);
                if (p < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        }
        function statusKey(s) {
            var v = (s || '').toLowerCase();
            if (v === 'in progress') return 'inprogress';
            if (v === 'completed') return 'completed';
            return 'notstarted';
        }
        function colorFor(p) { return p.kind === 'evaluation' ? COLORS.evaluation : COLORS[statusKey(p.status)]; }
        function pillFor(p) {
            if (p.kind === 'evaluation') return '<span class="pill pill-evaluation">Site evaluation</span>';
            var k = statusKey(p.status);
            var label = { notstarted: 'Not started', inprogress: 'In progress', completed: 'Completed' }[k];
            return '<span class="pill pill-' + k + '">' + label + '</span>';
        }
        function mapsLink(p) {
            if (p.lat != null && p.lng != null) return 'https://www.google.com/maps/dir/?api=1&destination=' + p.lat + ',' + p.lng;
            return 'https://www.google.com/maps/dir/?api=1&destination=' + encodeURIComponent(p.address || '');
        }
        function telLink(phone) { return phone ? 'tel:' + String(phone).replace(/[^+0-9]/g, '') : null; }

        function api(params) {
            var q = Object.keys(params).map(function (k) {
                return encodeURIComponent(k) + '=' + encodeURIComponent(params[k]);
            }).join('&');
            return fetch(API + '?' + q, { credentials: 'same-origin', headers: { Accept: 'application/json' } })
                .then(function (r) {
                    if (!r.ok) {
                        return r.json().catch(function () { return {}; }).then(function (b) {
                            throw new Error(b.error || 'The calendar could not be loaded.');
                        });
                    }
                    return r.json();
                });
        }

        // ---------- filters ----------
        function visible(p) {
            if (!p) return false;
            if (!state.kinds[p.kind]) return false;
            if (state.status !== 'all') {
                if (p.kind !== 'job') return false;
                if (statusKey(p.status) !== state.status) return false;
            }
            if (state.query) {
                var hay = (p.client + ' ' + p.address + ' ' + p.items + ' ' + p.crew + ' ' +
                           (p.jobId || '') + ' ' + (p.requestId || '')).toLowerCase();
                if (hay.indexOf(state.query) === -1) return false;
            }
            return true;
        }

        function toFc(ev) {
            var p = ev.extendedProps;
            var cls = 'ev-' + p.kind + (p.kind === 'job' ? '-' + statusKey(p.status) : '');
            return {
                id: ev.id,
                title: ev.title,
                start: ev.start,
                allDay: true,
                classNames: [cls],
                display: visible(p) ? 'auto' : 'none',
                extendedProps: p
            };
        }

        function applyFilters() {
            if (state.calendar) {
                state.calendar.getEvents().forEach(function (e) {
                    e.setProp('display', visible(e.extendedProps) ? 'auto' : 'none');
                });
            }
            renderSide();
        }

        // ---------- data ----------
        function remember(list) {
            list.forEach(function (ev) { state.known[ev.id] = ev; });
        }

        function fetchRange(startStr, endStr) {
            var key = [state.scope, state.staffId, startStr, endStr].join('|');
            if (state.cache[key]) return Promise.resolve(state.cache[key]);
            return api({ action: 'events', scope: state.scope, staffId: state.staffId, start: startStr, end: endStr })
                .then(function (list) { state.cache[key] = list; return list; });
        }

        function loadStats() {
            return api({ action: 'stats', scope: state.scope, staffId: state.staffId }).then(function (s) {
                var team = state.scope === 'team';
                var weekLbl = $('st-week-lbl');
                weekLbl.textContent = team ? 'Team jobs this week' : 'Jobs this week';
                countUp($('st-week'), s.weekJobs, 0);
                countUp($('st-hours'), s.weekHours, 1);
                countUp($('st-progress'), s.inProgress, 0);
                countUp($('st-eval'), s.evaluationsWeek, 0);

                var sub;
                if (team) {
                    sub = plural(s.weekJobs, 'job') + ' scheduled across the team this week' +
                          (s.todayCount ? ', ' + plural(s.todayCount, 'item') + ' today.' : '.');
                } else {
                    sub = s.weekJobs === 0
                        ? 'Nothing booked for you this week. Enjoy the breathing room.'
                        : 'You have ' + plural(s.weekJobs, 'job') + ' this week' +
                          (s.weekHours ? ' · ' + s.weekHours + ' hours booked' : '') + '.';
                    if (s.todayCount) sub += ' ' + (s.todayCount === 1 ? 'One item today.' : s.todayCount + ' items today.');
                }
                $('cal-sub').textContent = sub;
                renderNext(s.nextUp);
            }).catch(function (e) { toast(e.message, true); });
        }

        function renderNext(ev) {
            var box = $('next-card');
            if (!ev) {
                box.innerHTML =
                    '<div class="next-when">Next up</div>' +
                    '<div class="next-title">Clear skies</div>' +
                    '<div class="next-empty">Nothing scheduled ahead. A good moment to catch up.</div>';
                return;
            }
            remember([ev]);
            var p = ev.extendedProps;
            var d = daysFrom(ev.start);
            var when = d < 0 ? 'Overdue' : d === 0 ? 'Today' : d === 1 ? 'Tomorrow' : 'In ' + d + ' days';
            var phoneBtn = telLink(p.phone) ? '<a class="hero-btn ghost" href="' + telLink(p.phone) + '"><i class="bi bi-telephone"></i>Call</a>' : '';
            box.innerHTML =
                '<div class="next-when">' + esc(when) + '</div>' +
                '<div class="next-title">' + esc(ev.title) + '</div>' +
                '<div class="next-meta"><i class="bi bi-geo-alt me-1"></i>' + esc(p.address || 'No address') + '</div>' +
                '<div class="d-flex flex-wrap gap-2 mt-3">' +
                    '<button type="button" class="hero-btn" data-open="' + esc(ev.id) + '"><i class="bi bi-info-circle"></i>Details</button>' +
                    '<a class="hero-btn ghost" target="_blank" rel="noopener" href="' + mapsLink(p) + '"><i class="bi bi-signpost-2"></i>Directions</a>' +
                    phoneBtn +
                '</div>';
        }

        // ---------- side panels ----------
        function knownList() {
            return Object.keys(state.known).map(function (k) { return state.known[k]; });
        }

        function renderSide() {
            renderDay();
            renderUpcoming();
        }

        function renderDay() {
            var d = state.selected;
            var today = todayStr();
            var label = d === today ? 'Today' : parseDay(d).toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'short' });
            $('day-title').textContent = label;

            var items = knownList()
                .filter(function (e) { return e.start === d && visible(e.extendedProps); })
                .sort(function (a, b) { return a.extendedProps.kind.localeCompare(b.extendedProps.kind) || a.title.localeCompare(b.title); });

            $('day-count').textContent = items.length ? plural(items.length, 'item') : '';
            if (!items.length) {
                $('day-list').innerHTML =
                    '<div class="empty"><i class="bi bi-cup-hot"></i>Nothing on this day. Pick another date on the calendar.</div>';
                return;
            }
            $('day-list').innerHTML = items.map(function (e) {
                var p = e.extendedProps;
                var crew = p.crew ? '<div class="m"><i class="bi bi-people me-1"></i>' + esc(p.crew) + '</div>' : '';
                return '<div class="day-item" data-open="' + esc(e.id) + '">' +
                         '<span class="day-bar" style="background:' + colorFor(p) + '"></span>' +
                         '<div class="day-main">' +
                           '<div class="d-flex align-items-center justify-content-between gap-2 mb-1">' +
                             '<span class="t">' + esc(e.title) + '</span>' + pillFor(p) +
                           '</div>' +
                           '<div class="m"><i class="bi bi-geo-alt me-1"></i>' + esc(p.address || 'No address') + '</div>' +
                           crew +
                         '</div>' +
                       '</div>';
            }).join('');
        }

        function renderUpcoming() {
            var today = todayStr();
            var list = knownList()
                .filter(function (e) {
                    var d = daysFrom(e.start);
                    return d >= 0 && d <= 14 && visible(e.extendedProps);
                })
                .sort(function (a, b) { return a.start < b.start ? -1 : a.start > b.start ? 1 : a.title.localeCompare(b.title); })
                .slice(0, 6);

            if (!list.length) {
                $('upcoming-list').innerHTML = '<div class="empty"><i class="bi bi-calendar-check"></i>No jobs in the next two weeks.</div>';
                return;
            }
            $('upcoming-list').innerHTML = list.map(function (e) {
                var dt = parseDay(e.start);
                return '<div class="up-row" data-open="' + esc(e.id) + '">' +
                         '<div class="up-date"><b>' + dt.getDate() + '</b><span>' + dt.toLocaleDateString(undefined, { month: 'short' }) + '</span></div>' +
                         '<div class="day-main">' +
                           '<div class="t" style="font-weight:700;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">' + esc(e.title) + '</div>' +
                           '<div class="m" style="color:var(--gg-muted);font-size:.8rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">' +
                             esc(e.extendedProps.address || 'No address') + '</div>' +
                         '</div>' +
                         pillFor(e.extendedProps) +
                       '</div>';
            }).join('');
        }

        // ---------- detail modal ----------
        function openById(id) {
            var ev = state.known[id];
            if (!ev) return;
            var p = ev.extendedProps;
            var k = p.kind === 'evaluation' ? 'evaluation' : statusKey(p.status);

            $('ev-head').className = 'ev-head k-' + k;
            $('ev-pill').className = 'pill';
            $('ev-pill').style.background = 'rgba(255,255,255,.2)';
            $('ev-pill').style.color = '#fff';
            $('ev-pill').textContent = p.kind === 'evaluation' ? 'Site evaluation' : ({ notstarted: 'Not started', inprogress: 'In progress', completed: 'Completed' }[k]);
            $('ev-title').textContent = ev.title;
            $('ev-when').textContent = parseDay(ev.start).toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
            $('ev-client').textContent = p.client || '—';
            $('ev-address').textContent = p.address || 'No address on file';
            $('ev-items').textContent = p.items || 'No items listed';

            var tel = telLink(p.phone);
            $('ev-phone-row').style.display = tel ? '' : 'none';
            if (tel) { $('ev-phone').textContent = p.phone; $('ev-phone').href = tel; }
            $('ev-call').style.display = tel ? '' : 'none';
            if (tel) $('ev-call').href = tel;

            var isJob = p.kind === 'job';
            $('ev-crew-row').style.display = isJob ? '' : 'none';
            $('ev-crew').textContent = p.crew || 'Unassigned';
            $('ev-hours-row').style.display = isJob ? '' : 'none';
            $('ev-hours').textContent = (state.scope === 'me' && !p.isMine && isJob && p.totalHours === 0)
                ? 'Not on your crew'
                : (isJob ? (state.scope === 'me' ? p.myHours : p.totalHours) + ' h logged' : '');

            $('ev-dir').href = mapsLink(p);
            $('ev-open').href = ev.url || '#';
            $('ev-open').style.display = ev.url ? '' : 'none';
            $('ev-request').href = p.requestUrl || '#';
            $('ev-request').style.display = p.requestUrl && !(p.kind === 'evaluation' && ev.url === p.requestUrl) ? '' : 'none';

            bootstrap.Modal.getOrCreateInstance($('ev-modal')).show();
        }

        // ---------- scope / team picker ----------
        function setScope(scope) {
            state.scope = scope;
            document.querySelectorAll('[data-scope]').forEach(function (b) {
                b.classList.toggle('on', b.getAttribute('data-scope') === scope);
            });
            var sel = $('team-select');
            if (sel) sel.style.display = scope === 'team' ? '' : 'none';
            $('st-eval-card') && ($('st-eval-card').style.display = (CAN_TEAM) ? '' : 'none');
            refresh();
        }

        function refresh() {
            state.known = {};
            if (state.calendar) state.calendar.refetchEvents();
            loadStats();
        }

        function loadStaff() {
            if (!CAN_TEAM) return;
            api({ action: 'staff' }).then(function (d) {
                var sel = $('team-select');
                if (!sel) return;
                var opts = ['<option value="0">Everyone</option>'];
                (d.staff || []).forEach(function (s) {
                    opts.push('<option value="' + s.id + '">' + esc(s.name) + (s.role ? ' · ' + esc(s.role) : '') + '</option>');
                });
                sel.innerHTML = opts.join('');
            }).catch(function () { /* team picker is optional */ });
        }

        // ---------- calendar ----------
        function initCalendar() {
            var el = $('calendar');
            state.calendar = new FullCalendar.Calendar(el, {
                initialView: 'dayGridMonth',
                initialDate: new Date(),
                firstDay: 1,
                height: 'auto',
                nowIndicator: true,
                dayMaxEvents: 3,
                fixedWeekCount: false,
                headerToolbar: {
                    left: 'prev,next today',
                    center: 'title',
                    right: 'dayGridMonth,timeGridWeek,listMonth'
                },
                buttonText: { today: 'Today', dayGridMonth: 'Month', timeGridWeek: 'Week', listMonth: 'Agenda' },
                events: function (info, ok, fail) {
                    fetchRange(info.startStr, info.endStr).then(function (list) {
                        remember(list);
                        ok(list.map(toFc));
                        renderSide();
                    }).catch(function (e) {
                        toast(e.message, true);
                        fail(e);
                    });
                },
                eventContent: function (arg) {
                    var p = arg.event.extendedProps;
                    var icon = p.kind === 'evaluation' ? 'bi-clipboard2-check' : 'bi-tools';
                    return { html: '<div class="ev-chip" title="' + esc(arg.event.title) + '"><i class="bi ' + icon + '"></i>' +
                                   '<span class="ev-text">' + esc(arg.event.title) + '</span></div>' };
                },
                eventClick: function (info) {
                    info.jsEvent.preventDefault();
                    openById(info.event.id);
                },
                dateClick: function (info) {
                    state.selected = info.dateStr.slice(0, 10);
                    highlightSelected();
                    renderDay();
                },
                datesSet: function () { highlightSelected(); }
            });
            state.calendar.render();
        }

        function highlightSelected() {
            document.querySelectorAll('#calendar .fc-daygrid-day').forEach(function (td) {
                td.classList.toggle('is-selected', td.getAttribute('data-date') === state.selected);
            });
        }

        // ---------- wiring ----------
        function wire() {
            // greeting
            var h = new Date().getHours();
            var part = h < 12 ? 'morning' : h < 17 ? 'afternoon' : 'evening';
            var name = $('cal-greet').getAttribute('data-name');
            $('cal-greet').textContent = 'Good ' + part + ', ' + name + '.';

            document.querySelectorAll('[data-scope]').forEach(function (b) {
                b.addEventListener('click', function () { setScope(b.getAttribute('data-scope')); });
            });
            document.querySelectorAll('[data-kind]').forEach(function (b) {
                b.addEventListener('click', function () {
                    var k = b.getAttribute('data-kind');
                    state.kinds[k] = !state.kinds[k];
                    b.classList.toggle('on', state.kinds[k]);
                    applyFilters();
                });
            });
            document.querySelectorAll('#status-seg [data-status]').forEach(function (b) {
                b.addEventListener('click', function () {
                    state.status = b.getAttribute('data-status');
                    document.querySelectorAll('#status-seg [data-status]').forEach(function (x) {
                        x.classList.toggle('on', x === b);
                    });
                    applyFilters();
                });
            });

            var search = $('cal-search');
            var t = null;
            search.addEventListener('input', function () {
                clearTimeout(t);
                t = setTimeout(function () {
                    state.query = search.value.trim().toLowerCase();
                    applyFilters();
                }, 180);
            });

            var sel = $('team-select');
            if (sel) sel.addEventListener('change', function () {
                state.staffId = parseInt(sel.value, 10) || 0;
                refresh();
            });

            $('btn-ics').addEventListener('click', function () {
                window.location.href = API + '?action=ics';
            });

            // open details from any panel
            document.addEventListener('click', function (e) {
                var b = e.target.closest('[data-open]');
                if (b) openById(b.getAttribute('data-open'));
            });

            // keyboard: t = today, arrows = move, 1/2/3 = views (ignored while typing)
            document.addEventListener('keydown', function (e) {
                if (!state.calendar || /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) return;
                if (e.key === 't') state.calendar.today();
                else if (e.key === 'ArrowLeft') state.calendar.prev();
                else if (e.key === 'ArrowRight') state.calendar.next();
                else if (e.key === '1') state.calendar.changeView('dayGridMonth');
                else if (e.key === '2') state.calendar.changeView('timeGridWeek');
                else if (e.key === '3') state.calendar.changeView('listMonth');
            });

            if (!CAN_TEAM) {
                document.querySelectorAll('[data-kind="evaluation"]').forEach(function (x) { x.remove(); });
            }
        }

        document.addEventListener('DOMContentLoaded', function () {
            wire();
            loadStaff();
            initCalendar();
            loadStats();
            renderSide();
        });
    })();
    </script>
</asp:Content>
