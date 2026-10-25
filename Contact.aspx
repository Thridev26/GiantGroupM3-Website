<%@ Page Title="Contact Us" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="GiantGroupM3.Contact" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        /* Honeypot: invisible to people, tempting to bots */
        .hp-field { position: absolute !important; left: -10000px; top: auto; width: 1px; height: 1px; overflow: hidden; }
        .field-error { color: #b02a37; font-size: 0.8rem; font-weight: 600; margin-top: 0.25rem; display: block; }
    </style>

    <div class="container py-5">

        <!-- Page Header Section -->
        <div class="text-center mb-5">
            <h1 class="fw-bold text-success display-5">CONTACT US</h1>
            <p class="lead text-muted mx-auto" style="max-width: 650px;">
                Available 24/7 for all your tree felling and garden service needs. We would love to hear from you. Get in touch via phone, email, or drop us a message below!
            </p>
        </div>

        <div class="row g-4 mb-5">
            <!-- Contact Info Cards -->
            <div class="col-lg-4">
                <div class="card border-0 shadow-sm h-100 rounded-4 overflow-hidden border-top border-success border-4">
                    <div class="card-body p-4 text-center d-flex flex-column justify-content-center">
                        <div class="mb-3 text-success">
                            <i class="bi bi-telephone-fill fs-1" aria-hidden="true"></i>
                        </div>
                        <h2 class="h5 fw-bold text-dark">Call Us Anytime</h2>
                        <p class="text-muted small mb-2">Available 24 hours, 7 days a week</p>
                        <a href="tel:0848331373" class="text-success fw-bold text-decoration-none fs-5">084 833 1373</a>
                    </div>
                </div>
            </div>

            <div class="col-lg-4">
                <div class="card border-0 shadow-sm h-100 rounded-4 overflow-hidden border-top border-success border-4">
                    <div class="card-body p-4 text-center d-flex flex-column justify-content-center">
                        <div class="mb-3 text-success">
                            <i class="bi bi-envelope-fill fs-1" aria-hidden="true"></i>
                        </div>
                        <h2 class="h5 fw-bold text-dark">Email Us</h2>
                        <p class="text-muted small mb-2">We reply as soon as possible</p>
                        <a href="mailto:info@gianttreefelling.co.za" class="text-success fw-bold text-decoration-none text-break">info@gianttreefelling.co.za</a>
                    </div>
                </div>
            </div>

            <div class="col-lg-4">
                <div class="card border-0 shadow-sm h-100 rounded-4 overflow-hidden border-top border-success border-4">
                    <div class="card-body p-4 text-center d-flex flex-column justify-content-center">
                        <div class="mb-3 text-success">
                            <i class="bi bi-geo-alt-fill fs-1" aria-hidden="true"></i>
                        </div>
                        <h2 class="h5 fw-bold text-dark">Location</h2>
                        <p class="text-muted small mb-2">Giant Tree Felling HQ</p>
                        <span class="text-dark small fw-semibold">8 Baumann Rd, Queensmead Industrial, Queensburgh, 4147</span>
                    </div>
                </div>
            </div>
        </div>

        <!-- Interactive Form and Map Section -->
        <div class="row g-4">
            <!-- Contact Form -->
            <div class="col-lg-7">
                <div class="card border-0 shadow-lg rounded-4 p-4 p-md-5">
                    <h3 class="fw-bold text-dark mb-4">Send Us a Message</h3>

                    <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert alert-success rounded-3 mb-4" role="status">
                        <i class="bi bi-check-circle-fill me-2" aria-hidden="true"></i> Thank you! Your message has been sent successfully. We will be in touch shortly.
                    </asp:Panel>

                    <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-danger rounded-3 mb-4" role="alert">
                        <i class="bi bi-exclamation-triangle-fill me-2" aria-hidden="true"></i>
                        <asp:Label ID="lblError" runat="server" />
                    </asp:Panel>

                    <div class="row g-3">
                        <div class="col-md-6">
                            <asp:Label runat="server" AssociatedControlID="txtName" CssClass="form-label fw-semibold text-secondary" Text="Your Name" />
                            <asp:TextBox ID="txtName" runat="server" MaxLength="100" autocomplete="name" CssClass="form-control form-control-lg bg-light border-0" placeholder="John Doe" />
                            <asp:Label ID="errName" runat="server" CssClass="field-error" Visible="false" />
                        </div>
                        <div class="col-md-6">
                            <asp:Label runat="server" AssociatedControlID="txtEmail" CssClass="form-label fw-semibold text-secondary" Text="Email Address" />
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="254" autocomplete="email" CssClass="form-control form-control-lg bg-light border-0" placeholder="john@example.com" />
                            <asp:Label ID="errEmail" runat="server" CssClass="field-error" Visible="false" />
                        </div>
                        <div class="col-12">
                            <asp:Label runat="server" AssociatedControlID="txtPhone" CssClass="form-label fw-semibold text-secondary" Text="Phone Number (optional)" />
                            <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" MaxLength="20" autocomplete="tel" CssClass="form-control form-control-lg bg-light border-0" placeholder="082 123 4567" />
                            <asp:Label ID="errPhone" runat="server" CssClass="field-error" Visible="false" />
                        </div>
                        <div class="col-12">
                            <asp:Label runat="server" AssociatedControlID="txtMessage" CssClass="form-label fw-semibold text-secondary" Text="Message / Service Required" />
                            <asp:TextBox ID="txtMessage" runat="server" TextMode="MultiLine" Rows="4" MaxLength="2000" CssClass="form-control bg-light border-0" placeholder="Tell us about the trees or garden services you need..." />
                            <asp:Label ID="errMessage" runat="server" CssClass="field-error" Visible="false" />
                        </div>

                        <!-- Honeypot (leave empty) -->
                        <div class="hp-field" aria-hidden="true">
                            <label for="<%= txtWebsite.ClientID %>">Website</label>
                            <asp:TextBox ID="txtWebsite" runat="server" TabIndex="-1" autocomplete="off" />
                        </div>

                        <div class="col-12 mt-4">
                            <asp:Button ID="btnSubmit" runat="server" Text="Send Message" OnClick="btnSubmit_Click" UseSubmitBehavior="false"
                                OnClientClick="this.disabled = true; this.value = 'Sending…';"
                                CssClass="btn btn-success btn-lg w-100 fw-bold shadow-sm py-3 rounded-3" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- Embedded Map & Area info -->
            <div class="col-lg-5">
                <div class="card border-0 shadow-lg rounded-4 overflow-hidden h-100">
                    <div class="card-body p-0 d-flex flex-column h-100">
                        <div class="p-4 bg-success text-white">
                            <h4 class="fw-bold mb-1">Based in Queensburgh</h4>
                            <p class="mb-0 small text-white-50">Proudly servicing Durban, KZN and surrounding areas.</p>
                        </div>
                        <!-- Clean Google Maps Embed Code -->
                        <div class="flex-grow-1" style="min-height: 350px;">
                            <iframe
                                title="Queensburgh Location Map"
                                src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3460.854!2d30.932!3d-29.885!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0xef7002a2818a7a9%3A0x400f40d2f093f0!2s8%20Baumann%20Rd%2C%20Queensmead%2520Industrial%2C%20Queensburgh!5e0!3m2!1sen!2sza!4v1650000000000!5m2!1sen!2sza"
                                width="100%"
                                height="100%"
                                style="border:0;"
                                allowfullscreen=""
                                loading="lazy"
                                referrerpolicy="no-referrer-when-downgrade">
                            </iframe>
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </div>
</asp:Content>
