<%@ Page Title="View Asset Allocation" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewAssetAllocation.aspx.cs" Inherits="GiantGroupM3.ViewAssetAllocation" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/giant-ui.css") %>?v=1" />

    <div class="gg-page">
        <header class="gg-page-head">
            <span class="gg-label"><i class="bi bi-tools"></i>Allocation</span>
            <h1>Asset Allocations</h1>
            <p>Overview of equipment and assets assigned to active and historical jobs.</p>
        </header>

        <asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
            <ContentTemplate>

                <!-- Search (searches every page of results, not just the visible one) -->
                <div class="gg-search-wrap mb-4">
                    <div class="gg-search-box">
                        <i class="bi bi-search"></i>
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="gg-input"
                            ClientIDMode="Static" AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"
                            onkeyup="triggerPostBackOnType(this)" Placeholder="Search by asset, client, address..."></asp:TextBox>
                    </div>
                    <asp:LinkButton ID="btnClear" runat="server" OnClick="btnClear_Click" CausesValidation="false" CssClass="gg-btn gg-btn-outline gg-btn-sm">
                        <i class="bi bi-x-lg"></i> Clear
                    </asp:LinkButton>
                </div>

                <div class="gg-panel">
                    <div class="gg-table-wrap">
                        <asp:GridView ID="gvAssetAssignments" runat="server" AutoGenerateColumns="False"
                            CssClass="table gg-table" ClientIDMode="Static"
                            GridLines="None"
                            AllowPaging="True"
                            PageSize="10"
                            OnPageIndexChanging="gvAssetAssignments_PageIndexChanging">

                            <EmptyDataTemplate>
                                <div class="gg-empty">
                                    <span class="gg-tile is-lg"><i class="bi bi-tools"></i></span>
                                    <h3>No asset allocations found</h3>
                                    <p>Nothing matches your search yet. Try different words, or clear the search to see everything.</p>
                                </div>
                            </EmptyDataTemplate>

                            <Columns>
                                <asp:BoundField DataField="AssetName" HeaderText="Asset Name" SortExpression="AssetName" />
                                <asp:BoundField DataField="SiteAddress" HeaderText="Site Address" SortExpression="SiteAddress" />
                                <asp:BoundField DataField="ClientName" HeaderText="Client Name" SortExpression="ClientName" />

                                <asp:TemplateField HeaderText="Ownership Status">
                                    <ItemTemplate>
                                        <span class='gg-chip <%# Eval("AssetSource").ToString() == "Owned Equipment" ? "is-done" : "is-pending" %>'>
                                            <%#: Eval("AssetSource") %>
                                        </span>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:BoundField DataField="assignmentDate" HeaderText="Assignment Date" DataFormatString="{0:yyyy-MM-dd}" SortExpression="assignmentDate" />

                                <asp:TemplateField HeaderText="Return Date">
                                    <ItemTemplate>
                                        <%# Eval("ReturnDate") != DBNull.Value && Eval("ReturnDate") != null
                                            ? "<span class='gg-chip is-pending'><i class='bi bi-calendar-event'></i>" + Convert.ToDateTime(Eval("ReturnDate")).ToString("yyyy-MM-dd") + "</span>"
                                            : "<span class='gg-sub'>N/A</span>" %>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>

                            <PagerTemplate>
                                <div class="gg-pager">
                                    <div class="gg-pager-info">
                                        Page <strong><%# gvAssetAssignments.PageIndex + 1 %></strong> of <strong><%# gvAssetAssignments.PageCount %></strong>
                                    </div>
                                    <div>
                                        <asp:LinkButton ID="btnPrev" runat="server" CommandName="Page" CommandArgument="Prev"
                                            CssClass='<%# "gg-pager-btn me-1 " + (gvAssetAssignments.PageIndex == 0 ? "disabled" : "") %>'>
                                            <i class="bi bi-arrow-left"></i> Previous
                                        </asp:LinkButton>
                                        <asp:LinkButton ID="btnNext" runat="server" CommandName="Page" CommandArgument="Next"
                                            CssClass='<%# "gg-pager-btn " + (gvAssetAssignments.PageIndex == gvAssetAssignments.PageCount - 1 ? "disabled" : "") %>'>
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

    <!-- Debounced type-ahead search -->
    <script type="text/javascript">
        var searchTimer;
        function triggerPostBackOnType(element) {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () { __doPostBack(element.name, ''); }, 350);
        }
    </script>
</asp:Content>
