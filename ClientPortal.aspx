<%@ Page Title="Client Portal" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ClientPortal.aspx.cs" Inherits="GiantGroupM3.ClientPortal" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <!-- SVG Icon Definitions -->
    <svg style="display: none;">
        <symbol id="icon-plus" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line>
        </symbol>
        <symbol id="icon-clock" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline>
        </symbol>
        <symbol id="icon-alert" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line>
        </symbol>
        <symbol id="icon-check" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline>
        </symbol>
        <symbol id="icon-edit" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
        </symbol>
        <symbol id="icon-history" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
            <polyline points="14 2 14 8 20 8"></polyline>
            <line x1="16" y1="13" x2="8" y2="13"></line>
            <line x1="16" y1="17" x2="8" y2="17"></line>
            <polyline points="10 9 9 9 8 9"></polyline>
        </symbol>
        <symbol id="icon-arrow-right" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="5" y1="12" x2="19" y2="12"></line><polyline points="12 5 19 12 12 19"></polyline>
        </symbol>
    </svg>

    <style>
        .portal-wrapper {
            font-family: 'Inter', system-ui, -apple-system, sans-serif;
            max-width: 1200px;
            margin: 0 auto;
        }

        /* Hero Header */
        .portal-hero {
            background: linear-gradient(135deg, #133a2b 0%, #22543d 50%, #2f855a 100%);
            border-radius: 20px;
            padding: 2.5rem 2rem;
            position: relative;
            overflow: hidden;
            box-shadow: 0 10px 30px -10px rgba(19, 58, 43, 0.3);
        }

        .portal-hero::before {
            content: '';
            position: absolute;
            top: -50%;
            right: -20%;
            width: 400px;
            height: 400px;
            background: radial-gradient(circle, rgba(255,255,255,0.08) 0%, rgba(255,255,255,0) 70%);
            border-radius: 50%;
            pointer-events: none;
        }

        .badge-portal {
            background: rgba(255, 255, 255, 0.15);
            backdrop-filter: blur(8px);
            border: 1px solid rgba(255, 255, 255, 0.2);
            color: #81e6d9;
            font-weight: 600;
            letter-spacing: 0.5px;
            padding: 0.4em 0.8em;
            border-radius: 30px;
            font-size: 0.75rem;
            text-transform: uppercase;
        }

        .btn-action-primary {
            background-color: #ffffff;
            color: #1a4d3e;
            font-weight: 600;
            border-radius: 12px;
            padding: 0.65rem 1.25rem;
            transition: all 0.25s ease;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            text-decoration: none;
            display: inline-flex;
            align-items: center;
            gap: 0.5rem;
            border: none;
        }

        .btn-action-primary:hover {
            background-color: #f0fff4;
            color: #133a2b;
            transform: translateY(-2px);
            box-shadow: 0 6px 16px rgba(0, 0, 0, 0.15);
        }

        /* Stat Cards */
        .stat-card {
            background: #ffffff;
            border: 1px solid #e2e8f0;
            border-radius: 16px;
            padding: 1.5rem;
            transition: all 0.25s ease;
            height: 100%;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            box-shadow: 0 2px 4px rgba(0,0,0,0.02);
        }

        .stat-card:hover {
            transform: translateY(-3px);
            box-shadow: 0 12px 24px -8px rgba(0, 0, 0, 0.08);
        }

        .stat-icon-box {
            width: 42px;
            height: 42px;
            border-radius: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        .stat-icon-box.green { background: #e6fffa; color: #0d9488; }
        .stat-icon-box.amber { background: #fffbeb; color: #d97706; }
        .stat-icon-box.blue { background: #eff6ff; color: #2563eb; }

        /* Module Cards */
        .action-card {
            background: #ffffff;
            border: 1px solid #e2e8f0;
            border-radius: 16px;
            padding: 2rem;
            transition: all 0.25s ease;
            height: 100%;
            display: flex;
            flex-direction: column;
        }

        .action-card:hover {
            transform: translateY(-3px);
            box-shadow: 0 12px 24px -8px rgba(0, 0, 0, 0.08);
            border-color: #cbd5e1;
        }

        .action-icon-circle {
            width: 52px;
            height: 52px;
            border-radius: 14px;
            background: #f0fdf4;
            color: #16a34a;
            display: flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 1.25rem;
        }

        .btn-card-action {
            border-radius: 10px;
            font-weight: 600;
            padding: 0.6rem 1.2rem;
            transition: all 0.2s ease;
            display: inline-flex;
            align-items: center;
            gap: 0.5rem;
            text-decoration: none;
        }

        .btn-card-action.btn-solid {
            background-color: #16a34a;
            color: #ffffff;
            border: none;
        }

        .btn-card-action.btn-solid:hover {
            background-color: #15803d;
            color: #ffffff;
        }

        .btn-card-action.btn-outline {
            background-color: transparent;
            color: #16a34a;
            border: 1.5px solid #16a34a;
        }

        .btn-card-action.btn-outline:hover {
            background-color: #f0fdf4;
            color: #15803d;
        }

        .svg-icon {
            width: 18px;
            height: 18px;
        }

        .svg-icon-lg {
            width: 26px;
            height: 26px;
        }
    </style>

    <div class="portal-wrapper my-4 px-3">
        <!-- Header Banner -->
        <div class="portal-hero mb-4 d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3">
            <div>
                <span class="badge-portal d-inline-block mb-2">Client Portal</span>
                <h1 class="h2 fw-bold text-white mb-2">
                    Welcome, <asp:Literal ID="litClientName" runat="server" Text="Valued Client" />!
                </h1>
                <p class="mb-0 text-white-50" style="font-size: 0.95rem;">
                    Track your property service requests, review estimates, and schedule site work seamlessly.
                </p>
            </div>
            <div class="mt-2 mt-md-0 flex-shrink-0">
                <a runat="server" href="~/Requests/SubmitRequest.aspx" class="btn-action-primary">
                    <svg class="svg-icon"><use href="#icon-plus"></use></svg>
                    New Request
                </a>
            </div>
        </div>

        <!-- Quick Status Overview -->
        <div class="row g-3 mb-5">
            <div class="col-md-4">
                <div class="stat-card">
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <span class="text-muted small fw-bold text-uppercase" style="letter-spacing: 0.5px;">Active Requests</span>
                        <div class="stat-icon-box green">
                            <svg class="svg-icon"><use href="#icon-clock"></use></svg>
                        </div>
                    </div>
                    <div>
                        <h2 class="fw-bold text-dark display-6 mb-1">
                            <asp:Literal ID="litActiveRequests" runat="server" Text="0" />
                        </h2>
                        <small class="text-muted">Currently processing</small>
                    </div>
                </div>
            </div>

            <div class="col-md-4">
                <div class="stat-card">
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <span class="text-muted small fw-bold text-uppercase" style="letter-spacing: 0.5px;">Quotes Ready</span>
                        <div class="stat-icon-box amber">
                            <svg class="svg-icon"><use href="#icon-alert"></use></svg>
                        </div>
                    </div>
                    <div>
                        <h2 class="fw-bold text-dark display-6 mb-1">
                            <asp:Literal ID="litQuotesReady" runat="server" Text="0" />
                        </h2>
                        <small class="text-warning-emphasis fw-semibold">Awaiting your approval</small>
                    </div>
                </div>
            </div>

            <div class="col-md-4">
    <div class="stat-card">
        <div class="d-flex justify-content-between align-items-center mb-3">
            <span class="text-muted small fw-bold text-uppercase" style="letter-spacing: 0.5px;">Approved Jobs</span>
            <div class="stat-icon-box blue">
                <svg class="svg-icon"><use href="#icon-check"></use></svg>
            </div>
        </div>
        <div>
            <h2 class="fw-bold text-dark display-6 mb-1">
                <asp:Literal ID="litCompletedJobs" runat="server" Text="0" />
            </h2>
            <small class="text-muted">Quotes signed & approved</small>
        </div>
    </div>
</div>
        </div>

        <!-- Client Action Modules -->
        <div class="d-flex align-items-center justify-content-between mb-3">
            <h4 class="fw-bold text-dark mb-0">Service Actions</h4>
        </div>
        
        <div class="row g-4">
            <div class="col-md-6">
                <div class="action-card">
                    <div class="action-icon-circle">
                        <svg class="svg-icon-lg"><use href="#icon-edit"></use></svg>
                    </div>
                    <h5 class="fw-bold text-dark mb-2">Submit New Job Request</h5>
                    <p class="text-secondary small mb-4 flex-grow-1" style="line-height: 1.6;">
                        Need tree removal, branch trimming, or site clearing? Submit photos and details directly to our field team for immediate review.
                    </p>
                    <div>
                        <a runat="server" href="~/Requests/SubmitRequest.aspx" class="btn-card-action btn-solid">
                            Request Service
                            <svg class="svg-icon"><use href="#icon-arrow-right"></use></svg>
                        </a>
                    </div>
                </div>
            </div>

            <div class="col-md-6">
                <div class="action-card">
                    <div class="action-icon-circle">
                        <svg class="svg-icon-lg"><use href="#icon-history"></use></svg>
                    </div>
                    <h5 class="fw-bold text-dark mb-2">Request & Job History</h5>
                    <p class="text-secondary small mb-4 flex-grow-1" style="line-height: 1.6;">
                        Track live status updates, review past estimates, and download completion reports for all your managed properties in one place.
                    </p>
                    <div>
                        <a runat="server" href="~/Requests/ViewRequests.aspx" class="btn-card-action btn-outline">
                            View History
                            <svg class="svg-icon"><use href="#icon-arrow-right"></use></svg>
                        </a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>