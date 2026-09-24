<%@ Page Title="Management Portal" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ManagementPortal.aspx.cs" Inherits="GiantGroupM3.ManagementPortal" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        /* Layout Container */
        .portal-container {
            max-width: 1100px;
            margin: 28px auto;
            padding: 0 20px;
            font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
            color: #1e293b;
        }

        /* Header Banner */
        .portal-header {
            background: #ffffff;
            border: 1px solid #e2e8f0;
            border-left: 6px solid #2563eb;
            border-radius: 10px;
            padding: 24px 28px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.04);
            margin-bottom: 24px;
        }

        .badge-role {
            font-size: 0.75rem;
            font-weight: 700;
            text-transform: uppercase;
            background: #eff6ff;
            color: #2563eb;
            padding: 4px 10px;
            border-radius: 6px;
            letter-spacing: 0.5px;
        }

        .portal-header h1 {
            margin: 10px 0 4px 0;
            font-size: 1.6rem;
            font-weight: 700;
            color: #0f172a;
        }

        .portal-header p {
            margin: 0;
            color: #64748b;
            font-size: 0.925rem;
        }

        .user-level-badge {
            background-color: #f8fafc;
            color: #334155;
            font-weight: 600;
            padding: 6px 14px;
            border-radius: 6px;
            font-size: 0.85rem;
            border: 1px solid #cbd5e1;
        }

        /* Metric Summary Cards Grid */
        .metrics-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 16px;
            margin-bottom: 28px;
        }

        .metric-card {
            background: #ffffff;
            border: 1px solid #e2e8f0;
            border-radius: 10px;
            padding: 20px;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
        }

        .metric-title {
            font-size: 0.725rem;
            font-weight: 700;
            color: #64748b;
            letter-spacing: 0.5px;
        }

        .metric-value {
            font-size: 2.25rem;
            font-weight: 700;
            color: #0f172a;
            margin: 4px 0;
        }

        .metric-subtext {
            font-size: 0.825rem;
            font-weight: 600;
        }

        .metric-subtext.warning { color: #d97706; }
        .metric-subtext.success { color: #16a34a; }
        .metric-subtext.info { color: #2563eb; }
        .metric-subtext.muted { color: #64748b; }

        /* Hub Spotlight Card */
        .hub-hero-card {
            background: #ffffff;
            border: 1px solid #cbd5e1;
            border-radius: 12px;
            padding: 32px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 24px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
        }

        .hero-main {
            display: flex;
            align-items: center;
            gap: 20px;
        }

        .hero-icon {
            font-size: 2.5rem;
            background: #eff6ff;
            padding: 16px;
            border-radius: 12px;
            line-height: 1;
        }

        .hero-details h2 {
            margin: 0 0 6px 0;
            font-size: 1.35rem;
            font-weight: 700;
            color: #0f172a;
        }

        .hero-details p {
            margin: 0;
            color: #64748b;
            font-size: 0.95rem;
            line-height: 1.5;
            max-width: 600px;
        }

        .hero-actions {
            flex-shrink: 0;
        }

        .btn-hero {
            display: inline-block;
            background-color: #2563eb;
            color: #ffffff !important;
            padding: 12px 24px;
            font-size: 0.95rem;
            font-weight: 600;
            border-radius: 8px;
            text-decoration: none !important;
            transition: background-color 0.15s ease, transform 0.15s ease;
        }

        .btn-hero:hover {
            background-color: #1d4ed8;
            transform: translateY(-1px);
        }
    </style>

    <div class="portal-container">
        <!-- Header Banner -->
        <div class="portal-header">
            <div class="header-content">
                <span class="badge-role">Staff Operations Hub</span>
                <h1>Welcome back, <asp:Literal ID="litStaffName" runat="server">Shawn</asp:Literal>!</h1>
                <p>View operational schedules, manage site timelines, and handle team allocations.</p>
            </div>
            <div class="header-meta">
                <span class="user-level-badge">User Level: <asp:Literal ID="litUserLevel" runat="server">6</asp:Literal></span>
            </div>
        </div>

        <!-- Metric Summary Cards -->
        <div class="metrics-grid">
            <div class="metric-card">
                <div class="metric-title">PENDING REQUESTS</div>
                <div class="metric-value">5</div>
                <div class="metric-subtext warning">Needs quotation</div>
            </div>
            <div class="metric-card">
                <div class="metric-title">SCHEDULED JOBS</div>
                <div class="metric-value">3</div>
                <div class="metric-subtext success">Active in field</div>
            </div>
            <div class="metric-card">
                <div class="metric-title">QUOTES AWAITING APPROVAL</div>
                <div class="metric-value">2</div>
                <div class="metric-subtext info">Sent to clients</div>
            </div>
            <div class="metric-card">
                <div class="metric-title">TOTAL ACTIVE CLIENTS</div>
                <div class="metric-value">14</div>
                <div class="metric-subtext muted">Registered accounts</div>
            </div>
        </div>

        <!-- Primary Focus Section: Operational Hub -->
        <div class="hub-hero-card">
            <div class="hero-main">
                <div class="hero-icon">📅</div>
                <div class="hero-details">
                    <h2>Schedule & Team Allocations</h2>
                    <p>Access active felling schedules, assign field crews, track team allocations, and oversee daily operational timelines across sites.</p>
                </div>
            </div>
            <div class="hero-actions">
                <asp:HyperLink ID="lnkCalendar" runat="server" NavigateUrl="~/Calendar.aspx" CssClass="btn-hero">
                    Open Operational Calendar &rarr;
                </asp:HyperLink>
            </div>
        </div>
    </div>
</asp:Content>