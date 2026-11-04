
<%@ Page Title="Edit Job Progress" Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="EditJobProgress.aspx.cs"
    Inherits="GiantGroupM3.EditJobProgress" %>

<asp:Content ID="Content1"
    ContentPlaceHolderID="MainContent"
    runat="server">

<style>
    .ejp-page {
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
        margin: .65rem 0;
        color: #fff;
    }

    .ejp-banner p,
    .ejp-breadcrumb {
        color: #cfe5d9;
    }

    .ejp-breadcrumb {
        font-size: .83rem;
        font-weight: 600;
    }

    .ejp-heading {
        display: flex;
        align-items: center;
        gap: .75rem;
        padding-bottom: 1rem;
        margin-bottom: 1.25rem;
        border-bottom: 1px solid var(--gg-border);
    }

    .ejp-heading h2 {
        font-size: 1.08rem;
        font-weight: 700;
        color: var(--gg-text);
        margin: 0;
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

    .ejp-help {
        color: var(--gg-muted);
        font-size: .8rem;
        display: block;
        margin-top: .4rem;
    }

    .ejp-details {
        padding: 1.25rem;
        border-radius: 12px;
        border: 1px solid var(--gg-border);
        background: var(--gg-surface-2);
    }

    .ejp-detail-row {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        padding: .75rem 0;
        border-bottom: 1px solid var(--gg-border);
        font-size: .88rem;
    }

    .ejp-detail-row:last-child {
        border-bottom: 0;
    }

    .ejp-detail-row span {
        color: var(--gg-muted);
    }

    .ejp-detail-row strong {
        color: var(--gg-text);
        text-align: right;
    }

    .ejp-progress {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: .75rem;
        margin: 1.5rem 0;
    }

    .ejp-stage {
        text-align: center;
        border: 2px solid var(--gg-border);
        background: var(--gg-surface-2);
        border-radius: 12px;
        padding: 1.25rem .5rem;
        color: var(--gg-muted);
    }

    .ejp-stage i {
        display: block;
        font-size: 1.5rem;
        margin-bottom: .65rem;
    }

    .ejp-stage strong {
        display: block;
        font-size: .85rem;
    }

    .ejp-stage small {
        display: block;
        font-size: .72rem;
        margin-top: .3rem;
    }

    .ejp-stage.is-current {
        border-color: var(--gg-accent);
        color: var(--gg-accent);
        background: var(--gg-accent-soft);
    }

    .ejp-stage.is-done {
        border-color: var(--gg-600);
        color: var(--gg-accent);
    }

    .ejp-evidence {
        padding: 1.25rem;
        background: var(--gg-surface-2);
        border: 1px dashed var(--gg-border);
        border-radius: 12px;
    }

    .ejp-file {
        width: 100%;
        padding: .75rem;
        background: var(--gg-surface);
        border: 1px solid var(--gg-border);
        border-radius: 10px;
        color: var(--gg-text);
    }

    .ejp-actions {
        display: flex;
        flex-wrap: wrap;
        gap: .75rem;
    }

    @media (max-width: 767px) {
        .ejp-banner {
            padding: 1.5rem;
        }

        .ejp-actions {
            flex-direction: column;
        }

        .ejp-actions .gg-btn {
            width: 100%;
        }
    }
</style>

<div class="ejp-page">

    <!-- HEADER -->
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
            Update operational progress,
            view job information and
            record completion evidence.
        </p>
    </div>

    <!-- MESSAGES -->
    <asp:Label
        ID="lblMessage"
        runat="server"
        Visible="false"
        EnableViewState="false"
        CssClass="gg-msg" />

    <!-- SELECT JOB -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="ejp-heading">
                <span class="gg-step-n">1</span>
                <h2>Select Job</h2>
            </div>

            <p class="ejp-description mb-4">
                Choose an existing job to review
                and update its progress.
            </p>

            <div class="row g-3 align-items-end">

                <div class="col-md-8">
                    <asp:Label
                        runat="server"
                        AssociatedControlID="ddlJobs"
                        CssClass="gg-field-label"
                        Text="Existing Jobs" />

                    <asp:DropDownList
                        ID="ddlJobs"
                        runat="server"
                        CssClass="gg-select" />
                </div>

                <div class="col-md-4">
                    <asp:Button
                        ID="btnLoadJob"
                        runat="server"
                        Text="Load Job Details"
                        CssClass="gg-btn gg-btn-primary w-100"
                        CausesValidation="false"
                        OnClick="btnLoadJob_Click" />
                </div>

            </div>

        </div>
    </div>

    <!-- JOB INFORMATION -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="ejp-heading">
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
                        CssClass="gg-input ejp-readonly"
                        ReadOnly="true" />
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
                        CssClass="gg-input ejp-readonly"
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
                        CssClass="gg-input ejp-readonly"
                        ReadOnly="true" />
                </div>

                <div class="col-md-6">
                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtScheduledDate"
                        CssClass="gg-field-label"
                        Text="Scheduled Date" />

                    <asp:TextBox
                        ID="txtScheduledDate"
                        runat="server"
                        CssClass="gg-input ejp-readonly"
                        ReadOnly="true" />
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
                        ReadOnly="true" />
                </div>

                <div class="col-12">
                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtTimeSlots"
                        CssClass="gg-field-label"
                        Text="Scheduled Time Slots" />

                    <asp:TextBox
                        ID="txtTimeSlots"
                        runat="server"
                        CssClass="gg-input ejp-readonly"
                        ReadOnly="true" />
                </div>

            </div>

        </div>
    </div>

    <!-- UPDATE STATUS -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="ejp-heading">
                <span class="gg-step-n">3</span>
                <h2>Update Job Status</h2>
            </div>

            <div class="row g-4">

                <div class="col-md-6">
                    <asp:Label
                        runat="server"
                        AssociatedControlID="txtCurrentStatus"
                        CssClass="gg-field-label"
                        Text="Current Job Status" />

                    <asp:TextBox
                        ID="txtCurrentStatus"
                        runat="server"
                        CssClass="gg-input ejp-readonly"
                        ReadOnly="true" />
                </div>

                <div class="col-md-6">
                    <asp:Label
                        runat="server"
                        AssociatedControlID="ddlNewStatus"
                        CssClass="gg-field-label"
                        Text="Update Status To" />

                    <asp:DropDownList
                        ID="ddlNewStatus"
                        runat="server"
                        CssClass="gg-select"
                        AutoPostBack="true"
                        OnSelectedIndexChanged="ddlNewStatus_SelectedIndexChanged">

                        <asp:ListItem
                            Text="-- Select next status --"
                            Value="" />

                        <asp:ListItem
                            Text="In Progress"
                            Value="In Progress" />

                        <asp:ListItem
                            Text="Completed"
                            Value="Completed" />

                    </asp:DropDownList>

                    <span class="ejp-help">
                        Only the next valid progress
                        stage can be saved.
                    </span>
                </div>

            </div>

            <!-- PROGRESS TRACKER -->
            <div class="ejp-progress">

                <asp:Panel
                    ID="pnlNotStarted"
                    runat="server"
                    CssClass="ejp-stage">

                    <i class="bi bi-calendar-check"></i>
                    <strong>Not Started</strong>
                    <small>Scheduled</small>

                </asp:Panel>

                <asp:Panel
                    ID="pnlInProgress"
                    runat="server"
                    CssClass="ejp-stage">

                    <i class="bi bi-gear"></i>
                    <strong>In Progress</strong>
                    <small>Work underway</small>

                </asp:Panel>

                <asp:Panel
                    ID="pnlCompleted"
                    runat="server"
                    CssClass="ejp-stage">

                    <i class="bi bi-check-circle"></i>
                    <strong>Completed</strong>
                    <small>Work finished</small>

                </asp:Panel>

            </div>

        </div>
    </div>

    <!-- COMPLETION EVIDENCE -->
    <asp:Panel
        ID="pnlCompletion"
        runat="server"
        Visible="false"
        CssClass="gg-panel mb-4">

        <div class="gg-panel-body">

            <div class="ejp-heading">
                <span class="gg-step-n">4</span>
                <h2>Completion Evidence</h2>
            </div>

            <p class="ejp-description">
                Upload a completion photo before
                marking this job as Completed.
            </p>

            <div class="ejp-evidence">

                <div class="row g-4">

                    <div class="col-md-6">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="txtCompletionDate"
                            CssClass="gg-field-label"
                            Text="Latest Completion Photo Date" />

                        <asp:TextBox
                            ID="txtCompletionDate"
                            runat="server"
                            CssClass="gg-input ejp-readonly"
                            ReadOnly="true"
                            placeholder="Not recorded" />
                    </div>

                    <div class="col-md-6">
                        <asp:Label
                            runat="server"
                            AssociatedControlID="fuCompletionPhoto"
                            CssClass="gg-field-label"
                            Text="Upload Completion Photo" />

                        <asp:FileUpload
                            ID="fuCompletionPhoto"
                            runat="server"
                            CssClass="ejp-file"
                            accept=".jpg,.jpeg,.png,image/jpeg,image/png" />

                        <span class="ejp-help">
                            JPEG or PNG, maximum 3 MB.
                        </span>
                    </div>

                </div>

                <asp:Label
                    ID="lblPhotoInfo"
                    runat="server"
                    CssClass="ejp-help mt-3" />

            </div>

        </div>
    </asp:Panel>

    <!-- SAVE ACTIONS -->
    <div class="gg-panel mb-4">
        <div class="gg-panel-body">

            <div class="ejp-heading">
                <span class="gg-step-n">5</span>
                <h2>Save Progress Update</h2>
            </div>

            <p class="ejp-description">
                Confirm the selected job's
                new operational status.
            </p>

            <div class="ejp-actions">

                <asp:Button
                    ID="btnSaveProgress"
                    runat="server"
                    Text="Save Progress Update"
                    CssClass="gg-btn gg-btn-primary"
                    CausesValidation="false"
                    Enabled="false"
                    OnClientClick="return confirm('Save this progress update?');"
                    OnClick="btnSaveProgress_Click" />

                <asp:Button
                    ID="btnResetProgress"
                    runat="server"
                    Text="Reset Changes"
                    CssClass="gg-btn gg-btn-outline"
                    CausesValidation="false"
                    OnClick="btnResetProgress_Click" />

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
