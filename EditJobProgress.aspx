
<%@ Page Title="Edit Job Progress" Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="EditJobProgress.aspx.cs"
    Inherits="GiantGroupM3.EditJobProgress" %>

<asp:Content ID="Content1"
    ContentPlaceHolderID="MainContent"
    runat="server">

<style>
    /* EDIT JOB PROGRESS - GIANT GROUP THEME */

    .ejp-wrap {
        max-width: 1140px;
        margin: 0 auto;
        padding: .5rem 0 2rem;
    }

    .ejp-banner {
        padding: 2rem 2.25rem;
        margin-bottom: 1.5rem;
    }

    .ejp-banner h1 {
        font-size: clamp(1.7rem, 3vw, 2.3rem);
        margin: .65rem 0 .5rem;
        color: #fff;
    }

    .ejp-banner p {
        max-width: 680px;
        margin: 0;
        color: #cfe5d9;
    }

    .ejp-breadcrumb {
        color: #cfe5d9;
        font-size: .83rem;
        font-weight: 600;
    }

    .ejp-heading {
        display: flex;
        align-items: center;
        gap: .75rem;
        margin-bottom: 1.25rem;
        padding-bottom: 1rem;
        border-bottom: 1px solid var(--gg-border);
    }

    .ejp-heading h2 {
        font-size: 1.08rem;
        font-weight: 700;
        margin: 0;
        color: var(--gg-text);
    }

    .ejp-help {
        display: block;
        color: var(--gg-muted);
        font-size: .82rem;
        line-height: 1.6;
        margin-top: .4rem;
    }

    .ejp-description {
        color: var(--gg-muted);
        font-size: .88rem;
        line-height: 1.6;
    }

    .ejp-readonly {
        background: var(--gg-surface-2) !important;
        color: var(--gg-muted) !important;
    }

    /* Selected job summary */

    .ejp-info {
        background: var(--gg-surface-2);
        border: 1px solid var(--gg-border);
        border-radius: 12px;
        padding: 1.25rem;
    }

    .ejp-info-title {
        color: var(--gg-text);
        font-weight: 700;
        font-size: .95rem;
        margin-bottom: 1rem;
    }

    .ejp-info-row {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 1rem;
        padding: .75rem 0;
        border-bottom: 1px solid var(--gg-border);
        font-size: .88rem;
    }

    .ejp-info-row:last-child {
        border-bottom: none;
    }

    .ejp-info-row span {
        color: var(--gg-muted);
    }

    .ejp-info-row strong {
        text-align: right;
        font-weight: 600;
        color: var(--gg-text);
    }

    /* Progress tracker */

    .ejp-progress-track {
        display: flex;
        gap: .75rem;
        justify-content: space-between;
        position: relative;
        margin: 2rem 0 1rem;
    }

    .ejp-progress-track::before {
        content: "";
        position: absolute;
        top: 20px;
        left: 16%;
        right: 16%;
        height: 2px;
        background: var(--gg-border);
    }

    .ejp-progress-step {
        flex: 1;
        position: relative;
        z-index: 1;
        text-align: center;
    }

    .ejp-progress-circle {
        width: 42px;
        height: 42px;
        border: 2px solid var(--gg-border);
        background: var(--gg-surface-2);
        color: var(--gg-muted);
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        margin: 0 auto .75rem;
        font-size: 1.1rem;
    }

    .ejp-progress-title {
        display: block;
        font-size: .85rem;
        font-weight: 700;
        color: var(--gg-text);
    }

    .ejp-progress-desc {
        display: block;
        font-size: .75rem;
        color: var(--gg-muted);
        margin-top: .25rem;
    }

    /* Completion evidence */

    .ejp-evidence {
        padding: 1.25rem;
        border: 1px dashed var(--gg-border);
        background: var(--gg-surface-2);
        border-radius: 12px;
    }

    .ejp-upload {
        width: 100%;
        border-radius: 10px;
        border: 1px solid var(--gg-border);
        padding: .75rem;
        background: var(--gg-surface);
        color: var(--gg-text);
        font-size: .88rem;
    }

    .ejp-upload::file-selector-button {
        border: 1px solid var(--gg-border);
        border-radius: 8px;
        padding: .5rem .8rem;
        margin-right: 1rem;
        background: var(--gg-surface-2);
        color: var(--gg-text);
        cursor: pointer;
    }

    .ejp-textarea {
        resize: vertical;
        min-height: 115px;
    }

    .ejp-warning {
        background: var(--gg-amber-soft);
        color: var(--gg-amber);
        border-radius: 10px;
        padding: 1rem;
        font-size: .85rem;
        line-height: 1.6;
    }

    /* Action controls */

    .ejp-actions {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: .75rem;
        padding-top: 1.25rem;
        border-top: 1px solid var(--gg-border);
    }

    .ejp-preview-tag {
        display: inline-flex;
        align-items: center;
        gap: .4rem;
        font-size: .75rem;
        font-weight: 600;
        color: var(--gg-muted);
        border: 1px solid var(--gg-border);
        border-radius: 999px;
        padding: .35rem .75rem;
    }

    @media (max-width: 767px) {
        .ejp-banner {
            padding: 1.5rem;
        }

        .ejp-actions {
            flex-direction: column;
            align-items: stretch;
        }

        .ejp-actions .gg-btn {
            width: 100%;
            text-align: center;
        }

        .ejp-progress-title {
            font-size: .75rem;
        }
    }
