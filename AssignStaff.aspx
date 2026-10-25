<%@ Page Title="Assign Staff to Job" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AssignStaff.aspx.cs" Inherits="GiantGroupM3.AssignStaff" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/giant-ui.css") %>?v=1" />

    <div class="gg-page">
        <header class="gg-page-head">
            <span class="gg-label"><i class="bi bi-person-plus"></i>Allocation</span>
            <h1>Assign Staff to Job</h1>
            <p>Select a Job and then assign Staff members</p>
        </header>

        <asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
            <ContentTemplate>

                <!-- Feedback Alert -->
                <asp:Label ID="lblMessage" runat="server" CssClass="d-none" role="alert"></asp:Label>

                <!-- Hidden ID tracker (the server keeps the trusted copy in ViewState) -->
                <asp:HiddenField ID="hfSelectedJobID" runat="server" />

                <!-- STEP 1: Select Active Job -->
                <div class="gg-panel mb-4">
                    <div class="gg-panel-body pb-2">
                        <div class="gg-panel-head">
                            <h2 class="gg-step-title"><span class="gg-step-n">1</span> Select Active Job</h2>
                            <div class="gg-search-box">
                                <i class="bi bi-search"></i>
                                <asp:TextBox ID="txtSearchJobs" runat="server" CssClass="gg-input" Placeholder="Search job, client or site..." AutoPostBack="true" OnTextChanged="txtSearchJobs_TextChanged" onkeyup="triggerPostBackOnType(this)"></asp:TextBox>
                            </div>
                            <asp:Button ID="btnSearchJobs" runat="server" Text="Search" CssClass="gg-btn gg-btn-outline gg-btn-sm" OnClick="btnSearchJobs_Click" />
                        </div>

                        <!-- Selected Job Banner -->
                        <asp:Panel ID="pnlSelectedJob" runat="server" CssClass="gg-selected d-none">
                            <div>
                                <small><i class="bi bi-check-circle-fill me-1"></i>Selected job</small><br />
                                <strong><asp:Literal ID="litSelectedJobDetails" runat="server"></asp:Literal></strong>
                            </div>
                            <asp:Button ID="btnClearJob" runat="server" Text="Change job" CssClass="gg-btn gg-btn-outline gg-btn-sm" OnClick="btnClearJob_Click" />
                        </asp:Panel>
                    </div>

                    <!-- Job GridView -->
                    <asp:Panel ID="pnlJobsGrid" runat="server" CssClass="gg-table-wrap">
                        <asp:GridView ID="gvJobs" runat="server" AutoGenerateColumns="False"
                            CssClass="table gg-table" ClientIDMode="Static"
                            DataKeyNames="jobID" OnRowCommand="gvJobs_RowCommand"
                            GridLines="None"
                            AllowPaging="True" PageSize="5" OnPageIndexChanging="gvJobs_PageIndexChanging"
                            EmptyDataText="No active jobs found matching your criteria.">
                            <EmptyDataRowStyle CssClass="gg-empty" />
                            <Columns>
                                <asp:BoundField DataField="jobID" HeaderText="Job ID" ItemStyle-Width="90px" />
                                <asp:BoundField DataField="ClientName" HeaderText="Client Name" />
                                <asp:BoundField DataField="siteAddress" HeaderText="Site Location" />
                                <asp:BoundField DataField="startDate" HeaderText="Start Date" DataFormatString="{0:yyyy-MM-dd}" />
                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="120px" ItemStyle-CssClass="text-end">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnSelectJob" runat="server" CommandName="SelectJob"
                                            CommandArgument='<%# Eval("jobID") %>' CssClass="gg-btn gg-btn-outline gg-btn-sm">
                                            Select <i class="bi bi-arrow-right"></i>
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>

                            <PagerTemplate>
                                <div class="gg-pager">
                                    <div class="gg-pager-info">
                                        Page <strong><%# gvJobs.PageIndex + 1 %></strong> of <strong><%# gvJobs.PageCount %></strong>
                                    </div>
                                    <div>
                                        <asp:LinkButton ID="btnPrevJob" runat="server" CommandName="Page" CommandArgument="Prev"
                                            CssClass='<%# "gg-pager-btn me-1 " + (gvJobs.PageIndex == 0 ? "disabled" : "") %>'>
                                            <i class="bi bi-arrow-left"></i> Previous
                                        </asp:LinkButton>
                                        <asp:LinkButton ID="btnNextJob" runat="server" CommandName="Page" CommandArgument="Next"
                                            CssClass='<%# "gg-pager-btn " + (gvJobs.PageIndex == gvJobs.PageCount - 1 ? "disabled" : "") %>'>
                                            Next <i class="bi bi-arrow-right"></i>
                                        </asp:LinkButton>
                                    </div>
                                </div>
                            </PagerTemplate>
                        </asp:GridView>
                    </asp:Panel>
                </div>

                <!-- STEP 2: Select Staff Member -->
                <div class="gg-panel mb-4">
                    <div class="gg-panel-body pb-2">
                        <div class="gg-panel-head">
                            <h2 class="gg-step-title"><span class="gg-step-n">2</span> Select Staff Member &amp; Assign</h2>
                            <div class="gg-search-box">
                                <i class="bi bi-search"></i>
                                <asp:TextBox ID="txtSearchStaff" runat="server" CssClass="gg-input" Placeholder="Search staff name or role..." AutoPostBack="true" OnTextChanged="txtSearchStaff_TextChanged" onkeyup="triggerPostBackOnType(this)"></asp:TextBox>
                            </div>
                            <asp:Button ID="btnSearchStaff" runat="server" Text="Search" CssClass="gg-btn gg-btn-outline gg-btn-sm" OnClick="btnSearchStaff_Click" />
                        </div>
                        <p class="gg-muted small mb-0">Assign buttons unlock once you have selected a job in step 1.</p>
                    </div>

                    <!-- Staff GridView -->
                    <div class="gg-table-wrap">
                        <asp:GridView ID="gvStaff" runat="server" AutoGenerateColumns="False"
                            CssClass="table gg-table" ClientIDMode="Static"
                            DataKeyNames="staffID" OnRowCommand="gvStaff_RowCommand" OnRowDataBound="gvStaff_RowDataBound"
                            GridLines="None"
                            AllowPaging="True" PageSize="5" OnPageIndexChanging="gvStaff_PageIndexChanging"
                            EmptyDataText="No active operational staff members found matching your criteria.">
                            <EmptyDataRowStyle CssClass="gg-empty" />
                            <Columns>
                                <asp:TemplateField HeaderText="Staff Member">
                                    <ItemTemplate>
                                        <div class="fw-bold"><%#: Eval("firstName") %> <%#: Eval("lastName") %></div>
                                        <div class="gg-sub"><%#: Eval("emailAddress") %></div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Role">
                                    <ItemTemplate>
                                        <span class="gg-chip"><%#: Eval("roleName") %></span>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="contactNumber" HeaderText="Contact Number" />
                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="150px" ItemStyle-CssClass="text-end">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnAssignStaff" runat="server" CommandName="AssignStaffMember"
                                            OnClientClick="return confirm('Assign this staff member to the selected job?');"
                                            CommandArgument='<%# Eval("staffID") %>' CssClass="gg-btn gg-btn-primary gg-btn-sm">
                                            <i class="bi bi-person-plus"></i> Assign
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>

                            <PagerTemplate>
                                <div class="gg-pager">
                                    <div class="gg-pager-info">
                                        Page <strong><%# gvStaff.PageIndex + 1 %></strong> of <strong><%# gvStaff.PageCount %></strong>
                                    </div>
                                    <div>
                                        <asp:LinkButton ID="btnPrevStaff" runat="server" CommandName="Page" CommandArgument="Prev"
                                            CssClass='<%# "gg-pager-btn me-1 " + (gvStaff.PageIndex == 0 ? "disabled" : "") %>'>
                                            <i class="bi bi-arrow-left"></i> Previous
                                        </asp:LinkButton>
                                        <asp:LinkButton ID="btnNextStaff" runat="server" CommandName="Page" CommandArgument="Next"
                                            CssClass='<%# "gg-pager-btn " + (gvStaff.PageIndex == gvStaff.PageCount - 1 ? "disabled" : "") %>'>
                                            Next <i class="bi bi-arrow-right"></i>
                                        </asp:LinkButton>
                                    </div>
                                </div>
                            </PagerTemplate>
                        </asp:GridView>
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
