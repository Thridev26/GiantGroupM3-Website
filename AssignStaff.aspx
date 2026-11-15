<%@ Page Title="Assign Staff to Job" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AssignStaff.aspx.cs" Inherits="GiantGroupM3.AssignStaff" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/giant-ui.css") %>?v=1" />

    <style>
        .gg-slotlist { list-style: none; display: flex; flex-wrap: wrap; gap: .5rem; padding: 0; margin: 0; }
        .gg-slotlist li { display: inline-flex; align-items: center; gap: .4rem; padding: .35rem .75rem; border: 1px solid var(--gg-border); border-radius: 999px; background: var(--gg-surface); font-size: .9rem; }
        .gg-slotlist input { accent-color: var(--gg-700); }
        .gg-slotlist label { margin: 0; cursor: pointer; }
        /* In-page confirmation dialog (replaces the browser's built-in confirm box) */
        .gg-modal-overlay { position: fixed; inset: 0; z-index: 2000; display: flex; align-items: center; justify-content: center; padding: 1rem; background: rgba(8, 20, 14, .55); animation: ggModalFade .15s ease; }
        .gg-modal { width: 100%; max-width: 440px; background: var(--gg-surface); color: var(--gg-text); border: 1px solid var(--gg-border); border-radius: 16px; padding: 1.5rem; box-shadow: 0 24px 60px rgba(0, 0, 0, .35); animation: ggModalPop .18s ease; }
        .gg-modal h3 { font-size: 1.15rem; font-weight: 700; margin: 0 0 .4rem; color: var(--gg-text); }
        .gg-modal p { margin: 0 0 .9rem; color: var(--gg-muted); }
        .gg-modal-details { margin: 0 0 1.1rem; padding: .75rem .9rem; border: 1px solid var(--gg-border); border-radius: 10px; background: var(--gg-bg); font-size: .9rem; }
        .gg-modal-details div + div { margin-top: .6rem; }
        .gg-modal-details span { display: block; font-size: .72rem; text-transform: uppercase; letter-spacing: .04em; color: var(--gg-muted); }
        .gg-modal-actions { display: flex; justify-content: flex-end; gap: .6rem; }
        @keyframes ggModalFade { from { opacity: 0; } to { opacity: 1; } }
        @keyframes ggModalPop { from { opacity: 0; transform: translateY(8px) scale(.98); } to { opacity: 1; transform: none; } }
        @media (prefers-reduced-motion: reduce) { .gg-modal-overlay, .gg-modal { animation: none; } }
    </style>

    <div class="gg-page">
        <header class="gg-page-head">
            <span class="gg-label"><i class="bi bi-person-plus"></i>Allocation</span>
            <h1>Assign Staff to Job</h1>
            <p>Select a job, choose its date and time slots, then assign staff who are free in those slots</p>
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

                <!-- STEP 2: Date & time slots -->
                <asp:Panel ID="pnlSchedule" runat="server" Visible="false" CssClass="gg-panel mb-4">
                    <div class="gg-panel-body">
                        <div class="gg-panel-head">
                            <h2 class="gg-step-title"><span class="gg-step-n">2</span> Choose Date &amp; Time Slots</h2>
                        </div>
                        <p class="gg-muted small">A staff member already booked on another job in any chosen slot on that date is locked. Each booking keeps its own slots, and back-to-back slots are fine: someone booked 08:00&ndash;09:00 can still take 09:00&ndash;10:00 on another job.</p>
                        <div class="row g-3 align-items-start">
                            <div class="col-md-3">
                                <asp:Label runat="server" AssociatedControlID="txtWorkDate" CssClass="gg-sub d-block mb-1" Text="Work date" />
                                <asp:TextBox ID="txtWorkDate" runat="server" TextMode="Date" CssClass="gg-input" />
                            </div>
                            <div class="col-md-9">
                                <span class="gg-sub d-block mb-1">Time slots to book (this assignment only)</span>
                               <asp:CheckBoxList ID="cblSlots" runat="server" RepeatLayout="UnorderedList"
    CssClass="gg-slotlist" DataTextField="Label" DataValueField="timeSlotID" />
                            </div>
                        </div>
                        <div class="d-flex align-items-center flex-wrap gap-3 mt-3">
                            <asp:Button ID="btnApplySchedule" runat="server" Text="Check availability" CssClass="gg-btn gg-btn-primary gg-btn-sm" OnClick="btnApplySchedule_Click" />
                            <asp:Label ID="lblScheduleSummary" runat="server" CssClass="gg-muted small" />
                        </div>
                    </div>
                </asp:Panel>

                <!-- STEP 3: Select Staff Member -->
                <div class="gg-panel mb-4">
                    <div class="gg-panel-body pb-2">
                        <div class="gg-panel-head">
                            <h2 class="gg-step-title"><span class="gg-step-n">3</span> Select Staff Member &amp; Assign</h2>
                            <div class="gg-search-box">
                                <i class="bi bi-search"></i>
                                <asp:TextBox ID="txtSearchStaff" runat="server" CssClass="gg-input" Placeholder="Search staff name or role..." AutoPostBack="true" OnTextChanged="txtSearchStaff_TextChanged" onkeyup="triggerPostBackOnType(this)"></asp:TextBox>
                            </div>
                            <asp:Button ID="btnSearchStaff" runat="server" Text="Search" CssClass="gg-btn gg-btn-outline gg-btn-sm" OnClick="btnSearchStaff_Click" />
                        </div>
                        <p class="gg-muted small mb-0">Assign buttons unlock once a job, date and time slots are set and the person is free in those slots.</p>
                        <div class="mt-2">
                            <asp:CheckBox ID="chkHideBooked" runat="server" AutoPostBack="true" OnCheckedChanged="chkHideBooked_CheckedChanged" Text="&nbsp;Hide staff that are booked in these slots" CssClass="gg-muted small" />
                        </div>

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
                                <asp:TemplateField HeaderText="Availability">
                                    <ItemTemplate><%# Eval("StatusHtml") %></ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="150px" ItemStyle-CssClass="text-end">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnAssignStaff" runat="server" CommandName="AssignStaffMember"
                                            OnClientClick="return ggConfirmAction(this, 'Assign staff member to job?', 'Please confirm this assignment.', 0);"
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

    <!-- Confirmation dialog for the Assign buttons (replaces the browser's built-in confirm box) -->
    <script type="text/javascript">
        function ggConfirmAction(el, title, message, nameCellIndex) {
            // Second pass: the person confirmed in our dialog, so let the original postback run.
            if (el.getAttribute('data-gg-confirmed') === '1') {
                el.removeAttribute('data-gg-confirmed');
                return true;
            }

            var details = [];
            var row = el.closest ? el.closest('tr') : null;
            if (row && row.cells.length > nameCellIndex) {
                var what = (row.cells[nameCellIndex].innerText || '').replace(/\s*[\r\n]+\s*/g, ' \u00B7 ').trim();
                if (what) details.push({ label: 'Selected', value: what });
            }
            var job = document.querySelector('#<%= pnlSelectedJob.ClientID %> strong');
            if (job && job.textContent.trim()) details.push({ label: 'Job', value: job.textContent.trim() });
            var sched = document.getElementById('<%= lblScheduleSummary.ClientID %>');
            if (sched && sched.textContent.trim()) details.push({ label: 'Date & time slots', value: sched.textContent.replace(/^\s*Checking\s*/, '').trim() });

            ggOpenConfirm({
                title: title,
                message: message,
                details: details,
                okText: 'Assign',
                onConfirm: function () {
                    // Run the button's own postback directly (LinkButtons use a javascript: href).
                    var href = el.getAttribute('href') || '';
                    if (href.indexOf('javascript:') === 0) {
                        (new Function(href.substring(11)))();
                    } else {
                        el.setAttribute('data-gg-confirmed', '1');
                        el.click();
                    }
                }
            });
            return false;
        }

        function ggOpenConfirm(opts) {
            if (document.querySelector('.gg-modal-overlay')) return;
            var lastFocus = document.activeElement;

            var overlay = document.createElement('div');
            overlay.className = 'gg-modal-overlay';

            var box = document.createElement('div');
            box.className = 'gg-modal';
            box.setAttribute('role', 'alertdialog');
            box.setAttribute('aria-modal', 'true');
            box.setAttribute('aria-labelledby', 'ggModalTitle');
            box.setAttribute('aria-describedby', 'ggModalMsg');

            var h = document.createElement('h3');
            h.id = 'ggModalTitle';
            h.textContent = opts.title;
            var p = document.createElement('p');
            p.id = 'ggModalMsg';
            p.textContent = opts.message;
            box.appendChild(h);
            box.appendChild(p);

            if (opts.details && opts.details.length) {
                var list = document.createElement('div');
                list.className = 'gg-modal-details';
                opts.details.forEach(function (d) {
                    var item = document.createElement('div');
                    var label = document.createElement('span');
                    label.textContent = d.label;
                    item.appendChild(label);
                    item.appendChild(document.createTextNode(d.value));
                    list.appendChild(item);
                });
                box.appendChild(list);
            }

            var actions = document.createElement('div');
            actions.className = 'gg-modal-actions';
            var cancelBtn = document.createElement('button');
            cancelBtn.type = 'button';
            cancelBtn.className = 'gg-btn gg-btn-outline';
            cancelBtn.textContent = 'Cancel';
            var okBtn = document.createElement('button');
            okBtn.type = 'button';
            okBtn.className = 'gg-btn gg-btn-primary';
            okBtn.textContent = opts.okText || 'OK';
            actions.appendChild(cancelBtn);
            actions.appendChild(okBtn);
            box.appendChild(actions);
            overlay.appendChild(box);

            function close(confirmed) {
                document.removeEventListener('keydown', onKey, true);
                if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
                if (lastFocus && lastFocus.focus && document.body.contains(lastFocus)) lastFocus.focus();
                if (confirmed) opts.onConfirm();
            }
            function onKey(e) {
                if (e.key === 'Escape') { e.preventDefault(); close(false); return; }
                if (e.key === 'Tab') {
                    // keep keyboard focus inside the dialog
                    e.preventDefault();
                    (document.activeElement === okBtn ? cancelBtn : okBtn).focus();
                }
            }

            cancelBtn.addEventListener('click', function () { close(false); });
            okBtn.addEventListener('click', function () { close(true); });
            overlay.addEventListener('mousedown', function (e) { if (e.target === overlay) close(false); });
            document.addEventListener('keydown', onKey, true);

            document.body.appendChild(overlay);
            okBtn.focus();
        }
    </script>

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
