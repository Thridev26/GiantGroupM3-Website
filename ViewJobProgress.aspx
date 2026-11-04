
<%@ Page Title="View Job Progress" Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="ViewJobProgress.aspx.cs"
    Inherits="GiantGroupM3.ViewJobProgress" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

<style>
    /* VIEW JOB PROGRESS - GIANT GROUP THEME */

    .vjp-wrap {
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

    .vjp-banner p {
        max-width: 650px;
        margin: 0;
        color: #cfe5d9;
    }

    .vjp-breadcrumb {
        color: #cfe5d9;
        font-size: .83rem;
        font-weight: 600;
    }

    /* Summary cards */

    .vjp-stat-icon {
        font-size: 1.25rem;
    }

    .vjp-stat-value {
        font-family: var(--gg-display);
        font-size: 2.4rem;
        font-weight: 600;
        line-height: 1.2;
        color: var(--gg-text);
        margin: .7rem 0 .3rem;
    }

    .vjp-stat-note {
        font-size: .8rem;
        color: var(--gg-muted);
    }

    /* Section headings */

    .vjp-section-heading {
        display: flex;
        align-items: center;
        gap: .75rem;
        margin-bottom: 1.25rem;
    }

    .vjp-section-heading h2 {
        font-size: 1.08rem;
        font-weight: 700;
        color: var(--gg-text);
        margin: 0;
    }

    .vjp-description {
        color: var(--gg-muted);
        font-size: .88rem;
        line-height: 1.6;
    }

    /* Search and filters */

    .vjp-filters {
        display: grid;
        grid-template-columns: 2fr 1fr 1fr 1fr;
        gap: 1rem;
        align-items: end;
    }

    .vjp-filter-actions {
        display: flex;
        gap: .7rem;
        margin-top: 1.2rem;
    }

    .vjp-filter-control:disabled {
        opacity: .7;
        cursor: not-allowed;
    }

    /* Table */

    .vjp-table-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        flex-wrap: wrap;
        margin-bottom: 1rem;
    }

    .vjp-table-head h2 {
        font-family: var(--gg-display);
        font-size: 1.4rem;
        font-weight: 600;
        color: var(--gg-text);
        margin: 0;
    }

    .vjp-record-count {
        display: inline-flex;
        align-items: center;
        gap: .4rem;
        border: 1px solid var(--gg-border);
        background: var(--gg-surface-2);
        color: var(--gg-muted);
        border-radius: 999px;
        padding: .35rem .75rem;
        font-size: .78rem;
        font-weight: 600;
    }

    .vjp-table-empty {
        text-align: center;
        padding: 3rem 1.25rem !important;
        color: var(--gg-muted);
    }

    .vjp-table-empty h3 {
        font-size: 1.15rem;
        font-weight: 700;
        color: var(--gg-text);
        margin: 1rem 0 .5rem;
    }

    .vjp-table-empty p {
        color: var(--gg-muted);
        font-size: .88rem;
        margin: 0;
    }

    /* Status legend */

    .vjp-status-legend {
        display: flex;
        gap: .6rem;
        flex-wrap: wrap;
    }

    /* Job details */

    .vjp-details {
        background: var(--gg-surface-2);
        border: 1px solid var(--gg-border);
        border-radius: 12px;
        padding: 1.2rem;
    }

    .vjp-detail-row {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        padding: .8rem 0;
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
        font-weight: 600;
    }

    /* Progress tracker */

    .vjp-timeline {
        display: flex;
        justify-content: space-between;
        gap: .75rem;
        position: relative;
        margin: 2rem 0;
    }

    .vjp-timeline::before {
        content: "";
        position: absolute;
        top: 20px;
        left: 15%;
        right: 15%;
        height: 2px;
        background: var(--gg-border);
    }

    .vjp-stage {
        flex: 1;
        text-align: center;
        position: relative;
        z-index: 1;
    }

    .vjp-stage-circle {
        width: 42px;
        height: 42px;
        border-radius: 50%;
        background: var(--gg-surface-2);
        border: 2px solid var(--gg-border);
        color: var(--gg-muted);
        display: flex;
        align-items: center;
        justify-content: center;
        margin: 0 auto .8rem;
        font-size: 1rem;
    }

    .vjp-stage-title {
        display: block;
        font-weight: 700;
        font-size: .85rem;
        color: var(--gg-text);
    }

    .vjp-stage-sub {
        display: block;
        font-size: .75rem;
        color: var(--gg-muted);
        margin-top: .3rem;
    }

    .vjp-no-selection {
        text-align: center;
        padding: 1.2rem;
        background: var(--gg-surface-2);
        border: 1px dashed var(--gg-border);
        border-radius: 12px;
        color: var(--gg-muted);
        font-size: .86rem;
    }

    .vjp-bottom-actions {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: .75rem;
        padding-top: 1.3rem;
        border-top: 1px solid var(--gg-border);
        margin-top: 1.5rem;
    }

    @media (max-width: 991px) {
        .vjp-filters {
            grid-template-columns: repeat(2, 1fr);
        }
    }

    @media (max-width: 575px) {
        .vjp-banner {
            padding: 1.5rem;
        }

        .vjp-filters {
            grid-template-columns: 1fr;
        }

        .vjp-filter-actions {
            flex-direction: column;
        }

        .vjp-filter-actions .gg-btn {
            width: 100%;
        }

        .vjp-stage-title {
            font-size: .74rem;
        }

        .vjp-stage-sub {
            font-size: .68rem;
        }
    }
