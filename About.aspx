<%@ Page Title="About Us" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="GiantGroupM3.About" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5">
        
        <!-- Hero Section -->
        <div class="row align-items-center mb-5">
            <div class="col-lg-7">
                <span class="badge bg-success-subtle text-success fw-bold px-3 py-2 rounded-pill mb-3 text-uppercase" style="letter-spacing: 1px;">About Giant Tree Felling</span>
                <h1 class="fw-bold display-4 text-dark mb-3">Professional Tree Felling & Garden Services in KZN</h1>
                <p class="lead text-muted">
                    Giant Tree Felling is a KwaZulu-Natal-based company specialising in all aspects of tree felling and garden services. Our goal is to deliver superior-quality workmanship on every job, no matter the size or complexity.
                </p>
                <p class="text-secondary">
                    We are fully insured, committed to safety first, and equipped to handle work for domestic, commercial, and industrial clients. With 24/7 availability and same-day free quotations, you can count on us when you need reliable service fast.
                </p>
            </div>
            <div class="col-lg-5 text-center mt-4 mt-lg-0">
                <div class="card border-0 shadow-lg rounded-4 bg-success text-white p-4">
                    <div class="card-body">
                        <i class="bi bi-shield-check display-3 mb-3 text-white-50"></i>
                        <h3 class="fw-bold">Fully Insured & Certified</h3>
                        <p class="small text-white-50 mb-0">Peace of mind for domestic, commercial, and industrial properties across KwaZulu-Natal.</p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Why Choose Us Grid -->
        <div class="mb-5">
            <div class="text-center mb-4">
                <h2 class="fw-bold text-success">Why Choose Giant Tree Felling?</h2>
                <p class="text-muted">What sets our team and standards apart from the rest</p>
            </div>
            <div class="row g-4">
                <div class="col-md-4">
                    <div class="card border-0 shadow-sm h-100 rounded-4 p-3 border-top border-success border-3">
                        <div class="card-body">
                            <div class="text-success mb-2"><i class="bi bi-shield-fill-check fs-2"></i></div>
                            <h5 class="fw-bold">Fully Insured</h5>
                            <p class="text-muted small mb-0">Fully insured for your peace of mind on every single project.</p>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card border-0 shadow-sm h-100 rounded-4 p-3 border-top border-success border-3">
                        <div class="card-body">
                            <div class="text-success mb-2"><i class="bi bi-cone-striped fs-2"></i></div>
                            <h5 class="fw-bold">Safety First Approach</h5>
                            <p class="text-muted small mb-0">Strict safety standards, site managers on every job, and proper safety gear worn at all times.</p>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card border-0 shadow-sm h-100 rounded-4 p-3 border-top border-success border-3">
                        <div class="card-body">
                            <div class="text-success mb-2"><i class="bi bi-clock-history fs-2"></i></div>
                            <h5 class="fw-bold">24/7 Availability</h5>
                            <p class="text-muted small mb-0">On call 24 hours a day, 7 days a week with same-day free quotations.</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Specialized Services Overview -->
        <div class="mb-5 bg-light rounded-4 p-4 p-md-5 shadow-sm">
            <div class="row align-items-center mb-4">
                <div class="col">
                    <h2 class="fw-bold text-dark mb-1">Our Core Services</h2>
                    <p class="text-muted mb-0">Comprehensive solutions to keep your property safe and neat</p>
                </div>
            </div>
            <div class="row g-3">
                <div class="col-md-6 col-lg-4">
                    <div class="p-3 bg-white rounded-3 shadow-sm h-100 border-start border-success border-4">
                        <h6 class="fw-bold text-dark mb-1">Tree & Sectional Felling</h6>
                        <p class="text-muted small mb-0">Safe removal of all or part of a tree, including controlled removal in confined or high-risk areas.</p>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="p-3 bg-white rounded-3 shadow-sm h-100 border-start border-success border-4">
                        <h6 class="fw-bold text-dark mb-1">Stump Removal & Regrowth Prevention</h6>
                        <p class="text-muted small mb-0">Complete removal using our own stump grinder to ensure roots and stumps do not return.</p>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="p-3 bg-white rounded-3 shadow-sm h-100 border-start border-success border-4">
                        <h6 class="fw-bold text-dark mb-1">Hedge Trimming & Shaping</h6>
                        <p class="text-muted small mb-0">Year-round maintenance keeping hedges neat, safe, and improving property appearance.</p>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="p-3 bg-white rounded-3 shadow-sm h-100 border-start border-success border-4">
                        <h6 class="fw-bold text-dark mb-1">Vegetation & Site Clearance</h6>
                        <p class="text-muted small mb-0">Full site and land clearing of all growth, roots, and waste for residential or industrial plots.</p>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="p-3 bg-white rounded-3 shadow-sm h-100 border-start border-success border-4">
                        <h6 class="fw-bold text-dark mb-1">Grass Cutting & Garden Clean-Ups</h6>
                        <p class="text-muted small mb-0">Regular or once-off lawn care, brush cutter work, and responsible transport to dump sites.</p>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="p-3 bg-white rounded-3 shadow-sm h-100 border-start border-success border-4">
                        <h6 class="fw-bold text-dark mb-1">Professional Tree Planting</h6>
                        <p class="text-muted small mb-0">Ornamental and shade tree planting designed to promote healthy, long-term growth.</p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Equipment & Management Standards -->
        <div class="row g-4">
            <div class="col-md-6">
                <div class="card border-0 shadow-sm h-100 rounded-4 p-4">
                    <div class="card-body">
                        <div class="text-success mb-3"><i class="bi bi-tools fs-1"></i></div>
                        <h4 class="fw-bold text-dark">Stihl-Approved Equipment</h4>
                        <p class="text-muted">
                            We ensure high-quality workmanship by using Stihl-approved equipment only. All equipment is fully tested, and safety gear is worn at all times to protect both our team and your property.
                        </p>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="card border-0 shadow-sm h-100 rounded-4 p-4">
                    <div class="card-body">
                        <div class="text-success mb-3"><i class="bi bi-people-fill fs-1"></i></div>
                        <h4 class="fw-bold text-dark">Expert Site Management</h4>
                        <p class="text-muted">
                            Every job is overseen by a site manager to ensure smooth operations and strict safety standards. Our knowledgeable and enthusiastic team members are fully trained and qualified.
                        </p>
                    </div>
                </div>
            </div>
        </div>

    </div>
</asp:Content>