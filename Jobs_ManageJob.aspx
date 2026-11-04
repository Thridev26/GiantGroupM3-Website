
<%@ Page Title="Manage Jobs" Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="Jobs_ManageJob.aspx.cs"
    Inherits="GiantGroupM3.Jobs_ManageJob" %>

<asp:Content ID="Content1"
    ContentPlaceHolderID="MainContent"
    runat="server">

<style>
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
        font-size: clamp(1.7rem,3vw,2.3rem);
        margin: .6rem 0;
    }

    .jm-breadcrumb {
        font-size: .83rem;
        color: #cfe5d9;
        font-weight: 600;
    }

    .jm-heading {
        display: flex;
        align-items: center;
        gap: .75rem;
        padding-bottom: 1rem;
        border-bottom: 1px solid var(--gg-border);
        margin-bottom: 1.2rem;
    }

    .jm-heading h2 {
        color: var(--gg-text);
        font-size: 1.1rem;
        font-weight: 700;
        margin: 0;
    }

    .jm-help {
        color: var(--gg-muted);
        font-size: .82rem;
        line-height: 1.6;
        margin-top: .4rem;
        display: block;
    }

    .jm-readonly {
        background: var(--gg-surface-2) !important;
        color: var(--gg-muted) !important;
    }

    .jm-slot-box {
        padding: 1.2rem;
        border: 1px dashed var(--gg-border);
        border-radius: 12px;
        background: var(--gg-surface-2);
    }

    .jm-slots {
        display: flex;
        flex-wrap: wrap;
        gap: .75rem;
        list-style: none;
        margin: 0;
        padding: 0;
    }

    .jm-slots li {
        padding: .7rem 1rem;
        border: 1px solid var(--gg-border);
        border-radius: 10px;
        background: var(--gg-surface);
        color: var(--gg-text);
    }

    .jm-slots input {
        accent-color: var(--gg-700);
        margin-right: .45rem;
    }

    .jm-summary {
        background: var(--gg-surface-2);
        border: 1px solid var(--gg-border);
        border-radius: 12px;
        padding: 1.25rem;
    }

    .jm-actions {
        display: flex;
        flex-wrap: wrap;
        gap: .75rem;
        align-items: center;
    }

    .jm-evidence {
        background: var(--gg-surface-2);
        border: 1px dashed var(--gg-border);
        border-radius: 12px;
        padding: 1.2rem;
    }

    .jm-file {
        width: 100%;
        padding: .7rem;
        color: var(--gg-text);
        background: var(--gg-surface);
        border: 1px solid var(--gg-border);
        border-radius: 10px;
    }

    .jm-danger {
        border: 1px solid #e4b6b1;
        border-radius: 12px;
        padding: 1.2rem;
    }

    .jm-danger-title {
        color: #c0392b;
        font-weight: 700;
    }

    .jm-table-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 1rem;
        margin-bottom: 1rem;
    }

    .jm-grid {
        width: 100%;
    }

    .jm-grid th {
        background: var(--gg-surface-2);
        color: var(--gg-muted);
        padding: .85rem;
        font-weight: 600;
        font-size: .85rem;
    }

    .jm-grid td {
        color: var(--gg-text);
        padding: .85rem;
        border-bottom: 1px solid var(--gg-border);
        font-size: .88rem;
    }

    .jm-grid a {
        color: var(--gg-accent);
        font-weight: 700;
    }

    .jm-grid tr:hover td {
        background: var(--gg-surface-2);
    }

    @media(max-width:767px) {
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

    <!-- HEADER -->
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
            Create jobs from accepted quotations,
            manage schedules, update records and
            control job operations.
        </p>
    </div>

    <!-- RESULT MESSAGE -->
    <asp:Label
        ID="lblMessage"
        runat="server"
        Visible="false"
        EnableViewState="false"
        CssClass="gg-msg" />

    <!-- STEP 1: SELECT JOB -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="jm-heading">
                <span class="gg-step-n">1</span>
                <h2>Select Quotation or Existing Job</h2>
            </div>

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
        CssClass="gg-select"
        AutoPostBack="true"
        OnSelectedIndexChanged="ddlAcceptedQuote_SelectedIndexChanged">
    </asp:DropDownList>

    <asp:Label
        ID="lblQuoteInfo"
        runat="server"
        CssClass="jm-help"
        Text="Loading quotations..." />

    <div class="mt-2">
        <a runat="server"
           href="~/ViewQuote.aspx"
           class="gg-link">
            <i class="bi bi-file-earmark-text me-1"></i>
            View All Quotations
        </a>
    </div>

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
                        CssClass="gg-select"
                        AutoPostBack="true"
                        OnSelectedIndexChanged="ddlExistingJob_SelectedIndexChanged" />

                    <span class="jm-help">
                        Select an existing job to update,
                        archive or cancel.
                    </span>
                </div>

            </div>

        </div>
    </div>

    <!-- STEP 2: JOB INFORMATION -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="jm-heading">
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
                        ReadOnly="true" />
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
                        ReadOnly="true" />
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
                        CssClass="gg-select"
                        AutoPostBack="true"
                        OnSelectedIndexChanged="ddlJobStatus_SelectedIndexChanged">

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
                        ReadOnly="true" />
                </div>

            </div>

        </div>
    </div>

    <!-- STEP 3: SCHEDULING -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="jm-heading">
                <span class="gg-step-n">3</span>
                <h2>Job Schedule</h2>
            </div>

            <div class="row g-3 align-items-end mb-4">

                <div class="col-md-8">
                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtStartDate"
                        CssClass="gg-field-label"
                        Text="Scheduled Start Date" />

                    <asp:TextBox
                        ID="txtStartDate"
                        runat="server"
                        TextMode="Date"
                        CssClass="gg-input"
                        AutoPostBack="true"
                        OnTextChanged="txtStartDate_TextChanged" />
                </div>

                <div class="col-md-4">
                    <asp:Button
                        ID="btnRefreshSlots"
                        runat="server"
                        Text="Check Availability"
                        CssClass="gg-btn gg-btn-outline w-100"
                        CausesValidation="false"
                        OnClick="btnRefreshSlots_Click" />
                </div>

            </div>

            <label class="gg-field-label">
                Available Time Slots
            </label>

            <div class="jm-slot-box">

                <asp:CheckBoxList
                    ID="cblTimeSlots"
                    runat="server"
                    CssClass="jm-slots"
                    RepeatLayout="UnorderedList" />

                <asp:Label
                    ID="lblSlotMessage"
                    runat="server"
                    CssClass="jm-help" />

            </div>

            <span class="jm-help">
                Time slots are loaded directly from
                the existing TimeSlot table.
                Booked slots are unavailable.
                Select at least one slot.
            </span>

        </div>
    </div>

    <!-- STEP 4: JOB COSTS -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="jm-heading">
                <span class="gg-step-n">4</span>
                <h2>Job Costs</h2>
            </div>

            <div class="row g-4">

                <div class="col-md-6">
                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtFuelCost"
                        CssClass="gg-field-label"
                        Text="Fuel / Travel Cost (R)" />

                    <asp:TextBox
                        ID="txtFuelCost"
                        runat="server"
                        CssClass="gg-input jm-readonly"
                        ReadOnly="true"
                        Text="0.00" />

                    <span class="jm-help">
                        Calculated automatically from
                        the accepted quotation.
                    </span>
                </div>

                <div class="col-md-6">
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
                        Text="0.00"
                        min="0"
                        step="0.01" />

                    <span class="jm-help">
                        Initially R0.00 when a job is
                        created. Can be updated later.
                    </span>
                </div>

            </div>

        </div>
    </div>

    <!-- STEP 5: COMPLETION PHOTO -->
    <asp:Panel
        ID="pnlCompletion"
        runat="server"
        Visible="false"
        CssClass="gg-panel mb-4">

        <div class="gg-panel-body">

            <div class="jm-heading">
                <span class="gg-step-n">5</span>
                <h2>Completion Evidence</h2>
            </div>

            <div class="jm-evidence">

                <asp:Label
                    runat="server"
                    AssociatedControlID="fuCompletionPhoto"
                    CssClass="gg-field-label"
                    Text="Upload Completion Photo" />

                <asp:FileUpload
                    ID="fuCompletionPhoto"
                    runat="server"
                    CssClass="jm-file"
                    accept=".jpg,.jpeg,.png,image/jpeg,image/png" />

                <span class="jm-help">
                    Required when changing a job to
                    Completed, unless an AFTER photo
                    already exists for that job.
                    JPEG or PNG, maximum 15 MB.
                </span>

            </div>

        </div>
    </asp:Panel>

    <!-- STEP 6: JOB ACTIONS -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="jm-heading">
                <span class="gg-step-n">6</span>
                <h2>Manage Job</h2>
            </div>

            <p class="gg-muted">
                Create a job from an accepted quotation
                or update a selected job.
            </p>

            <div class="jm-actions">

                <asp:Button
                    ID="btnCreateJob"
                    runat="server"
                    Text="Create Job"
                    CssClass="gg-btn gg-btn-primary"
                    CausesValidation="false"
                    OnClientClick="return confirm('Create this scheduled job?');"
                    OnClick="btnCreateJob_Click" />

                <asp:Button
                    ID="btnUpdateJob"
                    runat="server"
                    Text="Update Job"
                    CssClass="gg-btn gg-btn-outline"
                    CausesValidation="false"
                    OnClientClick="return confirm('Save changes to this job?');"
                    OnClick="btnUpdateJob_Click" />

                <asp:Button
                    ID="btnArchiveJob"
                    runat="server"
                    Text="Archive Job"
                    CssClass="gg-btn gg-btn-outline"
                    CausesValidation="false"
                    OnClientClick="return confirm('Archive this job?');"
                    OnClick="btnArchiveJob_Click" />

                <asp:Button
                    ID="btnClear"
                    runat="server"
                    Text="Clear Form"
                    CssClass="gg-btn gg-btn-outline"
                    CausesValidation="false"
                    OnClick="btnClear_Click" />

            </div>

        </div>
    </div>

    <!-- CANCELLATION -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="jm-danger">

                <div class="jm-danger-title mb-2">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    Cancel Job
                </div>

                <p class="gg-muted">
                    Cancellation permanently removes
                    the selected job, its schedules,
                    assignments and job photos.
                    The original request and quotation
                    remain available for historical records.
                    Paid jobs cannot be deleted here.
                </p>

                <asp:Button
                    ID="btnCancelJob"
                    runat="server"
                    Text="Cancel Job Permanently"
                    CssClass="gg-btn gg-btn-danger-outline"
                    CausesValidation="false"
                    OnClientClick="return confirm('WARNING: This permanently deletes the job and its operational records. Continue?');"
                    OnClick="btnCancelJob_Click" />

            </div>

        </div>
    </div>

    <!-- EXISTING JOBS -->
    <div class="jm-table-header">

        <div>
            <span class="gg-label">
                <i class="bi bi-list-task"></i>
                Job Records
            </span>

            <h2 class="gg-section-title mt-2">
                Existing Jobs
            </h2>
        </div>

        <div class="d-flex gap-2">

            <asp:TextBox
                ID="txtSearchJobs"
                runat="server"
                CssClass="gg-input"
                placeholder="Search job, client or site..." />

            <asp:Button
                ID="btnSearch"
                runat="server"
                Text="Search"
                CssClass="gg-btn gg-btn-primary"
                CausesValidation="false"
                OnClick="btnSearch_Click" />

        </div>

    </div>

    <div class="gg-panel mb-4">
        <div class="gg-table-wrap">

            <asp:GridView
                ID="gvJobs"
                runat="server"
                CssClass="table gg-table jm-grid"
                AutoGenerateColumns="false"
                DataKeyNames="jobID"
                AllowPaging="true"
                PageSize="10"
                GridLines="None"
                EmptyDataText="No jobs found."
                OnSelectedIndexChanged="gvJobs_SelectedIndexChanged"
                OnPageIndexChanging="gvJobs_PageIndexChanging">

                <Columns>

                    <asp:BoundField
                        DataField="jobID"
                        HeaderText="Job ID" />

                    <asp:BoundField
                        DataField="ClientName"
                        HeaderText="Client" />

                    <asp:BoundField
                        DataField="siteAddress"
                        HeaderText="Site Address" />

                    <asp:BoundField
                        DataField="startDate"
                        HeaderText="Start Date"
                        DataFormatString="{0:dd MMM yyyy}"
                        HtmlEncode="false" />

                    <asp:BoundField
                        DataField="jobStatus"
                        HeaderText="Status" />

                    <asp:BoundField
                        DataField="totalFuelCost"
                        HeaderText="Fuel Cost"
                        DataFormatString="{0:N2}" />

                    <asp:BoundField
                        DataField="dumpingCost"
                        HeaderText="Dumping Cost"
                        DataFormatString="{0:N2}" />

                    <asp:CommandField
                        ShowSelectButton="true"
                        SelectText="Open Job"
                        HeaderText="Action" />

                </Columns>

                <PagerStyle CssClass="gg-pager" />

            </asp:GridView>

        </div>
    </div>

</div>

</asp:Content>
