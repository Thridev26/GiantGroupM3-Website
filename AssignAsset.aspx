<%@ Page Title="Assign Asset to Job" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AssignAsset.aspx.cs" Inherits="GiantGroupM3.AssignAsset" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/giant-ui.css") %>?v=1" />

    <style>
        .gg-slotlist { list-style: none; display: flex; flex-wrap: wrap; gap: .5rem; padding: 0; margin: 0; }
        .gg-slotlist li { display: inline-flex; align-items: center; gap: .4rem; padding: .35rem .75rem; border: 1px solid var(--gg-border); border-radius: 999px; background: var(--gg-surface); font-size: .9rem; }
        .gg-slotlist input { accent-color: var(--gg-700); }
        .gg-slotlist label { margin: 0; cursor: pointer; }
    </style>

    <div class="gg-page">
        <header class="gg-page-head">
            <span class="gg-label"><i class="bi bi-tools"></i>Allocation</span>
            <h1>Assign Asset to Job</h1>
            <p>Select a job, choose its date and time slots, then assign assets that are free in those slots</p>
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

                <!-- STEP 2: Date & time slots -->
                <asp:Panel ID="pnlSchedule" runat="server" Visible="false" CssClass="gg-panel mb-4">
                    <div class="gg-panel-body">
                        <div class="gg-panel-head">
                            <h2 class="gg-step-title"><span class="gg-step-n">2</span> Choose Date &amp; Time Slots</h2>
                        </div>
                        <p class="gg-muted small">An asset already booked on another job in any chosen slot on that date is locked. Each booking keeps its own slots, and back-to-back slots are fine: someone booked 08:00&ndash;09:00 can still take 09:00&ndash;10:00 on another job.</p>
                        <div class="row g-3 align-items-start">
                            <div class="col-md-3">
                                <asp:Label runat="server" AssociatedControlID="txtWorkDate" CssClass="gg-sub d-block mb-1" Text="Work date" />
                                <asp:TextBox ID="txtWorkDate" runat="server" TextMode="Date" CssClass="gg-input" />
                            </div>
                            <div class="col-md-9">
                                <span class="gg-sub d-block mb-1">Time slots to book (this assignment only)</span>
                               <asp:RadioButtonList ID="cblSlots" runat="server" RepeatLayout="UnorderedList"
    CssClass="gg-slotlist" DataTextField="Label" DataValueField="timeSlotID" />
                            </div>
                        </div>
                        <div class="d-flex align-items-center flex-wrap gap-3 mt-3">
                            <asp:Button ID="btnApplySchedule" runat="server" Text="Check availability" CssClass="gg-btn gg-btn-primary gg-btn-sm" OnClick="btnApplySchedule_Click" />
                            <asp:Label ID="lblScheduleSummary" runat="server" CssClass="gg-muted small" />
                        </div>
                    </div>
                </asp:Panel>

                <!-- STEP 3: Select Asset (Owned or Hired) -->
                <div class="gg-panel mb-4">
                    <div class="gg-panel-body pb-2">
                        <div class="gg-panel-head">
                            <h2 class="gg-step-title"><span class="gg-step-n">3</span> Select Available Asset &amp; Assign</h2>
                            <div class="gg-search-box">
                                <i class="bi bi-search"></i>
                                <asp:TextBox ID="txtSearchAssets" runat="server" CssClass="gg-input" Placeholder="Search type, serial or supplier..." AutoPostBack="true" OnTextChanged="txtSearchAssets_TextChanged" onkeyup="triggerPostBackOnType(this)"></asp:TextBox>
                            </div>
                            <asp:Button ID="btnSearchAssets" runat="server" Text="Search" CssClass="gg-btn gg-btn-outline gg-btn-sm" OnClick="btnSearchAssets_Click" />
                        </div>
                        <p class="gg-muted small mb-0">Assign buttons unlock once a job, date and time slots are set and the asset is free in those slots.</p>
                        <div class="mt-2">
                            <asp:CheckBox ID="chkHideBooked" runat="server" AutoPostBack="true" OnCheckedChanged="chkHideBooked_CheckedChanged" Text="&nbsp;Hide assets that are booked in these slots" CssClass="gg-muted small" />
                        </div>

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

                                <asp:TemplateField HeaderText="Availability">
                                    <ItemTemplate><%# Eval("StatusHtml") %></ItemTemplate>
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
