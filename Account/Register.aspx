<%@ Page Title="Register" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="GiantGroupM3.Account.Register" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <div class="row justify-content-center align-items-center py-4" style="min-height: 80vh;">
        <div class="col-md-8 col-lg-6 col-xl-5">
            <div class="card shadow-lg border-0 rounded-4 p-4 p-md-5 bg-white position-relative overflow-hidden">
                
                <!-- Decorative Top Accent Bar matching your theme -->
                <div class="position-absolute top-0 start-0 w-100" style="height: 6px; background-color: #2e7d32;"></div>

                <!-- Brand / Header Section -->
                <div class="text-center mb-4 mt-1">
                    <div class="mb-2 text-success">
                        <i class="bi bi-person-plus-fill fs-2"></i>
                    </div>
                    <h2 class="fw-bold text-dark mb-1">Create Account</h2>
                    <p class="text-muted small">Sign up to join The Giant Group portal</p>
                </div>

              <!-- Status / Error Message Alert -->
<asp:PlaceHolder runat="server" ID="ErrorPlaceHolder" Visible="false">
    <div class="alert alert-danger py-2 small text-center mb-3 rounded-3" role="alert">
        <asp:Literal runat="server" ID="FailureText" />
    </div>
</asp:PlaceHolder>

                <div class="form-horizontal">
                    
                    <!-- Row 1: First Name & Surname -->
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label class="form-label small fw-semibold text-secondary">First Name</label>
                            <asp:TextBox runat="server" ID="FirstName" ClientIDMode="Static" CssClass="form-control form-control-sm py-2" placeholder="Enter first name" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="FirstName" CssClass="text-danger small mt-1 d-block" ErrorMessage="First name is required." Display="Dynamic" />
                        </div>
                        <div class="col-md-6 mb-3">
                            <label class="form-label small fw-semibold text-secondary">Surname</label>
                            <asp:TextBox runat="server" ID="Surname" ClientIDMode="Static" CssClass="form-control form-control-sm py-2" placeholder="Enter surname" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="Surname" CssClass="text-danger small mt-1 d-block" ErrorMessage="Surname is required." Display="Dynamic" />
                        </div>
                    </div>

                    <!-- Row 2: Client Type & Phone Number -->
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label class="form-label small fw-semibold text-secondary">Client Type</label>
                            <asp:DropDownList ID="ClientType" runat="server" CssClass="form-select form-select-sm py-2">
                                <asp:ListItem Text="Residential" Value="Residential" Selected="True" />
                                <asp:ListItem Text="Commercial" Value="Commercial" />
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-6 mb-3">
                            <label class="form-label small fw-semibold text-secondary">Phone Number</label>
                            <asp:TextBox runat="server" ID="PhoneNumber" CssClass="form-control form-control-sm py-2" placeholder="e.g. 0821234567" />
                        </div>
                    </div>

                    <!-- Email Field -->
                    <div class="mb-3">
                        <label class="form-label small fw-semibold text-secondary">Email Address</label>
                        <asp:TextBox runat="server" ID="Email" ClientIDMode="Static" CssClass="form-control form-control-sm py-2" TextMode="Email" placeholder="name@example.com" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="Email" CssClass="text-danger small mt-1 d-block" ErrorMessage="Email address is required." Display="Dynamic" />
                    </div>

                    <!-- Password Field with Live Strength Indicator -->
                    <div class="mb-3">
                        <label class="form-label small fw-semibold text-secondary">Password</label>
                        <asp:TextBox runat="server" ID="Password" ClientIDMode="Static" CssClass="form-control form-control-sm py-2" TextMode="Password" placeholder="••••••••" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="Password" CssClass="text-danger small mt-1 d-block" ErrorMessage="Password is required." Display="Dynamic" />
                        
                        <!-- Live Password Strength Bar -->
                        <div class="progress mt-2" style="height: 5px;">
                            <div id="passwordStrengthBar" class="progress-bar" role="progressbar" style="width: 0%; transition: width 0.3s ease;"></div>
                        </div>
                        <small id="passwordStrengthText" class="text-muted" style="font-size: 0.75rem;"></small>
                    </div>

                    <!-- Confirm Password Field with Clean Single Match Indicator -->
                    <div class="mb-4">
                        <label class="form-label small fw-semibold text-secondary">Confirm Password</label>
                        <asp:TextBox runat="server" ID="ConfirmPassword" ClientIDMode="Static" CssClass="form-control form-control-sm py-2" TextMode="Password" placeholder="••••••••" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="ConfirmPassword" CssClass="text-danger small mt-1 d-block" ErrorMessage="Please confirm your password." Display="Dynamic" />
                        
                        <!-- Live Match Text Feedback (Duplicate ASP.NET Compare Validator completely removed) -->
                        <small id="passwordMatchText" class="mt-1 d-block" style="font-size: 0.75rem;"></small>
                    </div>

                    <!-- Submit Button -->
                    <div class="d-grid gap-2 mb-3">
                        <asp:Button runat="server" ID="SubmitButton" OnClick="CreateUser_Click" Text="Create Account" CssClass="btn btn-success w-100 py-2 fw-semibold shadow-sm" style="background-color: #2e7d32; border: none;" />
                    </div>

                    <!-- Back to Login Link -->
                    <div class="text-center mt-3">
                        <p class="text-muted small mb-0">
                            Already have an account? 
                            <a href="Login.aspx" class="text-decoration-none fw-semibold" style="color: #2e7d32;">Sign in</a>
                        </p>
                    </div>

                </div>
            </div>
        </div>
    </div>

    <!-- Custom Styling & Live Validation Script -->
    <style>
        .btn-success:hover {
            background-color: #256328 !important;
        }
        .form-control:focus, .form-select:focus {
            border-color: #2e7d32 !important;
            box-shadow: 0 0 0 0.25rem rgba(46, 125, 50, 0.15) !important;
        }
    </style>

    <script>
        document.addEventListener('DOMContentLoaded', function () {
            function setupLiveValidation(inputId) {
                const input = document.getElementById(inputId);
                if (input) {
                    input.addEventListener('input', function () {
                        if (typeof ValidatorValidate === 'function') {
                            for (let i = 0; i < Page_Validators.length; i++) {
                                let val = Page_Validators[i];
                                if (val.controltovalidate === input.id) {
                                    ValidatorValidate(val);
                                }
                            }
                        }
                    });
                }
            }

            setupLiveValidation('FirstName');
            setupLiveValidation('Surname');
            setupLiveValidation('Email');
            setupLiveValidation('Password');
            setupLiveValidation('ConfirmPassword');

            const passwordInput = document.getElementById('Password');
            const confirmInput = document.getElementById('ConfirmPassword');
            const strengthBar = document.getElementById('passwordStrengthBar');
            const strengthText = document.getElementById('passwordStrengthText');
            const matchText = document.getElementById('passwordMatchText');

            if (passwordInput) {
                passwordInput.addEventListener('input', function () {
                    const val = passwordInput.value;
                    let strength = 0;

                    if (val.length > 5) strength += 25;
                    if (val.match(/[A-Z]/)) strength += 25;
                    if (val.match(/[0-9]/)) strength += 25;
                    if (val.match(/[^a-zA-Z0-9]/)) strength += 25;

                    strengthBar.style.width = strength + '%';

                    if (strength <= 25) {
                        strengthBar.className = 'progress-bar bg-danger';
                        strengthText.textContent = 'Weak password';
                        strengthText.className = 'text-danger d-block';
                    } else if (strength <= 50) {
                        strengthBar.className = 'progress-bar bg-warning';
                        strengthText.textContent = 'Fair password';
                        strengthText.className = 'text-warning d-block';
                    } else if (strength <= 75) {
                        strengthBar.className = 'progress-bar bg-info';
                        strengthText.textContent = 'Good password';
                        strengthText.className = 'text-info d-block';
                    } else {
                        strengthBar.className = 'progress-bar bg-success';
                        strengthText.textContent = 'Strong password!';
                        strengthText.className = 'text-success d-block';
                    }

                    checkMatch();
                });
            }

            if (confirmInput) {
                confirmInput.addEventListener('input', function () {
                    checkMatch();
                });
            }

            function checkMatch() {
                if (!confirmInput || !passwordInput) return;
                if (confirmInput.value === '') {
                    matchText.textContent = '';
                    return;
                }
                if (confirmInput.value === passwordInput.value) {
                    matchText.textContent = '✓ Passwords match';
                    matchText.className = 'text-success d-block mt-1';
                } else {
                    matchText.textContent = '✕ Passwords do not match';
                    matchText.className = 'text-danger d-block mt-1';
                }
            }
        });
    </script>
</asp:Content>