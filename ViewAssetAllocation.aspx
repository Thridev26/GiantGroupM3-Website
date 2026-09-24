<%@ Page Title="View Asset Allocation" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewAssetAllocation.aspx.cs" Inherits="GiantGroupM3.ViewAssetAllocation" %>

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
            border-color: #2D7A58;
            box-shadow: 0 0 0 0.2rem rgba(45, 122, 88, 0.15);
        }

        .custom-search-icon {
            padding: 10px 0 10px 14px;
            color: #2D7A58;
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

        /* Table & Card Custom Theme matching Quotes Page */
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

        /* Status Badge Colors */
        .badge-owned {
            background-color: #eef7ee;
            color: #2e7d32;
            border: 1px solid #c8e6c9;
        }

        .badge-hired {
            background-color: #fff8e1;
            color: #b78103;
            border: 1px solid #ffe082;
        }

        /* Return Date Badge Styling */
        .badge-return-date {
            background-color: #fff3cd;
            color: #856404;
            border: 1px solid #ffeeba;
        }
    </style>

    <div class="container mt-4">
        <!-- Styled Header Block -->
        <div class="mb-4">
            <h2 class="page-header-title mb-1"><i class="fas fa-tools me-2"></i>Asset Allocations</h2>
            <div class="header-accent-bar mb-2"></div>
            <p class="text-secondary mb-0">Overview of equipment and assets assigned to active and historical jobs.</p>
        </div>

        <!-- Styled Green Search Bar -->
        <div class="search-container mb-4">
            <div class="custom-search-group shadow-sm">
                <div class="custom-search-icon">
                    <i class="fas fa-search"></i>
                </div>
                <asp:TextBox ID="txtSearch" runat="server" CssClass="custom-search-input" 
                    ClientIDMode="Static" onkeyup="filterAssetsLive()" Placeholder="Search by asset, client, address..."></asp:TextBox>
            </div>
        </div>

        <!-- Custom Card Wrapper -->
        <div class="card custom-card shadow-sm border-0 mb-4">
            <div class="card-body p-0">
                <div class="table-responsive">
                    <asp:GridView ID="gvAssetAssignments" runat="server" AutoGenerateColumns="False" 
                        CssClass="table table-hover align-middle mb-0" ClientIDMode="Static"
                        GridLines="None"
                        AllowPaging="True" 
                        PageSize="10" 
                        OnPageIndexChanging="gvAssetAssignments_PageIndexChanging"
                        EmptyDataText="No asset allocations found.">
                        
                        <HeaderStyle CssClass="table-custom-header text-uppercase fw-bold" />
                        
                        <Columns>
                            <asp:BoundField DataField="AssetName" HeaderText="Asset Name" SortExpression="AssetName" />
                            <asp:BoundField DataField="SiteAddress" HeaderText="Site Address" SortExpression="SiteAddress" />
                            <asp:BoundField DataField="ClientName" HeaderText="Client Name" SortExpression="ClientName" />
                            
                            <asp:TemplateField HeaderText="Ownership Status">
                                <ItemTemplate>
                                    <span class='<%# Eval("AssetSource").ToString() == "Owned Equipment" ? "badge badge-owned" : "badge badge-hired" %> px-2 py-1 rounded-pill fw-semibold'>
                                        <%# Eval("AssetSource") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:BoundField DataField="assignmentDate" HeaderText="Assignment Date" DataFormatString="{0:yyyy-MM-dd}" SortExpression="assignmentDate" />

                            <asp:TemplateField HeaderText="Return Date">
                                <ItemTemplate>
                                    <%# Eval("ReturnDate") != DBNull.Value && Eval("ReturnDate") != null 
                                        ? "<span class='badge badge-return-date px-2 py-1 rounded-pill fw-semibold'><i class='far fa-calendar-alt me-1'></i>" + Convert.ToDateTime(Eval("ReturnDate")).ToString("yyyy-MM-dd") + "</span>" 
                                        : "<span class='text-muted small'>N/A</span>" %>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>

                        <PagerTemplate>
                            <div class="pager-wrapper">
                                <div class="page-indicator">
                                    Page <strong><%# gvAssetAssignments.PageIndex + 1 %></strong> of <strong><%# gvAssetAssignments.PageCount %></strong>
                                </div>
                                <div>
                                    <asp:LinkButton ID="btnPrev" runat="server" CommandName="Page" CommandArgument="Prev"
                                        CssClass='<%# "pager-btn me-1 " + (gvAssetAssignments.PageIndex == 0 ? "disabled" : "") %>'>
                                        &larr; Previous
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="btnNext" runat="server" CommandName="Page" CommandArgument="Next"
                                        CssClass='<%# "pager-btn " + (gvAssetAssignments.PageIndex == gvAssetAssignments.PageCount - 1 ? "disabled" : "") %>'>
                                        Next &rarr;
                                    </asp:LinkButton>
                                </div>
                            </div>
                        </PagerTemplate>

                    </asp:GridView>
                </div>
            </div>
        </div>
    </div>

    <!-- Live Client-Side Filtering Script -->
    <script type="text/javascript">
        function filterAssetsLive() {
            var input = document.getElementById("txtSearch");
            var filter = input.value.toLowerCase();
            var grid = document.getElementById("gvAssetAssignments");

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