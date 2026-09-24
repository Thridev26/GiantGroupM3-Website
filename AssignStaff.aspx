<%@ Page Title="Assign Staff to Job" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AssignStaff.aspx.cs" Inherits="GiantGroupM3.AssignStaff" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        :root {
            --brand-green: #2D7A58;
            --brand-green-hover: #236146;
            --brand-green-light: #EBF4F0;
        }

        .page-header-title {
            color: var(--brand-green);
            font-weight: 700;
        }

        .custom-card {
            background-color: #fcfcf9;
            border: 1px solid #e2e2dc;
            border-radius: 12px;
        }

        .table-custom-header th {
            background-color: #eae9e1 !important;
            color: #212529 !important;
            font-size: 0.85rem;
            letter-spacing: 0.5px;
            border-bottom: 2px solid #d8d7cd;
        }

        .selected-job-banner {
            background-color: #eef7ee;
            border: 1px solid #c8e6c9;
            border-radius: 10px;
            padding: 15px 20px;
        }

        /* Custom Pagination Bar Styling */
        .pager-wrapper {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 12px 20px;
            background-color: #ffffff;
            border-top: 1px solid #e2e2dc;
            border-bottom-left-radius: 12px;
            border-bottom-right-radius: 12px;
        }

        .pager-btn {
            background-color: #ffffff;
            color: #444444;
            border: 1px solid #cccccc;
            padding: 5px 14px;
            border-radius: 6px;
            font-size: 0.82rem;
            font-weight: 600;
            text-transform: uppercase;
            text-decoration: none !important;
            display: inline-block;
            transition: all 0.2s ease;
        }

        .pager-btn:hover:not(.disabled) {
            background-color: var(--brand-green-light);
            color: var(--brand-green);
            border-color: var(--brand-green);
        }

        .pager-btn.disabled {
            color: #cccccc;
            border-color: #e6e6e6;
            cursor: not-allowed;
            pointer-events: none;
        }

        .page-indicator {
            color: #666666;
            font-size: 0.9rem;
        }
    </style>

    <div class="container py-4">
        <!-- Header -->
        <div class="mb-4">
            <h2 class="page-header-title mb-1"><i class="bi bi-person-plus me-2"></i>Assign Staff to Job</h2>
            <p class="text-secondary mb-0">Select a Job and then assign Staff members</p>
        </div>

        <asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
            <ContentTemplate>

                <!-- Feedback Alert -->
                <asp:Label ID="lblMessage" runat="server" CssClass="d-none" role="alert"></asp:Label>

                <!-- Hidden ID trackers -->
                <asp:HiddenField ID="hfSelectedJobID" runat="server" />

                <!-- STEP 1: Select Active Job -->
                <div class="card custom-card shadow-sm border-0 mb-4">
                    <div class="card-body p-4 p-bottom-0">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <h5 class="fw-bold mb-0 text-dark"><span class="badge bg-success me-2">Step 1</span> Select Active Job</h5>
                            <div class="input-group input-group-sm" style="max-width: 280px;">
                                <asp:TextBox ID="txtSearchJobs" runat="server" CssClass="form-control" Placeholder="Search job, client or site..." AutoPostBack="true" OnTextChanged="txtSearchJobs_TextChanged" onkeyup="triggerPostBackOnType(this)"></asp:TextBox>
                                <asp:Button ID="btnSearchJobs" runat="server" Text="Search" CssClass="btn btn-outline-secondary" OnClick="btnSearchJobs_Click" />
                            </div>
                        </div>

                        <!-- Selected Job Banner -->
                        <asp:Panel ID="pnlSelectedJob" runat="server" CssClass="selected-job-banner mb-3 d-none">
                            <div class="d-flex justify-content-between align-items-center">
                                <div>
                                    <span class="badge bg-success text-uppercase fw-semibold mb-1">Selected Job</span>
                                    <h6 class="mb-0 fw-bold text-dark"><asp:Literal ID="litSelectedJobDetails" runat="server"></asp:Literal></h6>
                                </div>
                                <asp:Button ID="btnClearJob" runat="server" Text="Change Job" CssClass="btn btn-sm btn-outline-danger" OnClick="btnClearJob_Click" />
                            </div>
                        </asp:Panel>

                        <!-- Job GridView -->
                        <asp:Panel ID="pnlJobsGrid" runat="server" CssClass="table-responsive">
                            <asp:GridView ID="gvJobs" runat="server" AutoGenerateColumns="False" 
                                CssClass="table table-hover align-middle mb-0" ClientIDMode="Static"
                                DataKeyNames="jobID" OnRowCommand="gvJobs_RowCommand"
                                GridLines="None"
                                AllowPaging="True" PageSize="5" OnPageIndexChanging="gvJobs_PageIndexChanging"
                                EmptyDataText="No active jobs found matching your criteria.">
                                <HeaderStyle CssClass="table-custom-header text-uppercase fw-bold" />
                                <Columns>
                                    <asp:BoundField DataField="jobID" HeaderText="Job ID" ItemStyle-Width="90px" />
                                    <asp:BoundField DataField="ClientName" HeaderText="Client Name" />
                                    <asp:BoundField DataField="siteAddress" HeaderText="Site Location" />
                                    <asp:BoundField DataField="startDate" HeaderText="Start Date" DataFormatString="{0:yyyy-MM-dd}" />
                                    <asp:TemplateField HeaderText="Action" ItemStyle-Width="120px" ItemStyle-CssClass="text-end">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="btnSelectJob" runat="server" CommandName="SelectJob" 
                                                CommandArgument='<%# Eval("jobID") %>' CssClass="btn btn-sm btn-outline-success fw-semibold">
                                                SELECT &rarr;
                                            </asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>

                                <PagerTemplate>
                                    <div class="pager-wrapper">
                                        <div class="page-indicator">
                                            Page <strong><%# gvJobs.PageIndex + 1 %></strong> of <strong><%# gvJobs.PageCount %></strong>
                                        </div>
                                        <div>
                                            <asp:LinkButton ID="btnPrevJob" runat="server" CommandName="Page" CommandArgument="Prev"
                                                CssClass='<%# "pager-btn me-1 " + (gvJobs.PageIndex == 0 ? "disabled" : "") %>'>
                                                &larr; Previous
                                            </asp:LinkButton>
                                            <asp:LinkButton ID="btnNextJob" runat="server" CommandName="Page" CommandArgument="Next"
                                                CssClass='<%# "pager-btn " + (gvJobs.PageIndex == gvJobs.PageCount - 1 ? "disabled" : "") %>'>
                                                Next &rarr;
                                            </asp:LinkButton>
                                        </div>
                                    </div>
                                </PagerTemplate>
                            </asp:GridView>
                        </asp:Panel>
                    </div>
                </div>

                <!-- STEP 2: Select Staff Member -->
                <div class="card custom-card shadow-sm border-0 mb-4">
                    <div class="card-body p-4 p-bottom-0">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <h5 class="fw-bold mb-0 text-dark"><span class="badge bg-secondary me-2">Step 2</span> Select Staff Member & Assign</h5>
                            <div class="input-group input-group-sm" style="max-width: 280px;">
                                <asp:TextBox ID="txtSearchStaff" runat="server" CssClass="form-control" Placeholder="Search staff name or role..." AutoPostBack="true" OnTextChanged="txtSearchStaff_TextChanged" onkeyup="triggerPostBackOnType(this)"></asp:TextBox>
                                <asp:Button ID="btnSearchStaff" runat="server" Text="Search" CssClass="btn btn-outline-secondary" OnClick="btnSearchStaff_Click" />
                            </div>
                        </div>

                        <!-- Staff GridView -->
                        <div class="table-responsive">
                            <asp:GridView ID="gvStaff" runat="server" AutoGenerateColumns="False" 
                                CssClass="table table-hover align-middle mb-0" ClientIDMode="Static"
                                DataKeyNames="staffID" OnRowCommand="gvStaff_RowCommand"
                                GridLines="None"
                                AllowPaging="True" PageSize="5" OnPageIndexChanging="gvStaff_PageIndexChanging"
                                EmptyDataText="No active operational staff members found matching your criteria.">
                                <HeaderStyle CssClass="table-custom-header text-uppercase fw-bold" />
                                <Columns>
                                    <asp:TemplateField HeaderText="Staff Member">
                                        <ItemTemplate>
                                            <div class="fw-bold text-dark"><%# Eval("firstName") %> <%# Eval("lastName") %></div>
                                            <div class="text-muted small"><%# Eval("emailAddress") %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Role">
                                        <ItemTemplate>
                                            <span class="badge bg-light text-dark border"><%# Eval("roleName") %></span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="contactNumber" HeaderText="Contact Number" />
                                    <asp:TemplateField HeaderText="Action" ItemStyle-Width="150px" ItemStyle-CssClass="text-end">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="btnAssignStaff" runat="server" CommandName="AssignStaffMember" 
                                                CommandArgument='<%# Eval("staffID") %>' CssClass="btn btn-sm btn-success px-3 fw-semibold" style="background-color: #1e5631;">
                                                <i class="bi bi-person-plus me-1"></i> Assign
                                            </asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>

                                <PagerTemplate>
                                    <div class="pager-wrapper">
                                        <div class="page-indicator">
                                            Page <strong><%# gvStaff.PageIndex + 1 %></strong> of <strong><%# gvStaff.PageCount %></strong>
                                        </div>
                                        <div>
                                            <asp:LinkButton ID="btnPrevStaff" runat="server" CommandName="Page" CommandArgument="Prev"
                                                CssClass='<%# "pager-btn me-1 " + (gvStaff.PageIndex == 0 ? "disabled" : "") %>'>
                                                &larr; Previous
                                            </asp:LinkButton>
                                            <asp:LinkButton ID="btnNextStaff" runat="server" CommandName="Page" CommandArgument="Next"
                                                CssClass='<%# "pager-btn " + (gvStaff.PageIndex == gvStaff.PageCount - 1 ? "disabled" : "") %>'>
                                                Next &rarr;
                                            </asp:LinkButton>
                                        </div>
                                    </div>
                                </PagerTemplate>
                            </asp:GridView>
                        </div>
                    </div>
                </div>

            </ContentTemplate>
        </asp:UpdatePanel>
    </div>

    <!-- Debounce script for type-as-you-go postbacks -->
    <script type="text/javascript">
        var searchTimer;
        function triggerPostBackOnType(element) {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () {
                __doPostBack(element.name, '');
            }, 300);
        }
    </script>
</asp:Content>