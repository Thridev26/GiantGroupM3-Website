<%@ Page Title="Management Portal" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ManagementPortal.aspx.cs" Inherits="GiantGroupM3.ManagementPortal" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        /* Base Container */
        .portal-container {
            max-width: 1140px;
            margin: 24px auto 40px auto;
            padding: 0 20px;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
            color: #2c3e50;
        }

        /* Hero Banner - Forest Green Styling */
        .portal-hero {
            background: linear-gradient(135deg, #1e4d3b 0%, #2d6a4f 100%);
            color: #ffffff;
            border-radius: 16px;
            padding: 36px 40px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            box-shadow: 0 10px 25px -5px rgba(30, 77, 59, 0.25);
            margin-bottom: 28px;
        }

        .badge-role {
            display: inline-block;
            font-size: 0.725rem;
            font-weight: 700;
            text-transform: uppercase;
            background: rgba(255, 255, 255, 0.18);
            color: #ffffff;
            padding: 5px 12px;
            border-radius: 20px;
            letter-spacing: 0.75px;
            margin-bottom: 12px;
            backdrop-filter: blur(4px);
        }

        .portal-hero h1 {
            margin: 0 0 8px 0;
            font-size: 2.1rem;
            font-weight: 800;
            letter-spacing: -0.5px;
            color: #ffffff;
        }

        .portal-hero p {
            margin: 0;
            color: rgba(255, 255, 255, 0.82);
            font-size: 0.975rem;
            max-width: 580px;
            line-height: 1.5;
        }

        .hero-badge-level {
            background: rgba(255, 255, 255, 0.15);
            border: 1px solid rgba(255, 255, 255, 0.25);
            color: #ffffff;
            padding: 8px 16px;
            border-radius: 8px;
            font-size: 0.875rem;
            font-weight: 600;
        }

        /* Metric Summary Cards Grid */
        .metrics-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
            gap: 20px;
            margin-bottom: 32px;
        }

        .metric-card {
            background: #ffffff;
            border: 1px solid #eef2f6;
            border-radius: 14px;
            padding: 24px;
            position: relative;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.03);
            transition: transform 0.2s ease, box-shadow 0.2s ease;
        }

        .metric-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(0, 0, 0, 0.06);
        }

        .metric-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 12px;
        }

        .metric-title {
            font-size: 0.75rem;
            font-weight: 700;
            color: #64748b;
            letter-spacing: 0.6px;
            text-transform: uppercase;
        }

        .metric-icon-circle {
            width: 36px;
            height: 36px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.95rem;
        }

        .icon-orange { background-color: #fff7ed; color: #ea580c; }
        .icon-green { background-color: #f0fdf4; color: #16a34a; }
        .icon-blue { background-color: #eff6ff; color: #2563eb; }
        .icon-purple { background-color: #faf5ff; color: #9333ea; }

        .metric-value {
            font-size: 2.35rem;
            font-weight: 800;
            color: #0f172a;
            line-height: 1;
            margin-bottom: 8px;
        }

        .metric-subtext {
            font-size: 0.825rem;
            font-weight: 600;
        }

        .metric-subtext.warning { color: #d97706; }
        .metric-subtext.success { color: #16a34a; }
        .metric-subtext.info { color: #2563eb; }
        .metric-subtext.muted { color: #64748b; }

        /* Section Headings */
        .section-title {
            font-size: 1.25rem;
            font-weight: 700;
            color: #0f172a;
            margin-bottom: 16px;
        }

        /* Hub Spotlight Card */
        .hub-hero-card {
            background: #ffffff;
            border: 1px solid #eef2f6;
            border-radius: 14px;
            padding: 28px 32px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 24px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.03);
        }

        .hero-main {
            display: flex;
            align-items: center;
            gap: 20px;
        }

        .hero-icon-wrapper {
            width: 52px;
            height: 52px;
            background: #e8f5e9;
            border-radius: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            color: #2d6a4f;
            flex-shrink: 0;
        }

        .hero-details h3 {
            margin: 0 0 6px 0;
            font-size: 1.15rem;
            font-weight: 700;
            color: #0f172a;
        }

        .hero-details p {
            margin: 0;
            color: #64748b;
            font-size: 0.9rem;
            line-height: 1.5;
            max-width: 620px;
        }

        .btn-green-action {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background-color: #2d6a4f;
            color: #ffffff !important;
            padding: 12px 22px;
            font-size: 0.9rem;
            font-weight: 600;
            border-radius: 8px;
            text-decoration: none !important;
            transition: background-color 0.15s ease, transform 0.15s ease;
        }

        .btn-green-action:hover {
            background-color: #1e4d3b;
            transform: translateY(-1px);
        }
    </style>

    <div class="portal-container">
        <!-- Matching Forest Green Hero Banner -->
        <div class="portal-hero">
            <div class="hero-content">
                <span class="badge-role">Staff Operations Hub</span>
                <h1>Welcome back, <asp:Literal ID="litStaffName" runat="server">Shawn</asp:Literal>!</h1>
                <p>View operational schedules, manage site timelines, and handle team allocations.</p>
            </div>
            <div class="hero-meta">
                <span class="hero-badge-level">User Level: <asp:Literal ID="litUserLevel" runat="server">6</asp:Literal></span>
            </div>
        </div>

        <!-- Metric Summary Cards Grid -->
        <div class="metrics-grid">
            <div class="metric-card">
                <div class="metric-header">
                    <span class="metric-title">Pending Requests</span>
                    <div class="metric-icon-circle icon-orange">🕒</div>
                </div>
                <div class="metric-value"><asp:Literal ID="litPendingRequests" runat="server">0</asp:Literal></div>
                <div class="metric-subtext warning">Needs quotation</div>
            </div>

            <div class="metric-card">
                <div class="metric-header">
                    <span class="metric-title">Scheduled Jobs</span>
                    <div class="metric-icon-circle icon-green">⚙️</div>
                </div>
                <div class="metric-value"><asp:Literal ID="litScheduledJobs" runat="server">0</asp:Literal></div>
                <div class="metric-subtext success">Active in field</div>
            </div>

            <div class="metric-card">
                <div class="metric-header">
                    <span class="metric-title">Quotes Awaiting Approval</span>
                    <div class="metric-icon-circle icon-blue">📄</div>
                </div>
                <div class="metric-value"><asp:Literal ID="litPendingQuotes" runat="server">0</asp:Literal></div>
                <div class="metric-subtext info">Sent to clients</div>
            </div>

            <div class="metric-card">
                <div class="metric-header">
                    <span class="metric-title">Total Active Clients</span>
                    <div class="metric-icon-circle icon-purple">👥</div>
                </div>
                <div class="metric-value"><asp:Literal ID="litActiveClients" runat="server">0</asp:Literal></div>
                <div class="metric-subtext muted">Registered accounts</div>
            </div>
        </div>

        <div class="section-title">Operations Control</div>

        <!-- Primary Hub Feature Card -->
        <div class="hub-hero-card">
            <div class="hero-main">
                <div class="hero-icon-wrapper">📅</div>
                <div class="hero-details">
                    <h3>Schedule & Team Allocations</h3>
                    <p>Access active felling schedules, assign field crews, track team allocations, and oversee daily operational timelines across sites.</p>
                </div>
            </div>
            <div class="hero-actions">
                <asp:HyperLink ID="lnkCalendar" runat="server" NavigateUrl="~/Calendar.aspx" CssClass="btn-green-action">
                    Open Operational Calendar &rarr;
                </asp:HyperLink>
            </div>
        </div>
    </div>
</asp:Content>