</style>

<div class="ejp-wrap">

    <!-- ========== HEADER ========== -->

    <div class="gg-banner ejp-banner">

        <div class="ejp-breadcrumb">
            <i class="bi bi-grid me-1"></i>
            Management / Jobs / Edit Progress
        </div>

        <h1>
            <i class="bi bi-pencil-square me-2"></i>
            Edit Job Progress
        </h1>

        <p>
            Update the operational status of a scheduled
            job, record completion details and monitor
            progress through each stage of the job.
        </p>

    </div>

    <!-- ========== PREVIEW NOTICE ========== -->

    <div class="gg-msg is-info mb-4">
        <i class="bi bi-info-circle me-2"></i>

        <strong>Interface Preview:</strong>
        This form is currently in design mode.
        Job loading, status updates, evidence uploading
        and database saving will be connected later.
    </div>

    <!-- ========== STEP 1: SELECT JOB ========== -->

    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="ejp-heading">
                <span class="gg-step-n">1</span>
                <h2>Select Job</h2>
            </div>

            <p class="ejp-description mb-4">
                Choose an existing job to review its
                information and update its progress.
            </p>

            <div class="row g-4 align-items-end">

                <div class="col-md-8">

                    <asp:Label
                        runat="server"
                        AssociatedControlID="ddlJobs"
                        CssClass="gg-field-label"
                        Text="Select Existing Job" />

                    <asp:DropDownList
                        ID="ddlJobs"
                        runat="server"
                        CssClass="gg-select">

                        <asp:ListItem
                            Text="-- Select a job --"
                            Value="" />

                    </asp:DropDownList>

                    <span class="ejp-help">
                        Jobs will load from the database
                        once the backend is connected.
                    </span>

                </div>

                <div class="col-md-4">

                    <button
                        type="button"
                        class="gg-btn gg-btn-primary w-100"
                        disabled="disabled">

                        <i class="bi bi-search"></i>
                        Load Job Details

                    </button>

                </div>

            </div>

        </div>
    </div>

    <!-- ========== STEP 2: JOB DETAILS ========== -->

    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="ejp-heading">
                <span class="gg-step-n">2</span>
                <h2>Job Information</h2>
            </div>

            <p class="ejp-description mb-4">
                Review the selected job details before
                making changes to its progress.
            </p>

            <div class="row g-4">

                <!-- LEFT COLUMN -->
                <div class="col-lg-7">

                    <div class="row g-3">

                        <div class="col-sm-6">

                            <asp:Label
                                runat="server"
                                AssociatedControlID="txtJobID"
                                CssClass="gg-field-label"
                                Text="Job ID" />

                            <asp:TextBox
                                ID="txtJobID"
                                runat="server"
                                CssClass="gg-input ejp-readonly"
                                ReadOnly="true"
                                placeholder="Job ID" />

                        </div>

                        <div class="col-sm-6">

                            <asp:Label
                                runat="server"
                                AssociatedControlID="txtQuoteID"
                                CssClass="gg-field-label"
                                Text="Quotation ID" />

                            <asp:TextBox
                                ID="txtQuoteID"
                                runat="server"
                                CssClass="gg-input ejp-readonly"
                                ReadOnly="true"
                                placeholder="Quotation ID" />

                        </div>

                        <div class="col-12">

                            <asp:Label
                                runat="server"
                                AssociatedControlID="txtClientName"
                                CssClass="gg-field-label"
                                Text="Client Name" />

                            <asp:TextBox
                                ID="txtClientName"
                                runat="server"
                                CssClass="gg-input ejp-readonly"
                                ReadOnly="true"
                                placeholder="Client name" />

                        </div>

                        <div class="col-12">

                            <asp:Label
                                runat="server"
                                AssociatedControlID="txtSiteAddress"
                                CssClass="gg-field-label"
                                Text="Site Address" />

                            <asp:TextBox
                                ID="txtSiteAddress"
                                runat="server"
                                CssClass="gg-input ejp-readonly"
                                ReadOnly="true"
                                placeholder="Job site address" />

                        </div>

                        <div class="col-sm-6">

                            <asp:Label
                                runat="server"
                                AssociatedControlID="txtScheduledDate"
                                CssClass="gg-field-label"
                                Text="Scheduled Start Date" />

                            <asp:TextBox
                                ID="txtScheduledDate"
                                runat="server"
                                CssClass="gg-input ejp-readonly"
                                ReadOnly="true"
                                placeholder="Scheduled date" />

                        </div>

                        <div class="col-sm-6">

                            <asp:Label
                                runat="server"
                                AssociatedControlID="txtTimeSlots"
                                CssClass="gg-field-label"
                                Text="Scheduled Time Slots" />

                            <asp:TextBox
                                ID="txtTimeSlots"
                                runat="server"
                                CssClass="gg-input ejp-readonly"
                                ReadOnly="true"
                                placeholder="Time slots" />

                        </div>

                    </div>

                </div>

                <!-- RIGHT COLUMN -->
                <div class="col-lg-5">

                    <div class="ejp-info h-100">

                        <div class="ejp-info-title">
                            <i class="bi bi-clipboard-check me-2"></i>
                            Current Job Overview
                        </div>

                        <div class="ejp-info-row">
                            <span>Job ID</span>
                            <strong>Not selected</strong>
                        </div>

                        <div class="ejp-info-row">
                            <span>Client</span>
                            <strong>Not selected</strong>
                        </div>

                        <div class="ejp-info-row">
                            <span>Scheduled Date</span>
                            <strong>Not scheduled</strong>
                        </div>

                        <div class="ejp-info-row">
                            <span>Current Status</span>
                            <strong>Not loaded</strong>
                        </div>

                    </div>

                </div>

            </div>

        </div>
    </div>

    <!-- ========== STEP 3: UPDATE STATUS ========== -->

    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="ejp-heading">
                <span class="gg-step-n">3</span>
                <h2>Update Job Status</h2>
            </div>

            <p class="ejp-description mb-4">
                Change the selected job's operational
                progress using the predefined job statuses.
            </p>

            <div class="row g-4">

                <div class="col-lg-6">

                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtCurrentStatus"
                        CssClass="gg-field-label"
                        Text="Current Job Status" />

                    <asp:TextBox
                        ID="txtCurrentStatus"
                        runat="server"
                        CssClass="gg-input ejp-readonly"
                        ReadOnly="true"
                        placeholder="Current status will appear here" />

                </div>

                <div class="col-lg-6">

                    <asp:Label
                        runat="server"
                        AssociatedControlID="ddlNewStatus"
                        CssClass="gg-field-label"
                        Text="New Job Status" />

                    <asp:DropDownList
                        ID="ddlNewStatus"
                        runat="server"
                        CssClass="gg-select">

                        <asp:ListItem
                            Text="-- Select new status --"
                            Value="" />

                        <asp:ListItem
                            Text="Not Started"
                            Value="Not Started" />

                        <asp:ListItem
                            Text="In Progress"
                            Value="In Progress" />

                        <asp:ListItem
                            Text="Completed"
                            Value="Completed" />

                    </asp:DropDownList>

                    <span class="ejp-help">
                        Select the new stage of the
                        job's operational progress.
                    </span>

                </div>

            </div>

            <!-- STATUS WORKFLOW -->

            <div class="ejp-info mt-4">

                <h3 class="gg-section-title">
                    Job Progress Workflow
                </h3>

                <p class="ejp-description">
                    The progress tracker will highlight
                    the selected job's current status
                    when connected to the database.
                </p>

                <div class="ejp-progress-track">

                    <div class="ejp-progress-step">

                        <div class="ejp-progress-circle">
                            <i class="bi bi-calendar-check"></i>
                        </div>

                        <span class="ejp-progress-title">
                            Not Started
                        </span>

                        <span class="ejp-progress-desc">
                            Job scheduled
                        </span>

                    </div>

                    <div class="ejp-progress-step">

                        <div class="ejp-progress-circle">
                            <i class="bi bi-gear"></i>
                        </div>

                        <span class="ejp-progress-title">
                            In Progress
                        </span>

                        <span class="ejp-progress-desc">
                            Work underway
                        </span>

                    </div>

                    <div class="ejp-progress-step">

                        <div class="ejp-progress-circle">
                            <i class="bi bi-check-circle"></i>
                        </div>

                        <span class="ejp-progress-title">
                            Completed
                        </span>

                        <span class="ejp-progress-desc">
                            Work finished
                        </span>

                    </div>

                </div>

            </div>

            <div class="gg-msg is-info mt-4 mb-0">

                <i class="bi bi-info-circle me-2"></i>

                When progress is connected to the
                backend, status changes will be
                recorded against the selected job.

            </div>

        </div>
    </div>

    <!-- ========== STEP 4: COMPLETION EVIDENCE ========== -->

    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="ejp-heading">
                <span class="gg-step-n">4</span>
                <h2>Job Completion Evidence</h2>
            </div>

            <p class="ejp-description mb-4">
                Record completion information when
                work on a job has finished.
                These fields will be used when a job
                is marked as Completed.
            </p>

            <div class="ejp-evidence">

                <div class="row g-4">

                    <!-- COMPLETION DATE -->
                    <div class="col-md-6">

                        <asp:Label
                            runat="server"
                            AssociatedControlID="txtCompletionDate"
                            CssClass="gg-field-label"
                            Text="Completion Date" />

                        <asp:TextBox
                            ID="txtCompletionDate"
                            runat="server"
                            CssClass="gg-input ejp-readonly"
                            ReadOnly="true"
                            placeholder="Automatically recorded" />

                        <span class="ejp-help">
                            The completion date will be
                            recorded automatically when
                            this feature is implemented.
                        </span>

                    </div>

                    <!-- COMPLETION PHOTO -->
                    <div class="col-md-6">

                        <asp:Label
                            runat="server"
                            AssociatedControlID="fuCompletionPhoto"
                            CssClass="gg-field-label"
                            Text="Upload Completion Photo" />

                        <asp:FileUpload
                            ID="fuCompletionPhoto"
                            runat="server"
                            CssClass="ejp-upload"
                            accept="image/*" />

                        <span class="ejp-help">
                            Select a photo showing the
                            completed work. Uploading
                            is not yet connected.
                        </span>

                    </div>

                    <!-- COMPLETION NOTES -->
                    <div class="col-12">

                        <asp:Label
                            runat="server"
                            AssociatedControlID="txtCompletionNotes"
                            CssClass="gg-field-label"
                            Text="Completion Notes" />

                        <asp:TextBox
                            ID="txtCompletionNotes"
                            runat="server"
                            TextMode="MultiLine"
                            Rows="5"
                            CssClass="gg-input ejp-textarea"
                            placeholder="Enter completion details, work performed, any issues encountered or additional remarks..." />

                        <span class="ejp-help">
                            Notes may include a summary
                            of work completed and any
                            relevant site observations.
                        </span>

                    </div>

                </div>

            </div>

            <!-- RESOURCE RELEASE NOTICE -->

            <div class="ejp-warning mt-4">

                <i class="bi bi-exclamation-triangle me-2"></i>

                <strong>Completion Reminder:</strong>
                When a job is marked as Completed,
                the system must update the availability
                of assigned staff and equipment.

                This action will be implemented with
                the backend functionality.

            </div>

        </div>
    </div>

    <!-- ========== STEP 5: SAVE CHANGES ========== -->

    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="d-flex justify-content-between
                        align-items-start flex-wrap gap-3 mb-3">

                <div>
                    <h2 class="gg-section-title">
                        Save Progress Update
                    </h2>

                    <p class="ejp-description mb-0">
                        Review all details before saving
                        changes to the selected job.
                    </p>
                </div>

                <span class="ejp-preview-tag">
                    <i class="bi bi-code-slash"></i>
                    Actions not connected
                </span>

            </div>

            <div class="ejp-actions">

                <button
                    type="button"
                    class="gg-btn gg-btn-primary"
                    disabled="disabled">

                    <i class="bi bi-save me-1"></i>
                    Save Progress Update

                </button>

                <button
                    type="button"
                    class="gg-btn gg-btn-outline"
                    disabled="disabled">

                    <i class="bi bi-arrow-counterclockwise me-1"></i>
                    Reset Changes

                </button>

                <a
                    runat="server"
                    href="~/ViewJobProgress.aspx"
                    class="gg-btn gg-btn-outline">

                    <i class="bi bi-arrow-left me-1"></i>
                    Back to View Job Progress

                </a>

            </div>

        </div>
    </div>

</div>

</asp:Content>
