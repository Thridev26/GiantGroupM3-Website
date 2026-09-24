<%@ Page Title="View Staff Allocation" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewStaffAllocation.aspx.cs" Inherits="GiantGroupM3.ViewStaffAllocation" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        /* Green Accent Theme Variables */
        :root {
            --brand-green: #2D7A58;
            --brand-green-hover: #236146;
            --brand-green-light: #EBF4F0;
        }

        /* Page Heading Styling */
        .page-header-title {
            color: var(--brand-green);
            font-weight: 700;
            letter-spacing: -0.5px;
        }

        .header-accent-bar {
            width: 45px;
            height: 4px;
            background-color: var(--brand-green);
            border-radius: 2px;
        }

        /* Search Control Styling */
        .search-container {
            max-width: 420px;
        }

        .custom-search-group {
            display: flex;
            align-items: center;
            border: 1.5px solid #dcdcd6;
            border-radius: 8px;
            background-color: #ffffff;
            overflow: hidden;
            transition: border-color 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
        }

        .custom-search-group:focus-within {
            border-color: var(--brand-green);
            box-shadow: 0 0 0 0.2rem rgba(45, 122, 88, 0.15);
        }

        .custom-search-icon {
            padding: 10px 0 10px 14px;
            color: var(--brand-green);
            background: transparent;
        }

        .custom-search-input {
            border: none !important;
            box-shadow: none !important;
            padding: 10px 14px 10px 10px;
            font-size: 0.9rem;
            background: transparent;
            width: 100%;
        }

        .custom-search-input:focus {
            outline: none;
        }

        /* Table & Card Custom Theme */
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

        /* Status Badge Colors */
        .badge-status {
            padding: 0.35em 0.65em;
            font-size: 0.78rem;
            font-weight: 600;
            border-radius: 20px;
            text-transform: capitalize;
            display: inline-block;
        }
        .status-completed { background-color: #d1e7dd; color: #0f5132; }
        .status-pending { background-color: #fff3cd; color: #664d03; }
        .status-inprogress { background-color: #cff4fc; color: #055160; }
        .status-archived { background-color: #e2e3e5; color: #41464b; }
        .status-default { background-color: #f8f9fa; color: #212529; border: 1px solid #dee2e6; }
        
        /* Pagination Controls Styling */
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

    <div class="container mt-4">
        <!-- Styled Header Block -->
        <div class="mb-4">
            <h2 class="page-header-title mb-1"><i class="fas fa-users me-2"></i>Staff Allocations</h2>
            <div class="header-accent-bar mb-2"></div>
            <p class="text-secondary mb-0">Overview of staff assignments, job details, and site locations.</p>
        </div>

        <!-- Search Bar -->
        <div class="search-container mb-4">
            <div class="custom-search-group shadow-sm">
                <div class="custom-search-icon">
                    <i class="fas fa-search"></i>
                </div>
                <asp:TextBox ID="txtSearch" runat="server" CssClass="custom-search-input" 
                    ClientIDMode="Static" onkeyup="filterStaffLive()" Placeholder="Search by staff, site, status..."></asp:TextBox>
            </div>
        </div>

        <!-- Custom Card Wrapper -->
        <div class="card custom-card shadow-sm border-0 mb-4">
            <div class="card-body p-0">
                <div class="table-responsive">
                    <asp:GridView ID="gvStaffAssignments" runat="server" AutoGenerateColumns="False" 
                        CssClass="table table-hover align-middle mb-0" ClientIDMode="Static"
                        GridLines="None"
                        AllowPaging="True" 
                        PageSize="10" 
                        OnPageIndexChanging="gvStaffAssignments_PageIndexChanging">
                        
                        <HeaderStyle CssClass="table-custom-header text-uppercase fw-bold" />

                        <EmptyDataTemplate>
                            <div class="text-center py-5 px-4 my-2 rounded-4" style="background-color: #fdfdfd; border: 1px solid #e2e2dc;">
                                <div class="mb-3">
                                    <span style="font-size: 3.5rem;">📋</span>
                                </div>
                                <h4 class="fw-bold text-dark mb-2">No Staff Assigned Yet</h4>
                                <p class="text-muted mb-0 mx-auto" style="max-width: 480px;">
                                    There are currently no staff members allocated to active jobs in the system.
                                </p>
                            </div>
                        </EmptyDataTemplate>
                        
                        <PagerTemplate>
                            <div class="pager-wrapper">
                                <div class="page-indicator">
                                    Page <strong><%# gvStaffAssignments.PageIndex + 1 %></strong> of <strong><%# gvStaffAssignments.PageCount %></strong>
                                </div>
                                <div>
                                    <asp:LinkButton ID="btnPrev" runat="server" CommandName="Page" CommandArgument="Prev"
                                        CssClass='<%# "pager-btn me-1 " + (gvStaffAssignments.PageIndex == 0 ? "disabled" : "") %>'>
                                        &larr; Previous
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="btnNext" runat="server" CommandName="Page" CommandArgument="Next"
                                        CssClass='<%# "pager-btn " + (gvStaffAssignments.PageIndex == gvStaffAssignments.PageCount - 1 ? "disabled" : "") %>'>
                                        Next &rarr;
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
                                    <span class='<%# GetStatusBadgeCss(Eval("jobStatus")) %>'>
                                        <%# Eval("jobStatus") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </div>

    <!-- Live Client-Side Filtering Script -->
    <script type="text/javascript">
        function filterStaffLive() {
            var input = document.getElementById("txtSearch");
            var filter = input.value.toLowerCase();
            var grid = document.getElementById("gvStaffAssignments");

            if (!grid) return;

            var rows = grid.getElementsByTagName("tr");

            for (var i = 1; i < rows.length; i++) {
                var row = rows[i];

                if (row.getElementsByClassName("pager-wrapper").length > 0) {
                    continue;
                }

                var textContent = row.textContent || row.innerText;

                if (textContent.toLowerCase().indexOf(filter) > -1) {
                    row.style.display = "";
                } else {
                    row.style.display = "none";
                }
            }
        }
    </script>
</asp:Content>