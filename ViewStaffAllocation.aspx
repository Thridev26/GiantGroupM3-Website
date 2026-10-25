<%@ Page Title="View Staff Allocation" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewStaffAllocation.aspx.cs" Inherits="GiantGroupM3.ViewStaffAllocation" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/giant-ui.css") %>?v=1" />

    <style>
        .status-completed { background: var(--gg-accent-soft); color: var(--gg-accent); }
        .status-inprogress { background: var(--gg-blue-soft); color: var(--gg-blue); }
        .status-pending { background: var(--gg-amber-soft); color: var(--gg-amber); }
        .status-archived, .status-default { background: var(--gg-surface-2); color: var(--gg-muted); }
    </style>

    <div class="gg-page">
        <header class="gg-page-head">
            <span class="gg-label"><i class="bi bi-people"></i>Allocation</span>
            <h1>Staff Allocations</h1>
            <p>Overview of staff assignments, job details, and site locations.</p>
        </header>

        <asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
            <ContentTemplate>

                <!-- Search (searches every page of results, not just the visible one) -->
                <div class="gg-search-wrap mb-4">
                    <div class="gg-search-box">
                        <i class="bi bi-search"></i>
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="gg-input"
                            ClientIDMode="Static" AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"
                            onkeyup="triggerPostBackOnType(this)" Placeholder="Search by staff, site, status..."></asp:TextBox>
                    </div>
                    <asp:LinkButton ID="btnClear" runat="server" OnClick="btnClear_Click" CausesValidation="false" CssClass="gg-btn gg-btn-outline gg-btn-sm">
                        <i class="bi bi-x-lg"></i> Clear
                    </asp:LinkButton>
                </div>

                <div class="gg-panel">
                    <div class="gg-table-wrap">
                        <asp:GridView ID="gvStaffAssignments" runat="server" AutoGenerateColumns="False"
                            CssClass="table gg-table" ClientIDMode="Static"
                            GridLines="None"
                            AllowPaging="True"
                            PageSize="10"
                            OnPageIndexChanging="gvStaffAssignments_PageIndexChanging">

                            <EmptyDataTemplate>
                                <div class="gg-empty">
                                    <span class="gg-tile is-lg"><i class="bi bi-people"></i></span>
                                    <h3>No staff assigned yet</h3>
                                    <p>There are currently no staff members allocated to jobs that match your view. Try clearing the search.</p>
                                </div>
                            </EmptyDataTemplate>

                            <PagerTemplate>
                                <div class="gg-pager">
                                    <div class="gg-pager-info">
                                        Page <strong><%# gvStaffAssignments.PageIndex + 1 %></strong> of <strong><%# gvStaffAssignments.PageCount %></strong>
                                    </div>
                                    <div>
                                        <asp:LinkButton ID="btnPrev" runat="server" CommandName="Page" CommandArgument="Prev"
                                            CssClass='<%# "gg-pager-btn me-1 " + (gvStaffAssignments.PageIndex == 0 ? "disabled" : "") %>'>
                                            <i class="bi bi-arrow-left"></i> Previous
                                        </asp:LinkButton>
                                        <asp:LinkButton ID="btnNext" runat="server" CommandName="Page" CommandArgument="Next"
                                            CssClass='<%# "gg-pager-btn " + (gvStaffAssignments.PageIndex == gvStaffAssignments.PageCount - 1 ? "disabled" : "") %>'>
                                            Next <i class="bi bi-arrow-right"></i>
                                        </asp:LinkButton>
                                    </div>
                                </div>
                            </PagerTemplate>

                            <Columns>
                                <asp:BoundField DataField="StaffName" HeaderText="Staff Name" SortExpression="StaffName" />
                                <asp:BoundField DataField="SiteAddress" HeaderText="Site Address" SortExpression="SiteAddress" />
                                <asp:BoundField DataField="ClientName" HeaderText="Client Name" SortExpression="ClientName" />
                                <asp:BoundField DataField="startDate" HeaderText="Job Start Date" DataFormatString="{0:yyyy-MM-dd}" SortExpression="startDate" />
                                <asp:BoundField DataField="assignmentDate" HeaderText="Assignment Date" DataFormatString="{0:yyyy-MM-dd}" SortExpression="assignmentDate" />

                                <asp:TemplateField HeaderText="Job Status" SortExpression="jobStatus">
                                    <ItemTemplate>
                                        <span class='gg-chip <%# GetStatusBadgeCss(Eval("jobStatus")) %>'>
                                            <%#: Eval("jobStatus") %>
                                        </span>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>

            </ContentTemplate>
        </asp:UpdatePanel>
    </div>

    <!-- Debounced type-ahead search -->
    <script type="text/javascript">
        var searchTimer;
        function triggerPostBackOnType(element) {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () { __doPostBack(element.name, ''); }, 350);
        }
    </script>
</asp:Content>
