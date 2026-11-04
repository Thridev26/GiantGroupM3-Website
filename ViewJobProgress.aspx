
<%@ Page Title="View Job Progress" Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="ViewJobProgress.aspx.cs"
    Inherits="GiantGroupM3.ViewJobProgress" %>

<asp:Content ID="Content1"
    ContentPlaceHolderID="MainContent"
    runat="server">

<style>
    .vjp-page {
        max-width: 1140px;
        margin: 0 auto;
        padding: .5rem 0 2rem;
    }

    .vjp-banner {
        padding: 2rem 2.25rem;
        margin-bottom: 1.5rem;
    }

    .vjp-banner h1 {
        font-size: clamp(1.7rem, 3vw, 2.3rem);
        margin: .6rem 0;
        color: #fff;
    }

    .vjp-banner p,
    .vjp-breadcrumb {
        color: #cfe5d9;
    }

    .vjp-breadcrumb {
        font-size: .83rem;
        font-weight: 600;
    }

    .vjp-stat-value {
        font-family: var(--gg-display);
        font-size: 2.4rem;
        font-weight: 700;
        color: var(--gg-text);
        margin: .7rem 0 .3rem;
    }

    .vjp-stat-note,
    .vjp-description {
        color: var(--gg-muted);
        font-size: .85rem;
        line-height: 1.6;
    }

    .vjp-heading {
        display: flex;
        align-items: center;
        gap: .75rem;
        margin-bottom: 1.25rem;
    }

    .vjp-heading h2 {
        font-size: 1.1rem;
        font-weight: 700;
        color: var(--gg-text);
        margin: 0;
    }

    .vjp-filters {
        display: grid;
        grid-template-columns: 2fr 1fr 1fr 1fr;
        gap: 1rem;
    }

    .vjp-actions {
        display: flex;
        gap: .75rem;
        flex-wrap: wrap;
        margin-top: 1.2rem;
    }

    .vjp-record-count {
        color: var(--gg-muted);
        font-size: .85rem;
        font-weight: 600;
    }

    .vjp-status {
        display: inline-block;
        padding: .3rem .65rem;
        border-radius: 999px;
        font-size: .78rem;
        font-weight: 700;
        white-space: nowrap;
    }

    .vjp-pending {
        background: #fff0cf;
        color: #805700;
    }

    .vjp-active {
        background: #dcecff;
        color: #145da0;
    }

    .vjp-completed {
        background: #d9f1e1;
        color: #17663a;
    }

    .vjp-archived {
        background: #e9e9e9;
        color: #555;
    }

    .vjp-detail-box {
        border: 1px solid var(--gg-border);
        background: var(--gg-surface-2);
        border-radius: 12px;
        padding: 1.25rem;
    }

    .vjp-detail-row {
        display: flex;
        justify-content: space-between;
        align-items: start;
        gap: 1rem;
        padding: .85rem 0;
        border-bottom: 1px solid var(--gg-border);
        font-size: .88rem;
    }

    .vjp-detail-row:last-child {
        border-bottom: 0;
    }

    .vjp-detail-row span {
        color: var(--gg-muted);
    }

    .vjp-detail-row strong {
        color: var(--gg-text);
        text-align: right;
        overflow-wrap: anywhere;
    }

    .vjp-track {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: .75rem;
        margin: 1.5rem 0;
    }

    .vjp-stage {
        text-align: center;
        padding: 1.3rem .5rem;
        border: 2px solid var(--gg-border);
        background: var(--gg-surface-2);
        border-radius: 12px;
        color: var(--gg-muted);
    }

    .vjp-stage i {
        display: block;
        font-size: 1.55rem;
        margin-bottom: .65rem;
    }

    .vjp-stage strong {
        display: block;
        font-size: .85rem;
    }

    .vjp-stage small {
        display: block;
        font-size: .73rem;
        margin-top: .3rem;
    }

    .vjp-stage.is-current {
        background: var(--gg-accent-soft);
        border-color: var(--gg-600);
        color: var(--gg-accent);
    }

    .vjp-stage.is-done {
        border-color: var(--gg-600);
        color: var(--gg-accent);
    }

    .vjp-evidence {
        background: var(--gg-surface-2);
        border: 1px dashed var(--gg-border);
        border-radius: 12px;
        padding: 1.2rem;
        margin-top: 1rem;
    }

    .vjp-table td,
    .vjp-table th {
        vertical-align: middle;
    }

    .vjp-table td {
        font-size: .86rem;
    }

    .vjp-table a {
        color: var(--gg-accent);
        font-weight: 700;
    }

    .vjp-empty {
        text-align: center;
        padding: 2rem;
        color: var(--gg-muted);
    }

    @media(max-width: 991px) {
        .vjp-filters {
            grid-template-columns: repeat(2, 1fr);
        }
    }

    @media(max-width: 575px) {
        .vjp-banner {
            padding: 1.5rem;
        }

        .vjp-filters {
            grid-template-columns: 1fr;
        }

        .vjp-actions {
            flex-direction: column;
        }

        .vjp-actions .gg-btn {
            width: 100%;
        }

        .vjp-stage {
            padding: 1rem .35rem;
        }

        .vjp-stage strong {
            font-size: .72rem;
        }
    }
