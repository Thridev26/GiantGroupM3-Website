<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Calendar.aspx.cs" Inherits="GiantGroupM3.CalendarPage" MasterPageFile="~/Site.master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManagerProxy ID="ScriptManagerProxy1" runat="server" />

    <!-- FullCalendar CSS & JS CDN -->
    <link href="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.10/index.global.min.css" rel="stylesheet" />
    <script src="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.10/index.global.min.js"></script>

    <div class="container-fluid py-4 px-lg-5">
        <!-- Header Controls -->
        <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-center mb-4 pb-3 border-bottom gap-3">
            <div>
                <h2 class="h3 fw-bold text-dark mb-1">
                    <i class="fa fa-calendar-alt text-success me-2"></i> Operations &amp; Crew Schedule
                </h2>
                <p class="text-muted small mb-0">Manage active tree-felling dispatches, site evaluations, and crew assignments.</p>
            </div>

            <div class="d-flex align-items-center gap-3">
                <!-- Visual Legend -->
                <div class="d-none d-lg-flex align-items-center gap-3 bg-white px-3 py-2 rounded-pill shadow-sm border">
                    <span class="badge bg-primary px-2 py-1">⚙️ Active Job</span>
                    <span class="badge bg-success px-2 py-1">🌲 Site Inspection</span>
                </div>
            </div>
        </div>

        <!-- Calendar Container Card -->
        <div class="card shadow-sm border-0 rounded-4 p-4">
            <div id="calendar" style="min-height: 750px;"></div>
        </div>
    </div>

    <!-- Event Detail Modal -->
    <div class="modal fade" id="eventDetailModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow-lg rounded-4">
                <div class="modal-header border-0 pb-0">
                    <h5 class="modal-title fw-bold" id="modalTitle">Event Details</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body py-4">
                    <div class="d-flex align-items-center mb-3">
                        <span id="modalBadge" class="badge me-2 fs-6"></span>
                        <span id="modalDate" class="text-muted small fw-semibold"></span>
                    </div>
                    <p id="modalDescription" class="mb-0 text-secondary fs-6"></p>
                </div>
                <div class="modal-footer border-0 pt-0">
                    <button type="button" class="btn btn-light rounded-pill px-4" data-bs-dismiss="modal">Close</button>
                    <a id="modalActionBtn" href="#" class="btn btn-primary rounded-pill px-4">Open Record</a>
                </div>
            </div>
        </div>
    </div>

    <script>
        document.addEventListener('DOMContentLoaded', function () {
            var calendarEl = document.getElementById('calendar');

            var calendar = new FullCalendar.Calendar(calendarEl, {
                initialView: 'dayGridMonth',
                //initialDate: '2026-06-01',
                displayEventTime: false,
                eventDisplay: 'block',
                headerToolbar: {
                    left: 'prev,next today',
                    center: 'title',
                    right: 'dayGridMonth,timeGridWeek,listWeek'
                },
                buttonText: {
                    today: 'Today',
                    dayGridMonth: 'Month View',
                    timeGridWeek: 'Week Grid',
                    listWeek: 'List View'
                },
                themeSystem: 'bootstrap5',
                dayMaxEvents: 3,
                events: '<%= ResolveUrl("~/CalendarEvents.ashx") %>',

                // Attach rich hover tooltips on event mount
                eventDidMount: function (info) {
                    var props = info.event.extendedProps;
                    var isJob = props.type === 'Job';

                    var badgeMarkup = isJob
                        ? '<span class="badge bg-primary me-1">⚙️ Active Job</span>'
                        : '<span class="badge bg-success me-1">🌲 Site Evaluation</span>';

                    var tooltipContent = `
                        <div class="p-1">
                            <div class="mb-2">${badgeMarkup}</div>
                            <div class="fw-bold text-dark fs-6">${info.event.title}</div>
                            <div class="text-muted small mt-1">
                                <i class="fa fa-calendar me-1"></i> ${info.event.start.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' })}
                            </div>
                        </div>
                    `;

                    new bootstrap.Popover(info.el, {
                        title: '',
                        content: tooltipContent,
                        html: true,
                        trigger: 'hover',
                        placement: 'top',
                        container: 'body'
                    });
                },

                eventClick: function (info) {
                    info.jsEvent.preventDefault();

                    var props = info.event.extendedProps;
                    document.getElementById('modalTitle').innerText = info.event.title;
                    document.getElementById('modalDate').innerText = info.event.start.toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
                    document.getElementById('modalDescription').innerText = "Category: " + (props.type || 'Job');

                    var badge = document.getElementById('modalBadge');
                    var actionBtn = document.getElementById('modalActionBtn');

                    if (props.type === 'Job') {
                        badge.className = 'badge bg-primary px-3 py-2';
                        badge.innerText = '⚙️ Active Job';
                    } else {
                        badge.className = 'badge bg-success px-3 py-2';
                        badge.innerText = '🌲 Site Evaluation';
                    }

                    actionBtn.href = info.event.url;

                    var modal = new bootstrap.Modal(document.getElementById('eventDetailModal'));
                    modal.show();
                }
            });

            calendar.render();
        });
    </script>

    <style>
        .fc { font-family: inherit; }
        .fc-header-toolbar { flex-wrap: wrap; gap: 0.75rem; }
        .fc-toolbar-title { font-size: 1.35rem !important; font-weight: 700; color: #212529; }
        .fc-button-primary { background-color: #f8f9fa !important; border-color: #dee2e6 !important; color: #495057 !important; font-weight: 600 !important; border-radius: 50rem !important; padding: 0.375rem 1rem !important; }
        .fc-button-primary:hover, .fc-button-primary.fc-button-active { background-color: #2d6a4f !important; border-color: #2d6a4f !important; color: #fff !important; }
        
        /* Modern pill-style event badges */
        .fc-daygrid-event { 
            border-radius: 6px !important; 
            padding: 4px 8px !important; 
            font-size: 0.8rem !important; 
            font-weight: 600 !important; 
            margin-bottom: 3px !important;
            border: none !important;
            box-shadow: 0 2px 4px rgba(0,0,0,0.06);
            cursor: pointer;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .fc-daygrid-day-frame {
            min-height: 110px !important;
        }

        .fc-more-link {
            color: #2d6a4f !important;
            font-weight: 700 !important;
            font-size: 0.8rem !important;
        }

        /* Hover Popover Styling */
        .popover {
            border: none !important;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15) !important;
            border-radius: 12px !important;
            z-index: 1060;
        }
    </style>
</asp:Content>