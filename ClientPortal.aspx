<%@ Page Title="Client Portal" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ClientPortal.aspx.cs" Inherits="GiantGroupM3.ClientPortal" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container my-5">
        <!-- Header Banner -->
        <div class="p-4 mb-4 bg-dark text-white rounded-4 shadow-sm d-flex justify-content-between align-items-center" style="background: linear-gradient(135deg, #1b4332 0%, #2d6a4f 100%);">
            <div>
                <span class="badge bg-success mb-2">Client Portal</span>
                <h1 class="h3 fw-bold mb-1">Welcome, <asp:Literal ID="litClientName" runat="server" Text="Valued Client" />!</h1>
                <p class="mb-0 text-white-50 small">Track your property service requests, review estimates, and schedule site work.</p>
            </div>
            <div>
                <a runat="server" href="~/Requests/SubmitRequest.aspx" class="btn btn-light text-success fw-bold btn-sm shadow-sm">+ New Request</a>
            </div>
        </div>

        <!-- Quick Status Overview -->
        <div class="row g-3 mb-4">
            <div class="col-md-4">
                <div class="p-3 border rounded-3 bg-white shadow-sm border-start border-success border-4">
                    <span class="text-muted small fw-bold text-uppercase">Active Requests</span>
                    <h2 class="fw-bold text-dark my-1"><asp:Literal ID="litActiveRequests" runat="server" Text="0" /></h2>
                    <small class="text-muted">Currently processing</small>
                </div>
            </div>
            <div class="col-md-4">
                <div class="p-3 border rounded-3 bg-white shadow-sm border-start border-warning border-4">
                    <span class="text-muted small fw-bold text-uppercase">Quotes Ready</span>
                    <h2 class="fw-bold text-dark my-1"><asp:Literal ID="litQuotesReady" runat="server" Text="0" /></h2>
                    <small class="text-warning fw-semibold">Awaiting your sign-off</small>
                </div>
            </div>
            <div class="col-md-4">
                <div class="p-3 border rounded-3 bg-white shadow-sm border-start border-primary border-4">
                    <span class="text-muted small fw-bold text-uppercase">Completed Services</span>
                    <h2 class="fw-bold text-dark my-1"><asp:Literal ID="litCompletedJobs" runat="server" Text="0" /></h2>
                    <small class="text-muted">Total jobs finished</small>
                </div>
            </div>
        </div>

        <!-- Client Action Modules -->
        <h4 class="fw-bold text-dark mb-3">Service Actions</h4>
        <div class="row g-3">
            <div class="col-md-6">
                <div class="card h-100 shadow-sm border-0">
                    <div class="card-body p-4">
                        <div class="fs-2 text-success mb-2">📝</div>
                        <h5 class="fw-bold card-title">Submit New Job Request</h5>
                        <p class="text-muted small">Need tree removal, branch trimming, or site clearing? Submit photos and details directly to our field team.</p>
                        <a runat="server" href="~/Requests/SubmitRequest.aspx" class="btn btn-success fw-bold btn-sm">Request Service &rarr;</a>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="card h-100 shadow-sm border-0">
                    <div class="card-body p-4">
                        <div class="fs-2 text-success mb-2">📋</div>
                        <h5 class="fw-bold card-title">Request & Job History</h5>
                        <p class="text-muted small">Track live status updates, review past estimates, and download completion reports for all your properties.</p>
                        <a runat="server" href="~/Requests/ViewRequests.aspx" class="btn btn-outline-success fw-bold btn-sm">View History &rarr;</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>