</style>

<div class="vjp-page">

    <!-- HEADER -->
    <div class="gg-banner vjp-banner">
        <div class="vjp-breadcrumb">
            <i class="bi bi-grid me-1"></i>
            Management / Jobs / Progress
        </div>

        <h1>
            <i class="bi bi-graph-up-arrow me-2"></i>
            View Job Progress
        </h1>

        <p>
            Track scheduled jobs, monitor operational
            progress and review completed work.
        </p>
    </div>

    <!-- ERRORS -->
    <asp:Panel ID="pnlError"
        runat="server"
        Visible="false"
        CssClass="gg-msg is-error mb-4">

        <asp:Label
            ID="lblError"
            runat="server" />

    </asp:Panel>

    <!-- SUMMARY CARDS -->
    <div class="row g-3 mb-4">

        <div class="col-sm-6 col-xl-3">
            <div class="gg-kpi gg-surface h-100">

                <span class="gg-kpi-label">
                    Total Jobs
                </span>

                <div class="vjp-stat-value">
                    <asp:Label
                        ID="lblTotalJobs"
                        runat="server"
                        Text="0" />
                </div>

                <div class="vjp-stat-note">
                    All recorded jobs
                </div>

            </div>
        </div>

        <div class="col-sm-6 col-xl-3">
            <div class="gg-kpi gg-surface is-amber h-100">

                <span class="gg-kpi-label">
                    Not Started
                </span>

                <div class="vjp-stat-value">
                    <asp:Label
                        ID="lblNotStarted"
                        runat="server"
                        Text="0" />
                </div>

                <div class="vjp-stat-note">
                    Scheduled and awaiting start
                </div>

            </div>
        </div>

        <div class="col-sm-6 col-xl-3">
            <div class="gg-kpi gg-surface is-blue h-100">

                <span class="gg-kpi-label">
                    In Progress
                </span>

                <div class="vjp-stat-value">
                    <asp:Label
                        ID="lblInProgress"
                        runat="server"
                        Text="0" />
                </div>

                <div class="vjp-stat-note">
                    Currently being worked on
                </div>

            </div>
        </div>

        <div class="col-sm-6 col-xl-3">
            <div class="gg-kpi gg-surface h-100">

                <span class="gg-kpi-label">
                    Completed
                </span>

                <div class="vjp-stat-value">
                    <asp:Label
                        ID="lblCompleted"
                        runat="server"
                        Text="0" />
                </div>

                <div class="vjp-stat-note">
                    Finished jobs
                </div>

            </div>
        </div>

    </div>

    <!-- SEARCH AND FILTER -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="vjp-heading">
                <span class="gg-tile">
                    <i class="bi bi-funnel"></i>
                </span>

                <h2>Search &amp; Filter Jobs</h2>
            </div>

            <p class="vjp-description mb-4">
                Search by job number, client or site.
                Filter by job status or scheduled date.
            </p>

            <div class="vjp-filters">

                <div>
                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtSearchJobs"
                        CssClass="gg-field-label"
                        Text="Search Jobs" />

                    <asp:TextBox
                        ID="txtSearchJobs"
                        runat="server"
                        CssClass="gg-input"
                        MaxLength="100"
                        placeholder="Job ID, client, site..." />
                </div>

                <div>
                    <asp:Label
                        runat="server"
                        AssociatedControlID="ddlStatusFilter"
                        CssClass="gg-field-label"
                        Text="Job Status" />

                    <asp:DropDownList
                        ID="ddlStatusFilter"
                        runat="server"
                        CssClass="gg-select">

                        <asp:ListItem Text="All Statuses"
                            Value="All" />

                        <asp:ListItem Text="Not Started"
                            Value="Not Started" />

                        <asp:ListItem Text="In Progress"
                            Value="In Progress" />

                        <asp:ListItem Text="Completed"
                            Value="Completed" />

                        <asp:ListItem Text="Archived"
                            Value="Archived" />

                    </asp:DropDownList>
                </div>

                <div>
                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtFromDate"
                        CssClass="gg-field-label"
                        Text="From Date" />

                    <asp:TextBox
                        ID="txtFromDate"
                        runat="server"
                        TextMode="Date"
                        CssClass="gg-input" />
                </div>

                <div>
                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtToDate"
                        CssClass="gg-field-label"
                        Text="To Date" />

                    <asp:TextBox
                        ID="txtToDate"
                        runat="server"
                        TextMode="Date"
                        CssClass="gg-input" />
                </div>

            </div>

            <div class="vjp-actions">
                <asp:Button
                    ID="btnApplyFilters"
                    runat="server"
                    Text="Apply Filters"
                    CssClass="gg-btn gg-btn-primary"
                    CausesValidation="false"
                    OnClick="btnApplyFilters_Click" />

                <asp:Button
                    ID="btnClearFilters"
                    runat="server"
                    Text="Clear Filters"
                    CssClass="gg-btn gg-btn-outline"
                    CausesValidation="false"
                    OnClick="btnClearFilters_Click" />
            </div>

        </div>
    </div>

    <!-- JOB TABLE -->
    <div class="gg-panel mb-4">

        <div class="gg-panel-body">

            <div class="d-flex justify-content-between
                        align-items-center flex-wrap gap-3 mb-3">

                <div>
                    <span class="gg-label">
                        <i class="bi bi-list-task"></i>
                        Job Records
                    </span>

                    <h2 class="gg-section-title mt-2">
                        All Jobs
                    </h2>
                </div>

                <div class="vjp-record-count">
                    <i class="bi bi-database me-1"></i>

                    <asp:Label
                        ID="lblRecordCount"
                        runat="server"
                        Text="0 records" />
                </div>

            </div>

            <div class="d-flex flex-wrap gap-2 mb-3">
                <span class="vjp-status vjp-pending">
                    Not Started
                </span>

                <span class="vjp-status vjp-active">
                    In Progress
                </span>

                <span class="vjp-status vjp-completed">
                    Completed
                </span>

                <span class="vjp-status vjp-archived">
                    Archived
                </span>
            </div>

        </div>

        <div class="gg-table-wrap">

            <asp:GridView
                ID="gvJobs"
                runat="server"
                AutoGenerateColumns="false"
                DataKeyNames="jobID"
                CssClass="table gg-table vjp-table"
                GridLines="None"
                AllowPaging="true"
                PageSize="10"
                EmptyDataText="No jobs match the selected filters."
                OnSelectedIndexChanged="gvJobs_SelectedIndexChanged"
                OnPageIndexChanging="gvJobs_PageIndexChanging">

                <Columns>

                    <asp:BoundField
                        DataField="jobID"
                        HeaderText="Job ID" />

                    <asp:BoundField
                        DataField="ClientName"
                        HeaderText="Client Name" />

                    <asp:BoundField
                        DataField="siteAddress"
                        HeaderText="Site Address" />

                    <asp:BoundField
                        DataField="ScheduledDateDisplay"
                        HeaderText="Start Date" />

                    <asp:BoundField
                        DataField="TimeSlots"
                        HeaderText="Time Slots" />

                    <asp:TemplateField
                        HeaderText="Job Status">

                        <ItemTemplate>
                            <span class='<%# StatusCss(Eval("jobStatus")) %>'>
                                <%#: Eval("jobStatus") %>
                            </span>
                        </ItemTemplate>

                    </asp:TemplateField>

                    <asp:CommandField
                        ShowSelectButton="true"
                        SelectText="View Details"
                        HeaderText="Action" />

                </Columns>

                <PagerStyle CssClass="gg-pager" />

            </asp:GridView>

        </div>
    </div>

    <!-- EMPTY SELECTION -->
    <asp:Panel ID="pnlNoSelection"
        runat="server"
        CssClass="gg-panel mb-4">

        <div class="gg-panel-body vjp-empty">

            <i class="bi bi-clipboard-data fs-2"></i>

            <h3 class="mt-3">
                No Job Selected
            </h3>

            <p>
                Select View Details on any job
                to inspect its progress.
            </p>

        </div>

    </asp:Panel>

    <!-- SELECTED JOB DETAILS -->
    <asp:Panel ID="pnlJobDetails"
        runat="server"
        Visible="false"
        CssClass="gg-panel mb-4">

        <div class="gg-panel-body">

            <div class="vjp-heading">
                <span class="gg-tile">
                    <i class="bi bi-clipboard-data"></i>
                </span>

                <h2>Selected Job Details</h2>
            </div>

            <div class="row g-4">

                <!-- LEFT COLUMN -->
                <div class="col-lg-5">

                    <div class="vjp-detail-box">

                        <h3 class="gg-section-title mb-3">
                            Job Information
                        </h3>

                        <div class="vjp-detail-row">
                            <span>Job ID</span>
                            <strong>
                                <asp:Label ID="lblJobID"
                                    runat="server" />
                            </strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Client</span>
                            <strong>
                                <asp:Label ID="lblClientName"
                                    runat="server" />
                            </strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Quotation ID</span>
                            <strong>
                                <asp:Label ID="lblQuoteID"
                                    runat="server" />
                            </strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Site Address</span>
                            <strong>
                                <asp:Label ID="lblSiteAddress"
                                    runat="server" />
                            </strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Scheduled Date</span>
                            <strong>
                                <asp:Label ID="lblStartDate"
                                    runat="server" />
                            </strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Time Slots</span>
                            <strong>
                                <asp:Label ID="lblTimeSlots"
                                    runat="server" />
                            </strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Fuel / Travel Cost</span>
                            <strong>
                                <asp:Label ID="lblFuelCost"
                                    runat="server" />
                            </strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Dumping Cost</span>
                            <strong>
                                <asp:Label ID="lblDumpingCost"
                                    runat="server" />
                            </strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Current Status</span>
                            <strong>
                                <asp:Label ID="lblCurrentStatus"
                                    runat="server" />
                            </strong>
                        </div>

                    </div>

                </div>

                <!-- RIGHT COLUMN -->
                <div class="col-lg-7">

                    <div class="vjp-detail-box h-100">

                        <h3 class="gg-section-title mb-2">
                            Job Progress Tracker
                        </h3>

                        <p class="vjp-description">
                            Current operational stage
                            of the selected job.
                        </p>

                        <div class="vjp-track">

                            <asp:Panel
                                ID="pnlStageNotStarted"
                                runat="server"
                                CssClass="vjp-stage">

                                <i class="bi bi-calendar-check"></i>
                                <strong>Not Started</strong>
                                <small>Scheduled</small>

                            </asp:Panel>

                            <asp:Panel
                                ID="pnlStageInProgress"
                                runat="server"
                                CssClass="vjp-stage">

                                <i class="bi bi-gear"></i>
                                <strong>In Progress</strong>
                                <small>Work underway</small>

                            </asp:Panel>

                            <asp:Panel
                                ID="pnlStageCompleted"
                                runat="server"
                                CssClass="vjp-stage">

                                <i class="bi bi-check-circle"></i>
                                <strong>Completed</strong>
                                <small>Work finished</small>

                            </asp:Panel>

                        </div>

                        <p class="vjp-description">
                            <asp:Label
                                ID="lblProgressNote"
                                runat="server" />
                        </p>

                        <!-- COMPLETION EVIDENCE -->
                        <div class="vjp-evidence">

                            <h4 class="gg-section-title mb-3">
                                <i class="bi bi-camera me-2"></i>
                                Completion Evidence
                            </h4>

                            <div class="vjp-detail-row">
                                <span>Saved Completion Photos</span>
                                <strong>
                                    <asp:Label
                                        ID="lblPhotoCount"
                                        runat="server" />
                                </strong>
                            </div>

                            <div class="vjp-detail-row">
                                <span>Latest Photo Upload</span>
                                <strong>
                                    <asp:Label
                                        ID="lblPhotoDate"
                                        runat="server" />
                                </strong>
                            </div>

                        </div>

                    </div>

                </div>

            </div>

            <!-- ACTIONS -->
            <div class="vjp-actions mt-4">

                <asp:HyperLink
                    ID="lnkEditProgress"
                    runat="server"
                    CssClass="gg-btn gg-btn-primary"
                    Visible="false">

                    <i class="bi bi-pencil-square me-1"></i>
                    Edit Job Progress

                </asp:HyperLink>

                <a runat="server"
                   href="~/ManagementPortal.aspx"
                   class="gg-btn gg-btn-outline">

                    <i class="bi bi-arrow-left me-1"></i>
                    Back to Management

                </a>

            </div>

        </div>

    </asp:Panel>

</div>

</asp:Content>
