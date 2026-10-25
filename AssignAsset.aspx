<%@ Page Title="Assign Asset to Job" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AssignAsset.aspx.cs" Inherits="GiantGroupM3.AssignAsset" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/giant-ui.css") %>?v=1" />

    <div class="gg-page">
        <header class="gg-page-head">
            <span class="gg-label"><i class="bi bi-tools"></i>Allocation</span>
            <h1>Assign Asset to Job</h1>
            <p>Select a Job and then assign Assets</p>
        </header>

        <asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
            <ContentTemplate>

                <!-- Feedback Alert -->
                <asp:Label ID="lblMessage" runat="server" CssClass="d-none" role="alert"></asp:Label>

                <!-- Hidden Trackers (the server keeps the trusted copy in ViewState) -->
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

                <!-- STEP 2: Select Asset (Owned or Hired) -->
                <div class="gg-panel mb-4">
                    <div class="gg-panel-body pb-2">
                        <div class="gg-panel-head">
                            <h2 class="gg-step-title"><span class="gg-step-n">2</span> Select Available Asset &amp; Assign</h2>
                            <div class="gg-search-box">
                                <i class="bi bi-search"></i>
                                <asp:TextBox ID="txtSearchAssets" runat="server" CssClass="gg-input" Placeholder="Search type, serial or supplier..." AutoPostBack="true" OnTextChanged="txtSearchAssets_TextChanged" onkeyup="triggerPostBackOnType(this)"></asp:TextBox>
                            </div>
                            <asp:Button ID="btnSearchAssets" runat="server" Text="Search" CssClass="gg-btn gg-btn-outline gg-btn-sm" OnClick="btnSearchAssets_Click" />
                        </div>
                        <p class="gg-muted small mb-0">Assign buttons unlock once you have selected a job in step 1.</p>
                    </div>

                    <!-- Combined Assets GridView -->
                    <div class="gg-table-wrap">
                        <asp:GridView ID="gvAssets" runat="server" AutoGenerateColumns="False"
                            CssClass="table gg-table" ClientIDMode="Static"
                            OnRowCommand="gvAssets_RowCommand" OnRowDataBound="gvAssets_RowDataBound"
                            GridLines="None"
                            AllowPaging="True" PageSize="5" OnPageIndexChanging="gvAssets_PageIndexChanging"
                            EmptyDataText="No available assets found matching your criteria.">
                            <EmptyDataRowStyle CssClass="gg-empty" />
                            <Columns>
                                <asp:TemplateField HeaderText="Asset Source" ItemStyle-Width="120px">
                                    <ItemTemplate>
                                        <%# Eval("SourceType").ToString() == "Owned"
                                            ? "<span class='gg-chip is-done'><i class='bi bi-box-seam'></i>Owned</span>"
                                            : "<span class='gg-chip is-active'><i class='bi bi-truck'></i>Hired</span>" %>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Equipment Type & Identification">
                                    <ItemTemplate>
                                        <div class="fw-bold"><%#: Eval("EquipmentType") %></div>
                                        <div class="gg-sub">Identifier: <%#: Eval("Identifier") %></div>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Owner / Supplier">
                                    <ItemTemplate>
                                        <span class="fw-medium"><%#: Eval("SupplierOrOwner") %></span>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="140px" ItemStyle-CssClass="text-end">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnAssignAsset" runat="server" CommandName="AssignAssetToJob"
                                            OnClientClick="return confirm('Assign this asset to the selected job?');"
                                            CommandArgument='<%# Eval("SourceType") + ":" + Eval("AssetID") %>' CssClass="gg-btn gg-btn-primary gg-btn-sm">
                                            <i class="bi bi-plus-lg"></i> Assign
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>

                            <PagerTemplate>
                                <div class="gg-pager">
                                    <div class="gg-pager-info">
                                        Page <strong><%# gvAssets.PageIndex + 1 %></strong> of <strong><%# gvAssets.PageCount %></strong>
                                    </div>
                                    <div>
                                        <asp:LinkButton ID="btnPrevAsset" runat="server" CommandName="Page" CommandArgument="Prev"
                                            CssClass='<%# "gg-pager-btn me-1 " + (gvAssets.PageIndex == 0 ? "disabled" : "") %>'>
                                            <i class="bi bi-arrow-left"></i> Previous
                                        </asp:LinkButton>
                                        <asp:LinkButton ID="btnNextAsset" runat="server" CommandName="Page" CommandArgument="Next"
                                            CssClass='<%# "gg-pager-btn " + (gvAssets.PageIndex == gvAssets.PageCount - 1 ? "disabled" : "") %>'>
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
