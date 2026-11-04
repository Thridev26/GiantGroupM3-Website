
<%@ Page Title="Manage Jobs" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Jobs_ManageJob.aspx.cs"
    Inherits="GiantGroupM3.Jobs_ManageJob" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <style>
        /* Jobs page - uses the existing Giant Group theme */
        .jm-page {
            max-width: 1140px;
            margin: 0 auto;
            padding: .5rem 0 2rem;
        }

        .jm-banner {
            padding: 2rem 2.25rem;
            margin-bottom: 1.5rem;
        }

        .jm-banner h1 {
            font-size: clamp(1.7rem, 3vw, 2.3rem);
            margin: .65rem 0 .5rem;
        }

        .jm-banner p {
            max-width: 660px;
            margin-bottom: 0;
        }

        .jm-breadcrumb {
            color: #cfe5d9;
            font-size: .82rem;
            font-weight: 600;
        }

        .jm-card-note {
            color: var(--gg-muted);
            font-size: .85rem;
            line-height: 1.6;
            margin-bottom: 1.2rem;
        }

        .jm-help {
            color: var(--gg-muted);
            font-size: .78rem;
            display: block;
            margin-top: .35rem;
        }

        .jm-panel-heading {
            display: flex;
            align-items: center;
            gap: .75rem;
            padding-bottom: 1rem;
            margin-bottom: 1.25rem;
            border-bottom: 1px solid var(--gg-border);
        }

        .jm-panel-heading h2 {
            font-size: 1.08rem;
            font-weight: 700;
            margin: 0;
            color: var(--gg-text);
        }

        .jm-slot-area {
            padding: 1rem;
            border: 1px dashed var(--gg-border);
            border-radius: 12px;
            background: var(--gg-surface-2);
        }

        .jm-slot-empty {
            text-align: center;
            padding: 1.2rem .5rem;
            color: var(--gg-muted);
            font-size: .85rem;
        }

        .jm-slot-empty i {
            display: block;
            font-size: 1.6rem;
            color: var(--gg-accent);
            margin-bottom: .5rem;
        }

        .jm-slot-list {
            display: flex;
            flex-wrap: wrap;
            gap: .65rem;
            padding: 0;
            margin: 0;
            list-style: none;
        }

        .jm-slot-list li {
            display: inline-flex;
            align-items: center;
            gap: .4rem;
            padding: .6rem .85rem;
            background: var(--gg-surface);
            border: 1px solid var(--gg-border);
            border-radius: 10px;
        }

        .jm-slot-list input {
            accent-color: var(--gg-700);
        }

        .jm-slot-list label {
            margin: 0;
            font-size: .88rem;
            cursor: pointer;
        }

        .jm-summary {
            background: var(--gg-surface-2);
            border: 1px solid var(--gg-border);
            border-radius: 12px;
            padding: 1.25rem;
        }

        .jm-summary-title {
            font-weight: 700;
            margin-bottom: 1rem;
            color: var(--gg-text);
        }

        .jm-summary-row {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            gap: .75rem;
            padding: .75rem 0;
            border-bottom: 1px solid var(--gg-border);
            font-size: .88rem;
        }

        .jm-summary-row:last-child {
            border-bottom: 0;
        }

        .jm-summary-row span {
            color: var(--gg-muted);
        }

        .jm-summary-row strong {
            color: var(--gg-text);
            text-align: right;
        }

        .jm-actions {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: .75rem;
            padding-top: 1.25rem;
            border-top: 1px solid var(--gg-border);
        }

        .jm-section-top {
            display: flex;
            align-items: center;
            justify-content: space-between;
            flex-wrap: wrap;
            gap: 1rem;
            margin-bottom: 1rem;
        }

        .jm-preview-tag {
            display: inline-flex;
            align-items: center;
            gap: .4rem;
            border: 1px solid var(--gg-border);
            border-radius: 999px;
            padding: .35rem .7rem;
            color: var(--gg-muted);
            font-size: .75rem;
            font-weight: 600;
        }

        .jm-table-empty {
            text-align: center;
            padding: 2.5rem 1rem;
            color: var(--gg-muted);
        }

        .jm-table-empty i {
            display: block;
            font-size: 2rem;
            color: var(--gg-accent);
            margin-bottom: .7rem;
        }

        .jm-table-empty strong {
            display: block;
            color: var(--gg-text);
            margin-bottom: .3rem;
        }

        .jm-readonly {
            background: var(--gg-surface-2) !important;
            color: var(--gg-muted) !important;
        }

        @media (max-width: 767px) {
            .jm-banner {
                padding: 1.5rem;
            }

            .jm-actions {
                flex-direction: column;
                align-items: stretch;
            }

            .jm-actions .gg-btn {
                width: 100%;
            }
        }
    </style>

    <div class="jm-page">

        <!-- PAGE HEADER -->
        <div class="gg-banner jm-banner">
            <div class="jm-breadcrumb">
                <i class="bi bi-grid me-1"></i>
                Management / Jobs
            </div>

            <h1>
                <i class="bi bi-briefcase me-2"></i>
                Add / Edit Job Details
            </h1>

            <p>
                Create jobs from accepted quotations, manage
                schedules, update job information and monitor
                operational records.
            </p>
        </div>

        <!-- DESIGN PREVIEW NOTICE -->
        <div class="gg-msg is-info mb-4">
            <i class="bi bi-info-circle me-2"></i>
            <strong>Interface Preview:</strong>
            This page layout is ready. Database loading,
            validation and saving will be connected later.
        </div>

        <!-- SECTION 1: SELECT JOB OR QUOTATION -->
        <div class="gg-panel mb-4">
            <div class="gg-panel-body">

                <div class="jm-panel-heading">
                    <span class="gg-step-n">1</span>
                    <h2>Select Quotation or Existing Job</h2>
                </div>

                <p class="jm-card-note">
                    Select an accepted quotation to create a new
                    job, or choose an existing job to edit its details.
                </p>

                <div class="row g-4">

                    <div class="col-md-6">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="ddlAcceptedQuote"
                            CssClass="gg-field-label"
                            Text="Create Job from Accepted Quotation" />

                        <asp:DropDownList
                            ID="ddlAcceptedQuote"
                            runat="server"
                            CssClass="gg-select">
                            <asp:ListItem
                                Text="-- Select accepted quotation --"
                                Value="" />
                        </asp:DropDownList>

                        <span class="jm-help">
                            Only accepted quotations without
                            an existing job will appear here.
                        </span>
                    </div>

                    <div class="col-md-6">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="ddlExistingJob"
                            CssClass="gg-field-label"
                            Text="Edit Existing Job" />

                        <asp:DropDownList
                            ID="ddlExistingJob"
                            runat="server"
                            CssClass="gg-select">
                            <asp:ListItem
                                Text="-- Select existing job --"
                                Value="" />
                        </asp:DropDownList>

                        <span class="jm-help">
                            Select a saved job to view or update
                            its information.
                        </span>
                    </div>

                </div>
            </div>
        </div>

        <!-- SECTION 2: JOB INFORMATION -->
        <div class="gg-panel mb-4">
            <div class="gg-panel-body">

                <div class="jm-panel-heading">
                    <span class="gg-step-n">2</span>
                    <h2>Job Information</h2>
                </div>

                <div class="row g-4">

                    <div class="col-md-6">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="txtJobID"
                            CssClass="gg-field-label"
                            Text="Job ID" />

                        <asp:TextBox
                            ID="txtJobID"
                            runat="server"
                            CssClass="gg-input jm-readonly"
                            ReadOnly="true"
                            placeholder="Auto-generated" />
                    </div>

                    <div class="col-md-6">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="txtQuoteID"
                            CssClass="gg-field-label"
                            Text="Quotation ID" />

                        <asp:TextBox
                            ID="txtQuoteID"
                            runat="server"
                            CssClass="gg-input jm-readonly"
                            ReadOnly="true"
                            placeholder="Selected quotation ID" />
                    </div>

                    <div class="col-md-6">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="txtClientName"
                            CssClass="gg-field-label"
                            Text="Client Name" />

                        <asp:TextBox
                            ID="txtClientName"
                            runat="server"
                            CssClass="gg-input jm-readonly"
                            ReadOnly="true"
                            placeholder="Client name will appear here" />
                    </div>

                    <div class="col-md-6">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="ddlJobStatus"
                            CssClass="gg-field-label"
                            Text="Job Status" />

                        <asp:DropDownList
                            ID="ddlJobStatus"
                            runat="server"
                            CssClass="gg-select">

                            <asp:ListItem
                                Text="Not Started"
                                Value="Not Started" />

                            <asp:ListItem
                                Text="In Progress"
                                Value="In Progress" />

                            <asp:ListItem
                                Text="Completed"
                                Value="Completed" />

                            <asp:ListItem
                                Text="Archived"
                                Value="Archived" />
                        </asp:DropDownList>
                    </div>

                    <div class="col-12">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="txtSiteAddress"
                            CssClass="gg-field-label"
                            Text="Job Site Address" />

                        <asp:TextBox
                            ID="txtSiteAddress"
                            runat="server"
                            CssClass="gg-input jm-readonly"
                            ReadOnly="true"
                            placeholder="Site address from the job request" />
                    </div>

                </div>
            </div>
        </div>

        <!-- SCHEDULE AND SUMMARY -->
        <div class="row g-4 mb-4">

            <div class="col-lg-7">

                <!-- SECTION 3: SCHEDULING -->
                <div class="gg-panel mb-4">
                    <div class="gg-panel-body">

                        <div class="jm-panel-heading">
                            <span class="gg-step-n">3</span>
                            <h2>Job Schedule</h2>
                        </div>

                        <p class="jm-card-note">
                            Choose the date and time slots for
                            this job. Availability checks will be
                            added when scheduling is connected.
                        </p>

                        <div class="mb-4">
                            <asp:Label
                                runat="server"
                                AssociatedControlID="txtStartDate"
                                CssClass="gg-field-label"
                                Text="Scheduled Start Date" />

                            <asp:TextBox
                                ID="txtStartDate"
                                runat="server"
                                TextMode="Date"
                                CssClass="gg-input" />
                        </div>

                        <div class="mb-3">
                            <span class="gg-field-label">
                                Select Time Slots
                            </span>

                            <div class="jm-slot-area">

                                <asp:CheckBoxList
                                    ID="cblTimeSlots"
                                    runat="server"
                                    RepeatLayout="UnorderedList"
                                    CssClass="jm-slot-list" />

                                <div class="jm-slot-empty">
                                    <i class="bi bi-clock-history"></i>
                                    <strong>
                                        Time slots not loaded yet
                                    </strong>
                                    <div>
                                        Available time slots will
                                        appear here once connected
                                        to the database.
                                    </div>
                                </div>

                            </div>

                            <span class="jm-help">
                                One or more time slots may be
                                selected for a scheduled job.
                            </span>
                        </div>

                        <button
                            type="button"
                            class="gg-btn gg-btn-outline gg-btn-sm"
                            disabled>
                            <i class="bi bi-calendar-check"></i>
                            Check Availability
                        </button>

                    </div>
                </div>

                <!-- SECTION 4: JOB COSTS -->
                <div class="gg-panel">
                    <div class="gg-panel-body">

                        <div class="jm-panel-heading">
                            <span class="gg-step-n">4</span>
                            <h2>Job Costs</h2>
                        </div>

                        <p class="jm-card-note">
                            Capture operational costs associated
                            with the job.
                        </p>

                        <div class="row g-3">

                            <div class="col-sm-6">
                                <asp:Label
                                    runat="server"
                                    AssociatedControlID="txtFuelCost"
                                    CssClass="gg-field-label"
                                    Text="Fuel Cost (R)" />

                                <asp:TextBox
                                    ID="txtFuelCost"
                                    runat="server"
                                    TextMode="Number"
                                    CssClass="gg-input"
                                    min="0"
                                    step="0.01"
                                    placeholder="0.00" />
                            </div>

                            <div class="col-sm-6">
                                <asp:Label
                                    runat="server"
                                    AssociatedControlID="txtDumpingCost"
                                    CssClass="gg-field-label"
                                    Text="Dumping Cost (R)" />

                                <asp:TextBox
                                    ID="txtDumpingCost"
                                    runat="server"
                                    TextMode="Number"
                                    CssClass="gg-input"
                                    min="0"
                                    step="0.01"
                                    placeholder="0.00" />
                            </div>

                        </div>

                        <span class="jm-help mt-3">
                            All costs are displayed in South
                            African Rand (ZAR).
                        </span>

                    </div>
                </div>

            </div>

            <!-- RIGHT COLUMN: JOB SUMMARY -->
            <div class="col-lg-5">

                <div class="gg-panel h-100">
                    <div class="gg-panel-body">

                        <div class="d-flex align-items-center gap-3 mb-4">
                            <span class="gg-tile is-lg">
                                <i class="bi bi-clipboard-check"></i>
                            </span>

                            <div>
                                <h2 class="gg-section-title">
                                    Job Summary
                                </h2>
                                <small class="gg-muted">
                                    Overview of selected job
                                </small>
                            </div>
                        </div>

                        <div class="jm-summary">
                            <div class="jm-summary-title">
                                <i class="bi bi-card-checklist me-2"></i>
                                Job Overview
                            </div>

                            <div class="jm-summary-row">
                                <span>Job ID</span>
                                <strong>Not assigned</strong>
                            </div>

                            <div class="jm-summary-row">
                                <span>Client</span>
                                <strong>Not selected</strong>
                            </div>

                            <div class="jm-summary-row">
                                <span>Quotation</span>
                                <strong>Not selected</strong>
                            </div>

                            <div class="jm-summary-row">
                                <span>Scheduled Date</span>
                                <strong>Not scheduled</strong>
                            </div>

                            <div class="jm-summary-row">
                                <span>Time Slots</span>
                                <strong>None selected</strong>
                            </div>

                            <div class="jm-summary-row">
                                <span>Status</span>
                                <span class="gg-chip is-pending">
                                    Not Started
                                </span>
                            </div>
                        </div>

                        <div class="gg-msg is-info mt-4 mb-0">
                            <i class="bi bi-lightbulb me-2"></i>
                            This summary will update automatically
                            after the page is connected to the backend.
                        </div>

                    </div>
                </div>

            </div>
        </div>

        <!-- SECTION 5: ACTION BUTTONS -->
        <div class="gg-panel mb-5">
            <div class="gg-panel-body">

                <div class="jm-section-top">
                    <div>
                        <h2 class="gg-section-title">
                            Manage Job
                        </h2>
                        <p class="jm-card-note mb-0">
                            Save a new job or update an
                            existing job's information.
                        </p>
                    </div>

                    <span class="jm-preview-tag">
                        <i class="bi bi-code-slash"></i>
                        Actions not connected
                    </span>
                </div>

                <div class="jm-actions">

                    <button
                        type="button"
                        class="gg-btn gg-btn-primary"
                        disabled>
                        <i class="bi bi-plus-circle"></i>
                        Create Job
                    </button>

                    <button
                        type="button"
                        class="gg-btn gg-btn-outline"
                        disabled>
                        <i class="bi bi-save"></i>
                        Update Job
                    </button>

                    <button
                        type="button"
                        class="gg-btn gg-btn-danger-outline"
                        disabled>
                        <i class="bi bi-archive"></i>
                        Archive Job
                    </button>

                </div>

            </div>
        </div>

        <!-- SECTION 6: EXISTING JOBS -->
        <div class="jm-section-top">
            <div>
                <span class="gg-label mb-2">
                    <i class="bi bi-list-task"></i>
                    Job Records
                </span>
                <h2 class="gg-section-title">
                    Existing Jobs
                </h2>
                <p class="gg-muted mt-1 mb-0">
                    Browse and manage jobs already recorded
                    in the system.
                </p>
            </div>

            <div class="gg-search-box">
                <i class="bi bi-search"></i>
                <asp:TextBox
                    ID="txtSearchJobs"
                    runat="server"
                    CssClass="gg-input"
                    placeholder="Search job ID, client or site..."
                    Enabled="false" />
            </div>
        </div>

        <div class="gg-panel">

            <div class="gg-table-wrap">
                <table class="table gg-table">
                    <thead>
                        <tr>
                            <th>Job ID</th>
                            <th>Client</th>
                            <th>Site Address</th>
                            <th>Start Date</th>
                            <th>Status</th>
                            <th>Action</th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr>
                            <td colspan="6">
                                <div class="jm-table-empty">
                                    <i class="bi bi-inbox"></i>
                                    <strong>
                                        Job records not loaded
                                    </strong>
                                    <div>
                                        Existing jobs will appear here
                                        once the database is connected.
                                    </div>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

        </div>

    </div>

</asp:Content>
