<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Calendar.aspx.cs" Inherits="GiantGroupM3.Calendar" MasterPageFile="~/Site.master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5 my-3">
        
        <!-- Header & Controls -->
        <div class="row align-items-center mb-4">
            <div class="col-md-6">
                <h1 class="h3 fw-bold text-dark mb-1"><i class="fa fa-calendar-alt text-success me-2"></i> Operations & Crew Schedule</h1>
                <p class="text-muted small mb-0">Manage tree-felling dispatches, site inspections, and staff allocations.</p>
            </div>
            <div class="col-md-6 text-md-end mt-3 mt-md-0">
                <button type="button" class="btn btn-outline-secondary btn-sm px-3 rounded-pill me-2">Today</button>
                <button type="button" class="btn text-white btn-sm px-4 rounded-pill fw-semibold shadow-sm" style="background-color: #2d6a4f;">
                    <i class="fa fa-plus me-1"></i> New Schedule Event
                </button>
            </div>
        </div>

        <!-- Main Schedule Dashboard Card -->
        <div class="card shadow-sm border-0 rounded-4 p-4">
            
            <!-- Calendar Navigation Bar -->
            <div class="d-flex justify-content-between align-items-center mb-4 pb-3 border-bottom">
                <h5 class="fw-bold text-dark mb-0">August 2026</h5>
                <div class="btn-group" role="group">
                    <button type="button" class="btn btn-outline-secondary btn-sm">&larr; Prev</button>
                    <button type="button" class="btn btn-outline-secondary btn-sm">Next &rarr;</button>
                </div>
            </div>

            <!-- Schedule Grid / Table Placeholder -->
            <div class="table-responsive">
                <table class="table table-bordered align-middle text-center mb-0" style="table-layout: fixed;">
                    <thead class="table-light text-muted small">
                        <tr>
                            <th>Mon</th>
                            <th>Tue</th>
                            <th>Wed</th>
                            <th>Thu</th>
                            <th>Fri</th>
                            <th>Sat</th>
                            <th>Sun</th>
                        </tr>
                    </thead>
                    <tbody>
                        <!-- Sample Week Row -->
                        <tr style="height: 100px;">
                            <td class="text-muted small align-top p-2">24</td>
                            <td class="text-muted small align-top p-2">25
                                <div class="badge bg-success bg-opacity-10 text-success text-start w-100 p-1 mt-1 small text-truncate">
                                    🌲 Site Inspection: Sandton
                                </div>
                            </td>
                            <td class="text-muted small align-top p-2">26</td>
                            <td class="text-muted small align-top p-2">27
                                <div class="badge bg-dark bg-opacity-75 text-white text-start w-100 p-1 mt-1 small text-truncate">
                                    ⚙️ Heavy Tree Removal: Crew A
                                </div>
                            </td>
                            <td class="text-muted small align-top p-2">28</td>
                            <td class="text-muted small align-top p-2">29</td>
                            <td class="text-muted small align-top p-2">30</td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <!-- Future Capability Notice -->
            <div class="mt-4 p-3 bg-light rounded-3 border-0 small text-secondary d-flex align-items-center">
                <i class="fa fa-info-circle text-success fs-5 me-3"></i>
                <div>
                    <strong>Future Capabilities:</strong> This module is structured to support drag-and-drop job dispatches, full Google Calendar synchronization, and automated crew notifications in upcoming enterprise iterations.
                </div>
            </div>

        </div>
    </div>
</asp:Content>