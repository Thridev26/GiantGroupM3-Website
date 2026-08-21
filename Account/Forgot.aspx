<%@ Page Title="Forgot Password" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Forgot.aspx.cs" Inherits="GiantGroupM3.Account.ForgotPass" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <!-- Bootstrap Icons Stylesheet -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">

    <div class="row justify-content-center align-items-center" style="min-height: 75vh;">
        <div class="col-md-6 col-lg-5">
            <div class="card shadow-lg border-0 rounded-4 p-4 p-md-5 bg-white position-relative overflow-hidden">
                
                <!-- Decorative Top Accent Bar matching your Navbar -->
                <div class="position-absolute top-0 start-0 w-100" style="height: 6px; background-color: #2e7d32;"></div>

                <!-- Brand / Header Section -->
                <div class="text-center mb-4 mt-2">
                    <div class="mb-3">
                        <span class="d-inline-flex align-items-center justify-content-center bg-success bg-opacity-10 text-success rounded-circle" style="width: 55px; height: 55px;">
                            <i class="bi bi-key fs-4"></i>
                        </span>
                    </div>
                    <h2 class="fw-bold text-dark mb-1">Forgot Password</h2>
                    <p class="text-muted small">Enter your registered email address and we'll send you an OTP to reset your password.</p>
                </div>

                <section id="forgotPasswordForm">
                    <div class="form-horizontal">
                        
                        <!-- Validation/Error Summary -->
                        <asp:PlaceHolder runat="server" ID="ErrorMessage" Visible="false">
                            <div class="alert py-2 small text-center mb-3 rounded-3 border-0 bg-danger bg-opacity-10 text-danger" role="alert">
                                <i class="bi bi-exclamation-circle-fill me-1"></i> <asp:Literal runat="server" ID="FailureText" />
                            </div>
                        </asp:PlaceHolder>

                        <!-- Optional DisplayEmail Placeholder if used in code-behind -->
                        <asp:PlaceHolder runat="server" ID="DisplayEmail" Visible="false">
                            <!-- Code-behind toggle support container -->
                        </asp:PlaceHolder>

                        <!-- Email Field -->
                        <div class="mb-4">
                            <asp:Label runat="server" AssociatedControlID="Email" CssClass="form-label text-secondary small fw-semibold">Email Address</asp:Label>
                            <div class="input-group shadow-sm rounded-3 overflow-hidden border">
                                <span class="input-group-text bg-white border-0 text-muted px-3"><i class="bi bi-envelope"></i></span>
                                <asp:TextBox runat="server" ID="Email" CssClass="form-control border-0 bg-white py-2 shadow-none" TextMode="Email" placeholder="name@example.com" />
                            </div>
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="Email" CssClass="text-danger small mt-1 d-block fw-light" ErrorMessage="<i class='bi bi-info-circle me-1'></i> The email field is required." Display="Dynamic" />
                        </div>

                        <!-- Submit Button styled with the brand green -->
                        <div class="d-grid gap-2 mb-3">
                            <asp:Button runat="server" ID="SendOtpButton" Text="Send OTP" OnClick="SendEmail_Click" CssClass="btn text-white py-2.5 fw-semibold rounded-3 shadow-sm border-0" Style="background-color: #2e7d32; transition: all 0.2s;" />
                        </div>

                        <!-- Back to Login Link -->
                        <div class="text-center mt-4">
                            <p class="text-muted small mb-0">
                                Remember your password? 
                                <asp:HyperLink runat="server" ID="LoginHyperLink" NavigateUrl="~/Account/Login" CssClass="text-decoration-none fw-semibold" Style="color: #2e7d32;">Log in</asp:HyperLink>
                            </p>
                        </div>

                    </div>
                </section>

            </div>
        </div>
    </div>

    <!-- Custom Styling & Scripts -->
    <style>
        .btn:hover {
            background-color: #256328 !important;
            transform: translateY(-1px);
        }
        .form-control:focus {
            background-color: #fff !important;
            box-shadow: none !important;
        }
        .input-group:focus-within {
            box-shadow: 0 0 0 0.25rem rgba(46, 125, 50, 0.15) !important;
            border-color: #2e7d32 !important;
        }
        input:-webkit-autofill,
        input:-webkit-autofill:hover, 
        input:-webkit-autofill:focus {
            -webkit-box-shadow: 0 0 0 30px white inset !important;
            -webkit-text-fill-color: #212529 !important;
        }
    </style>
</asp:Content>