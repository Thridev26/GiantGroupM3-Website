<%@ Page Title="Log in" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="GiantGroupM3.Account.Login" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/giant-ui.css") %>?v=1" />

    <div class="gg-auth-wrap">
        <div class="gg-auth-card">

            <div class="gg-auth-top">
                <span class="gg-auth-icon"><i class="bi bi-shield-lock"></i></span>
                <h1>Welcome back</h1>
                <p>Sign in to access The Giant Group portal</p>
            </div>

            <div class="gg-auth-body">

                <!-- Validation / status messages (shared by login and MFA steps) -->
                <asp:PlaceHolder runat="server" ID="ErrorMessage" Visible="false">
                    <div id="alertBox" runat="server" class="gg-msg is-error" role="alert">
                        <i id="alertIcon" runat="server" class="me-1"></i> <asp:Literal runat="server" ID="FailureText" />
                    </div>
                </asp:PlaceHolder>

                <!-- Step 1: email + password -->
                <section id="loginForm" runat="server">

                    <div class="gg-field">
                        <asp:Label runat="server" AssociatedControlID="Email" CssClass="gg-field-label">Email address</asp:Label>
                        <div class="gg-iconfield">
                            <i class="bi bi-envelope lead-i"></i>
                            <asp:TextBox runat="server" ID="Email" CssClass="gg-input" TextMode="Email" placeholder="name@example.com" autocomplete="username" />
                        </div>
                        <asp:RequiredFieldValidator runat="server" ID="EmailValidator" ControlToValidate="Email" CssClass="gg-field-error" ErrorMessage="Email address is required." Display="Dynamic" />
                    </div>

                    <div class="gg-field mb-2">
                        <asp:Label runat="server" AssociatedControlID="Password" CssClass="gg-field-label">Password</asp:Label>
                        <div class="gg-iconfield">
                            <i class="bi bi-lock lead-i"></i>
                            <asp:TextBox runat="server" ID="Password" CssClass="gg-input" TextMode="Password" placeholder="Enter your password" autocomplete="current-password" />                           
                        </div>
                        <asp:RequiredFieldValidator runat="server" ID="PasswordValidator" ControlToValidate="Password" CssClass="gg-field-error" ErrorMessage="Password is required." Display="Dynamic" />
                    </div>

                    <div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-4">
                        <label class="gg-check mb-0">
                            <input type="checkbox" runat="server" id="RememberMe" />
                            Remember me on this device
                        </label>
                        <a href="Forgot.aspx" class="gg-link small">Forgot password?</a>
                    </div>

                    <asp:Button runat="server" ID="LoginButton" Text="Log in" OnClick="LogIn" OnClientClick="ggBusy(this, 'Signing in...');" CssClass="gg-btn gg-btn-primary w-100 py-3" />

                    <div class="gg-divider">or continue with</div>

                    <asp:LinkButton runat="server" ID="GoogleLoginBtn" OnClick="GoogleLogin_Click" CausesValidation="false" CssClass="gg-btn gg-google py-3">
                        <svg width="18" height="18" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48" aria-hidden="true">
                            <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
                            <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
                            <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
                            <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.46-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
                        </svg>
                        <span>Sign in with Google</span>
                    </asp:LinkButton>

                    <p class="gg-auth-foot mb-0">
                        Don't have an account?
                        <asp:HyperLink runat="server" ID="RegisterHyperLink">Register as a new user</asp:HyperLink>
                    </p>
                </section>

                <!-- Step 2: MFA verification -->
                <asp:Panel runat="server" ID="MfaPanel" Visible="false">
                    <div class="text-center mb-4">
                        <span class="gg-tile is-lg mb-3"><i class="bi bi-envelope-check"></i></span>
                        <h2 class="gg-display h4 mb-2" style="color: var(--gg-text);">Check your email</h2>
                        <p class="gg-muted small mb-0">Enter the 6-digit verification code we sent to your email address to finish signing in.</p>
                    </div>

                    <div class="mb-3">
                        <asp:TextBox runat="server" ID="MfaCode" CssClass="gg-input gg-otp" MaxLength="6" placeholder="------" autocomplete="one-time-code" inputmode="numeric" pattern="[0-9]*" />
                    </div>

                    <p class="text-center small gg-muted mb-4">
                        Code expires in <strong id="mfaTimer" style="color: var(--gg-text);">5:00</strong>
                        &nbsp;&middot;&nbsp;
                        <a href="javascript:void(0)" id="mfaResend" class="gg-link">Resend code</a>
                    </p>

                    <asp:Button runat="server" ID="VerifyMfaButton" Text="Verify and sign in" OnClick="VerifyMfa_Click" OnClientClick="ggBusy(this, 'Verifying...');" CssClass="gg-btn gg-btn-primary w-100 py-3" />

                    <p class="gg-auth-foot mb-0">
                        <a href="Login.aspx"><i class="bi bi-arrow-left me-1"></i>Back to sign in</a>
                    </p>
                </asp:Panel>
            </div>
        </div>
    </div>

    <style>
   /* Hide browser-native password reveal buttons */
    input[type="password"]::-ms-reveal, 
    input[type="password"]::-ms-clear { 
        display: none !important; 
    }
    input[type="password"]::-webkit-credentials-auto-fill-button { 
        display: none !important; 
    }

    /* Make the password's iconfield relative so we can anchor the eye inside it */
    .gg-field .gg-iconfield {
        position: relative !important;
        overflow: hidden !important;
    }

    /* Fix the C# server-tag selector syntax so the password box gets its right padding */
    input[id*="Password"] {
        padding-right: 48px !important;
    }

    /* Position the eye button absolutely on the right edge */
    .gg-field .gg-iconfield button.gg-eye {
        position: absolute !important;
        right: 10px !important;     /* <-- ADJUST THIS: change to move left/right */
        top: 50% !important;
        transform: translateY(-50%) !important;
        
        background: transparent !important;
        border: none !important;
        outline: none !important;
        box-shadow: none !important;
        width: 32px !important;
        height: 32px !important;
        display: flex !important;
        align-items: center !important;
        justify-content: center !important;
        color: var(--gg-muted) !important;
        border-radius: 6px !important;
        cursor: pointer;
    }

    .gg-field .gg-iconfield button.gg-eye:hover {
        background: var(--gg-surface-2) !important;
        color: var(--gg-accent) !important;
    }
    </style>

    <script>
        // Show/hide password
        document.getElementById('togglePassword')?.addEventListener('click', function () {
            const input = document.getElementById('<%= Password.ClientID %>');
            const icon = this.querySelector('i');
            if (input.type === 'password') { input.type = 'text'; icon.classList.replace('bi-eye', 'bi-eye-slash'); }
            else { input.type = 'password'; icon.classList.replace('bi-eye-slash', 'bi-eye'); }
        });

        // Change button text after the form has started submitting (keeps the postback intact)
        function ggBusy(btn, text) {
            if (typeof Page_ClientValidate === 'function' && !Page_ClientValidate()) return;
            setTimeout(function () { btn.value = text; }, 0);
        }

        // MFA step: numeric-only input, countdown, resend
        (function () {
            const code = document.getElementById('<%= MfaCode.ClientID %>');
            if (!code) return;
            code.focus();
            code.addEventListener('input', function () { this.value = this.value.replace(/\D/g, '').slice(0, 6); });

            let remaining = 300;
            const timer = document.getElementById('mfaTimer');
            const tick = setInterval(function () {
                remaining--;
                if (remaining <= 0) { clearInterval(tick); timer.textContent = 'expired'; return; }
                timer.textContent = Math.floor(remaining / 60) + ':' + String(remaining % 60).padStart(2, '0');
            }, 1000);

            document.getElementById('mfaResend')?.addEventListener('click', function () {
                __doPostBack('mfaResend', '');
            });
        })();
    </script>
</asp:Content>