</style>

<div class="vjp-wrap">

    <!-- ================= HEADER ================= -->

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
            Track scheduled jobs, monitor operational progress,
            and review job completion across all active sites.
        </p>

    </div>

    <!-- ================= PREVIEW NOTICE ================= -->

    <div class="gg-msg is-info mb-4">
        <i class="bi bi-info-circle me-2"></i>
        <strong>Interface Preview:</strong>
        Job records, totals and filters will become available
        once this page is connected to the database.
    </div>

    <!-- ================= SUMMARY CARDS ================= -->

    <div class="row g-3 mb-4">

        <!-- TOTAL JOBS -->
        <div class="col-sm-6 col-xl-3">
            <div class="gg-kpi gg-surface h-100">

                <div class="d-flex justify-content-between align-items-start">
                    <span class="gg-kpi-label">
                        Total Jobs
                    </span>

                    <span class="gg-tile">
                        <i class="bi bi-briefcase vjp-stat-icon"></i>
                    </span>
                </div>

                <div class="vjp-stat-value">
                    &mdash;
                </div>

                <div class="vjp-stat-note">
                    All recorded jobs
                </div>

            </div>
        </div>

        <!-- NOT STARTED -->
        <div class="col-sm-6 col-xl-3">
            <div class="gg-kpi gg-surface is-amber h-100">

                <div class="d-flex justify-content-between align-items-start">
                    <span class="gg-kpi-label">
                        Not Started
                    </span>

                    <span class="gg-tile is-amber">
                        <i class="bi bi-calendar-event vjp-stat-icon"></i>
                    </span>
                </div>

                <div class="vjp-stat-value">
                    &mdash;
                </div>

                <div class="vjp-stat-note">
                    Scheduled and awaiting start
                </div>

            </div>
        </div>

        <!-- IN PROGRESS -->
        <div class="col-sm-6 col-xl-3">
            <div class="gg-kpi gg-surface is-blue h-100">

                <div class="d-flex justify-content-between align-items-start">
                    <span class="gg-kpi-label">
                        In Progress
                    </span>

                    <span class="gg-tile is-blue">
                        <i class="bi bi-tools vjp-stat-icon"></i>
                    </span>
                </div>

                <div class="vjp-stat-value">
                    &mdash;
                </div>

                <div class="vjp-stat-note">
                    Currently being worked on
                </div>

            </div>
        </div>

        <!-- COMPLETED -->
        <div class="col-sm-6 col-xl-3">
            <div class="gg-kpi gg-surface h-100">

                <div class="d-flex justify-content-between align-items-start">
                    <span class="gg-kpi-label">
                        Completed
                    </span>

                    <span class="gg-tile">
                        <i class="bi bi-check-circle vjp-stat-icon"></i>
                    </span>
                </div>

                <div class="vjp-stat-value">
                    &mdash;
                </div>

                <div class="vjp-stat-note">
                    Successfully completed jobs
                </div>

            </div>
        </div>

    </div>

    <!-- ================= SEARCH AND FILTERS ================= -->

    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="vjp-section-heading">
                <span class="gg-tile">
                    <i class="bi bi-funnel"></i>
                </span>

                <h2>Search &amp; Filter Jobs</h2>
            </div>

            <p class="vjp-description mb-4">
                Find specific jobs using their ID, client,
                site location, status or scheduled date.
            </p>

            <div class="vjp-filters">

                <!-- SEARCH -->
                <div>
                    <label for="vjpSearch" class="gg-field-label">
                        Search Jobs
                    </label>

                    <div class="gg-search-box" style="max-width:none;">
                        <i class="bi bi-search"></i>

                        <input
                            id="vjpSearch"
                            type="search"
                            class="gg-input vjp-filter-control"
                            placeholder="Job ID, client or address..."
                            disabled="disabled" />
                    </div>
                </div>

                <!-- STATUS FILTER -->
                <div>
                    <label for="vjpStatus" class="gg-field-label">
                        Job Status
                    </label>

                    <select
                        id="vjpStatus"
                        class="gg-select vjp-filter-control"
                        disabled="disabled">

                        <option value="">All Statuses</option>
                        <option value="Not Started">Not Started</option>
                        <option value="In Progress">In Progress</option>
                        <option value="Completed">Completed</option>
                        <option value="Archived">Archived</option>

                    </select>
                </div>

                <!-- FROM DATE -->
                <div>
                    <label for="vjpFromDate" class="gg-field-label">
                        From Date
                    </label>

                    <input
                        id="vjpFromDate"
                        type="date"
                        class="gg-input vjp-filter-control"
                        disabled="disabled" />
                </div>

                <!-- TO DATE -->
                <div>
                    <label for="vjpToDate" class="gg-field-label">
                        To Date
                    </label>

                    <input
                        id="vjpToDate"
                        type="date"
                        class="gg-input vjp-filter-control"
                        disabled="disabled" />
                </div>

            </div>

            <div class="vjp-filter-actions">

                <button
                    type="button"
                    class="gg-btn gg-btn-primary"
                    disabled="disabled">
                    <i class="bi bi-search"></i>
                    Apply Filters
                </button>

                <button
                    type="button"
                    class="gg-btn gg-btn-outline"
                    disabled="disabled">
                    <i class="bi bi-arrow-counterclockwise"></i>
                    Clear Filters
                </button>

            </div>

        </div>
    </div>

    <!-- ================= JOB RECORDS TABLE ================= -->

    <div class="vjp-table-head">

        <div>
            <span class="gg-label mb-2">
                <i class="bi bi-list-task"></i>
                Job Records
            </span>

            <h2>All Jobs</h2>

            <p class="vjp-description mt-1 mb-0">
                View the current progress of recorded jobs.
            </p>
        </div>

        <div class="vjp-record-count">
            <i class="bi bi-database"></i>
            Records not loaded
        </div>

    </div>

    <!-- STATUS LEGEND -->
    <div class="vjp-status-legend mb-3">

        <span class="gg-chip is-pending">
            Not Started
        </span>

        <span class="gg-chip is-active">
            In Progress
        </span>

        <span class="gg-chip is-done">
            Completed
        </span>

        <span class="gg-chip">
            Archived
        </span>

    </div>

    <div class="gg-panel mb-4">

        <div class="gg-table-wrap">
            <table class="table gg-table">

                <thead>
                    <tr>
                        <th>Job ID</th>
                        <th>Client Name</th>
                        <th>Site Address</th>
                        <th>Start Date</th>
                        <th>Time Slots</th>
                        <th>Job Status</th>
                        <th>Action</th>
                    </tr>
                </thead>

                <tbody>

                    <tr>
                        <td colspan="7" class="vjp-table-empty">

                            <span class="gg-tile is-lg">
                                <i class="bi bi-inbox"></i>
                            </span>

                            <h3>
                                No Job Records Loaded
                            </h3>

                            <p>
                                Your jobs will appear here once
                                the page is connected to the
                                SQL Server database.
                            </p>

                        </td>
                    </tr>

                </tbody>

            </table>
        </div>

    </div>

    <!-- ================= JOB DETAILS ================= -->

    <div class="gg-panel mb-4">

        <div class="gg-panel-body">

            <div class="vjp-section-heading">

                <span class="gg-tile">
                    <i class="bi bi-clipboard-data"></i>
                </span>

                <h2>Selected Job Details</h2>

            </div>

            <p class="vjp-description mb-4">
                Select a job from the records table to
                view its scheduling information and
                current operational status.
            </p>

            <div class="row g-4">

                <!-- LEFT: JOB INFORMATION -->
                <div class="col-lg-5">

                    <div class="vjp-details">

                        <h3 class="gg-section-title mb-3">
                            Job Information
                        </h3>

                        <div class="vjp-detail-row">
                            <span>Job ID</span>
                            <strong>Not selected</strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Client</span>
                            <strong>Not selected</strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Quotation ID</span>
                            <strong>&mdash;</strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Site Address</span>
                            <strong>&mdash;</strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Scheduled Date</span>
                            <strong>&mdash;</strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Time Slots</span>
                            <strong>&mdash;</strong>
                        </div>

                        <div class="vjp-detail-row">
                            <span>Current Status</span>
                            <strong>&mdash;</strong>
                        </div>

                    </div>

                </div>

                <!-- RIGHT: PROGRESS TRACKER -->
                <div class="col-lg-7">

                    <div class="vjp-details h-100">

                        <h3 class="gg-section-title mb-2">
                            Job Progress Tracker
                        </h3>

                        <p class="vjp-description">
                            Follow the job through each stage
                            of the operational workflow.
                        </p>

                        <div class="vjp-timeline">

                            <!-- STAGE 1 -->
                            <div class="vjp-stage">

                                <div class="vjp-stage-circle">
                                    <i class="bi bi-calendar-check"></i>
                                </div>

                                <span class="vjp-stage-title">
                                    Not Started
                                </span>

                                <span class="vjp-stage-sub">
                                    Job scheduled
                                </span>

                            </div>

                            <!-- STAGE 2 -->
                            <div class="vjp-stage">

                                <div class="vjp-stage-circle">
                                    <i class="bi bi-gear"></i>
                                </div>

                                <span class="vjp-stage-title">
                                    In Progress
                                </span>

                                <span class="vjp-stage-sub">
                                    Work underway
                                </span>

                            </div>

                            <!-- STAGE 3 -->
                            <div class="vjp-stage">

                                <div class="vjp-stage-circle">
                                    <i class="bi bi-check-lg"></i>
                                </div>

                                <span class="vjp-stage-title">
                                    Completed
                                </span>

                                <span class="vjp-stage-sub">
                                    Work finished
                                </span>

                            </div>

                        </div>

                        <div class="vjp-no-selection">
                            <i class="bi bi-info-circle me-2"></i>
                            Select a job to view its current
                            progress and completion information.
                        </div>

                    </div>

                </div>

            </div>

            <!-- ACTION BUTTONS -->
            <div class="vjp-bottom-actions">

                <button
                    type="button"
                    class="gg-btn gg-btn-primary"
                    disabled="disabled">
                    <i class="bi bi-eye"></i>
                    View Full Details
                </button>

                <button
                    type="button"
                    class="gg-btn gg-btn-outline"
                    disabled="disabled">
                    <i class="bi bi-pencil-square"></i>
                    Edit Job Progress
                </button>

                <a
                    runat="server"
                    href="~/ManagementPortal.aspx"
                    class="gg-btn gg-btn-outline">
                    <i class="bi bi-arrow-left"></i>
                    Back to Management
                </a>

            </div>

        </div>

    </div>

</div>

</asp:Content>
