<%@ Page Title="Log in" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="GiantGroupM3.Account.Login" %>



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

                            <i class="bi bi-shield-lock fs-4"></i>

                        </span>

                    </div>

                    <h2 class="fw-bold text-dark mb-1">Welcome Back</h2>

                    <p class="text-muted small">Sign in to access The Giant Group portal</p>

                </div>



                <!-- Standard Login & MFA Multi-Step Container -->

                <section id="loginForm" runat="server">

                    <div class="form-horizontal">

                        

                        <!-- Validation/Error Summary -->

                        <asp:PlaceHolder runat="server" ID="ErrorMessage" Visible="false">

                            <div class="alert py-2 small text-center mb-3 rounded-3 border-0 bg-danger bg-opacity-10 text-danger" role="alert">

                                <i class="bi bi-exclamation-circle-fill me-1"></i> <asp:Literal runat="server" ID="FailureText" />

                            </div>

                        </asp:PlaceHolder>



                        <!-- Email Field -->

                        <div class="mb-3">

                            <asp:Label runat="server" AssociatedControlID="Email" CssClass="form-label text-secondary small fw-semibold">Email Address</asp:Label>

                            <div class="input-group shadow-sm rounded-3 overflow-hidden border">

                                <span class="input-group-text bg-white border-0 text-muted px-3"><i class="bi bi-envelope"></i></span>

                                <asp:TextBox runat="server" ID="Email" CssClass="form-control border-0 bg-white py-2 shadow-none" TextMode="Email" placeholder="name@example.com" />

                            </div>

                            <asp:RequiredFieldValidator runat="server" ID="EmailValidator" ControlToValidate="Email" CssClass="text-danger small mt-1 d-block fw-light" ErrorMessage="<i class='bi bi-info-circle me-1'></i> Email address is required." Display="Dynamic" />

                        </div>



                        <!-- Password Field -->

                        <div class="mb-2">

                            <asp:Label runat="server" AssociatedControlID="Password" CssClass="form-label text-secondary small fw-semibold mb-1">Password</asp:Label>

                            <div class="input-group shadow-sm rounded-3 overflow-hidden border">

                                <span class="input-group-text bg-white border-0 text-muted px-3"><i class="bi bi-lock"></i></span>

                                <asp:TextBox runat="server" ID="Password" CssClass="form-control border-0 bg-white py-2 shadow-none" TextMode="Password" placeholder="••••••••" />

                                <button class="btn bg-white border-0 text-muted px-3 d-flex align-items-center justify-content-center" type="button" id="togglePassword" title="Show/Hide Password" style="box-shadow: none !important;">

                                    <i class="bi bi-eye"></i>

                                </button>

                            </div>

                            <asp:RequiredFieldValidator runat="server" ID="PasswordValidator" ControlToValidate="Password" CssClass="text-danger small mt-1 d-block fw-light" ErrorMessage="<i class='bi bi-info-circle me-1'></i> Password is required." Display="Dynamic" />

                        </div>



                        <!-- Forgot Password Link -->

                        <div class="d-flex justify-content-end mb-3">

                            <a href="Forgot.aspx" class="small text-decoration-none fw-semibold" style="color: #2e7d32;">Forgot password?</a>

                        </div>


<!-- Remember Me Checkbox -->
<div class="mb-4">
    <div class="form-check">
        <input type="checkbox" runat="server" id="RememberMe" class="form-check-input" />
        <label runat="server" for="RememberMe" class="form-check-label text-secondary small user-select-none" style="cursor: pointer;">Remember me on this device</label>
    </div>
</div>



                        <!-- Submit Button -->

                        <div class="d-grid gap-2 mb-3">

                            <asp:Button runat="server" ID="LoginButton" Text="Log In" OnClick="LogIn" CssClass="btn text-white py-2.5 fw-semibold rounded-3 shadow-sm border-0" Style="background-color: #2e7d32; transition: all 0.2s;" />

                        </div>



                        <!-- Divider -->

                        <div class="position-relative text-center my-3">

                            <hr class="text-muted opacity-25">

                            <span class="position-absolute top-50 start-50 translate-middle px-3 bg-white text-muted small">or continue with</span>

                        </div>

<!-- Google Sign In Button -->
<div class="d-grid gap-2 mb-3">
    <asp:LinkButton runat="server" ID="GoogleLoginBtn" OnClick="GoogleLogin_Click" CausesValidation="false" CssClass="btn google-signin-btn py-2.5 fw-semibold rounded-3 shadow-sm d-flex align-items-center justify-content-center gap-2 bg-white border text-decoration-none">
        <svg width="18" height="18" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48">
            <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
            <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
            <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
            <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.46-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
            <path fill="none" d="M0 0h48v48H0z"/>
        </svg>
        <span style="color: #3c4043; font-family: Roboto, arial, sans-serif;">Sign in with Google</span>
    </asp:LinkButton>
