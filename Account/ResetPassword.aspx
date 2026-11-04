<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ResetPassword.aspx.cs" Inherits="GiantGroupM3.Account.ResetPassword" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <!-- Bootstrap Icons Stylesheet -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">

    <div class="row justify-content-center align-items-center" style="min-height: 75vh;">
        <div class="col-md-6 col-lg-5">
            <div class="card shadow-lg border-0 rounded-4 p-4 p-md-5 bg-white position-relative overflow-hidden">
                
                <!-- Decorative Top Accent Bar matching your theme -->
                <div class="position-absolute top-0 start-0 w-100" style="height: 6px; background-color: #2e7d32;"></div>

                <!-- Brand / Header Section -->
                <div class="text-center mb-4 mt-2">
                    <div class="mb-3">
                        <span class="d-inline-flex align-items-center justify-content-center bg-success bg-opacity-10 text-success rounded-circle" style="width: 55px; height: 55px;">
                            <i class="bi bi-shield-lock fs-4"></i>
                        </span>
                    </div>
                    <h2 id="title" class="fw-bold text-dark mb-1">Reset Password</h2>
                    <p class="text-muted small">Please enter and confirm your new secure password below.</p>
                </div>

                <!-- Only show form if verified, otherwise show security block message -->
                <asp:Panel runat="server" ID="PanelResetForm" Visible="false">
                    
                    <!-- New Password Field -->
                    <div class="mb-3">
                        <asp:Label runat="server" AssociatedControlID="Password" CssClass="form-label text-secondary small fw-semibold">New Password</asp:Label>
                        <div class="input-group shadow-sm rounded-3 overflow-hidden border">
                            <span class="input-group-text bg-white border-0 text-muted px-3"><i class="bi bi-lock"></i></span>
                            <asp:TextBox runat="server" ID="Password" CssClass="form-control border-0 bg-white py-2 shadow-none" TextMode="Password" placeholder="Enter new password" oninput="checkPasswordStrength(this.value)" />
                            <button class="btn btn-light border-0 text-muted px-3" type="button" onclick="togglePassword('MainContent_Password', this)"><i class="bi bi-eye"></i></button>
                        </div>
                        <!-- Password Strength Visual Meter -->
                        <div class="progress mt-2" style="height: 4px;">
                            <div id="strengthBar" class="progress-bar" role="progressbar" style="width: 0%; transition: width 0.3s;"></div>
                        </div>
                        <small id="strengthText" class="text-muted mt-1 d-block" style="font-size: 0.75rem;"></small>
                        
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="Password" CssClass="text-danger small mt-1 d-block fw-light" ErrorMessage="<i class='bi bi-info-circle me-1'></i> The password field is required." />
                    </div>

                    <!-- Confirm New Password Field -->
                    <div class="mb-4">
                        <asp:Label runat="server" AssociatedControlID="ConfirmPassword" CssClass="form-label text-secondary small fw-semibold">Confirm New Password</asp:Label>
                        <div class="input-group shadow-sm rounded-3 overflow-hidden border">
                            <span class="input-group-text bg-white border-0 text-muted px-3"><i class="bi bi-lock-fill"></i></span>
                            <asp:TextBox runat="server" ID="ConfirmPassword" CssClass="form-control border-0 bg-white py-2 shadow-none" TextMode="Password" placeholder="Confirm new password" />
                            <button class="btn btn-light border-0 text-muted px-3" type="button" onclick="togglePassword('MainContent_ConfirmPassword', this)"><i class="bi bi-eye"></i></button>
                        </div>
                        <asp:CompareValidator runat="server" ControlToValidate="ConfirmPassword" ControlToCompare="Password" CssClass="text-danger small mt-1 d-block fw-light" ErrorMessage="<i class='bi bi-info-circle me-1'></i> The passwords do not match." />
                    </div>

                    <!-- Submit Button -->
                    <div class="d-grid mt-4">
                        <asp:Label runat="server" ID="lblPasswordPolicy" Visible="false" CssClass="text-danger small mt-1 d-block fw-light" /> <asp:Button runat="server" OnClick="Reset_Click" Text="Save New Password" CssClass="btn text-white py-2.5 fw-semibold rounded-3 shadow-sm border-0" Style="background-color: #2e7d32; transition: all 0.2s;" />
                    </div>
                </asp:Panel>

                <!-- Access Denied Panel -->
                <asp:Panel runat="server" ID="PanelAccessDenied" Visible="true">
                    <div class="alert alert-danger py-3 border-0 bg-danger bg-opacity-10 text-danger rounded-3 text-center" role="alert">
                        <i class="bi bi-exclamation-triangle-fill fs-5 d-block mb-2"></i>
                        <strong>Unauthorized Access</strong>
                        <p class="small mb-0 mt-1">Please complete the OTP verification step first before trying to access this page.</p>
                    </div>
                    <div class="text-center mt-3">
                        <a href="~/Account/Forgot" runat="server" class="text-decoration-none fw-semibold small" style="color: #2e7d32;">
                            <i class="bi bi-arrow-left me-1"></i> Return to Forgot Password
                        </a>
                    </div>
                </asp:Panel>

            </div>
        </div>
    </div>

    <!-- Custom Styling & Interactive Scripts -->
    <style>
        .btn-success:hover, .btn[style*="background-color: #2e7d32"]:hover {
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
    </style>

    <script>
        // Toggle Password Visibility Feature
        function togglePassword(fieldId, btn) {
            const input = document.getElementById(fieldId);
            const icon = btn.querySelector('i');
            if (input.type === "password") {
                input.type = "text";
                icon.classList.remove("bi-eye");
                icon.classList.add("bi-eye-slash");
            } else {
                input.type = "password";
                icon.classList.remove("bi-eye-slash");
                icon.classList.add("bi-eye");
            }
        }

        // Live Password Strength Indicator
        function checkPasswordStrength(password) {
            const bar = document.getElementById('strengthBar');
            const text = document.getElementById('strengthText');
            if (!bar || !text) return;

            let strength = 0;
            if (password.length >= 6) strength += 25;
            if (password.length >= 10) strength += 25;
            if (/[A-Z]/.test(password)) strength += 25;
            if (/[0-9_$@$!%*?&]/.test(password)) strength += 25;

            bar.style.width = strength + '%';
            
            if (strength <= 25) {
                bar.className = 'progress-bar bg-danger';
                text.textContent = 'Weak password';
                text.className = 'text-danger small mt-1 d-block';
            } else if (strength <= 50) {
                bar.className = 'progress-bar bg-warning';
                text.textContent = 'Fair password';
                text.className = 'text-warning small mt-1 d-block';
            } else if (strength <= 75) {
                bar.className = 'progress-bar bg-info';
                text.textContent = 'Good password';
                text.className = 'text-info small mt-1 d-block';
            } else {
                bar.className = 'progress-bar bg-success';
                text.textContent = 'Strong password!';
                text.className = 'text-success small mt-1 d-block';
            }
        }
    </script>
</asp:Content>