</div>



                        <!-- Register Link -->

                        <div class="text-center mt-4">

                            <p class="text-muted small mb-0">

                                Don't have an account? 

                                <asp:HyperLink runat="server" ID="RegisterHyperLink" CssClass="text-decoration-none fw-semibold" Style="color: #2e7d32;">Register as a new user</asp:HyperLink>

                            </p>

                        </div>

                    </div>

                </section>


<!-- MFA Verification Panel -->
<asp:Panel runat="server" ID="MfaPanel" Visible="false" CssClass="py-2">
    <div class="text-center mb-4">
        <!-- Brand Security Icon Circle -->
        <div class="d-inline-flex align-items-center justify-content-center bg-success bg-opacity-10 text-success rounded-circle mb-3 shadow-sm" style="width: 64px; height: 64px;">
            <i class="bi bi-shield-lock-fill fs-3"></i>
        </div>
        <h4 class="fw-bold text-dark mb-1">Two-Factor Authentication</h4>
        <p class="text-muted small px-3">
            Please enter the 6-digit verification code sent to your email address to complete your sign-in.
        </p>
    </div>
    
    <div class="mb-4 d-flex justify-content-center">
        <div style="width: 260px;">
            <asp:TextBox runat="server" ID="MfaCode" CssClass="form-control fs-3 fw-bold bg-light py-2 rounded-3 shadow-sm" MaxLength="6" placeholder="• • • • • •" autocomplete="one-time-code" Style="text-align: center !important; letter-spacing: 0.3em !important;" />
        </div>
    </div>

    <div class="d-grid gap-2 mb-3">
        <asp:Button runat="server" ID="VerifyMfaButton" Text="Verify & Sign In" OnClick="VerifyMfa_Click" CssClass="btn text-white py-2.5 fw-semibold rounded-3 shadow-sm border-0" Style="background-color: #2e7d32;" />
    </div>

    <div class="text-center">
        <a href="Login.aspx" class="text-decoration-none small text-muted">
            <i class="bi bi-arrow-left me-1"></i> Back to Sign In
        </a>
    </div>
</asp:Panel>


            </div>

        </div>

    </div>



    <!-- Custom Styling & Scripts -->

   <!-- Custom Styling & Scripts -->
    <style>
      .btn:hover { background-color: #256328 !important; transform: translateY(-1px); }
        #togglePassword:hover { background-color: #f8f9fa !important; color: #2e7d32 !important; }
        .form-control:focus { background-color: #fff !important; box-shadow: none !important; }
        .input-group:focus-within { box-shadow: 0 0 0 0.25rem rgba(46, 125, 50, 0.15) !important; border-color: #2e7d32 !important; }

        /* Realistic Google Sign-In Button Styling */
        .google-signin-btn {
            border-color: #dadce0 !important;
            background-color: #ffffff !important;
            transition: background-color 0.2s, box-shadow 0.2s !important;
        }
        .google-signin-btn:hover {
            background-color: #f8f9fa !important;
            border-color: #c4c7c5 !important;
            box-shadow: 0 1px 3px rgba(60, 64, 67, 0.3) !important;
            transform: none !important;
        }
           
        /* Lowkey, professional dark slate/charcoal check appearance */
        .form-check-input:checked {
            background-color: #495057 !important; 
            border-color: #495057 !important;
        }
        .form-check-input:focus {
            border-color: #495057 !important;
            box-shadow: 0 0 0 0.25rem rgba(73, 80, 87, 0.15) !important;
        }

        /* Hide browser default password reveal icons (Edge, IE, Chrome, Safari) */
input[type="password"]::-ms-reveal,
input[type="password"]::-ms-clear {
    display: none !important;
}

input[type="password"]::-webkit-contacts-auto-fill-button,
input[type="password"]::-webkit-credentials-auto-fill-button {
    visibility: hidden;
    position: absolute;
    right: 0;
}
        
    </style>


    <script>

        document.getElementById('togglePassword')?.addEventListener('click', function () {

            const passwordInput = document.getElementById('<%= Password.ClientID %>');

            const icon = this.querySelector('i');

            if (passwordInput.type === 'password') {

                passwordInput.type = 'text';

                icon.classList.replace('bi-eye', 'bi-eye-slash');

            } else {

                passwordInput.type = 'password';

                icon.classList.replace('bi-eye-slash', 'bi-eye');

            }

        });

    </script>

</asp:Content> 

