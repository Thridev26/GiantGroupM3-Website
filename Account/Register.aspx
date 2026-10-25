<%@ Page Title="Register" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="GiantGroupM3.Account.Register" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/giant-ui.css") %>?v=1" />

    <div class="gg-auth-wrap">
        <div class="gg-auth-card is-wide">

            <div class="gg-auth-top">
                <span class="gg-auth-icon"><i class="bi bi-person-plus"></i></span>
                <h1>Create your account</h1>
                <p>Sign up to join The Giant Group portal</p>
            </div>

            <div class="gg-auth-body">

                <!-- Status / error message -->
                <asp:PlaceHolder runat="server" ID="ErrorPlaceHolder" Visible="false">
                    <div class="gg-msg is-error" role="alert">
                        <i class="bi bi-exclamation-circle-fill me-1"></i> <asp:Literal runat="server" ID="FailureText" />
                    </div>
                </asp:PlaceHolder>

                <!-- Honeypot: real people never see or fill this; bots do -->
                <div style="position: absolute; left: -9999px;" aria-hidden="true">
                    <input type="text" name="website" tabindex="-1" autocomplete="off" />
                </div>

                <div class="row g-3 mb-1">
                    <div class="col-md-6">
                        <label class="gg-field-label" for="FirstName">First name</label>
                        <asp:TextBox runat="server" ID="FirstName" ClientIDMode="Static" CssClass="gg-input" MaxLength="60" placeholder="Enter first name" autocomplete="given-name" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="FirstName" CssClass="gg-field-error" ErrorMessage="First name is required." Display="Dynamic" />
                    </div>
                    <div class="col-md-6">
                        <label class="gg-field-label" for="Surname">Surname</label>
                        <asp:TextBox runat="server" ID="Surname" ClientIDMode="Static" CssClass="gg-input" MaxLength="60" placeholder="Enter surname" autocomplete="family-name" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="Surname" CssClass="gg-field-error" ErrorMessage="Surname is required." Display="Dynamic" />
                    </div>
                </div>

                <div class="row g-3 mb-3 mt-0">
                    <div class="col-md-6">
                        <label class="gg-field-label" for="<%= ClientType.ClientID %>">Client type</label>
                        <asp:DropDownList ID="ClientType" runat="server" CssClass="gg-select">
                            <asp:ListItem Text="Residential" Value="Residential" Selected="True" />
                            <asp:ListItem Text="Commercial" Value="Commercial" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-6">
                        <label class="gg-field-label" for="<%= PhoneNumber.ClientID %>">Phone number</label>
                        <asp:TextBox runat="server" ID="PhoneNumber" CssClass="gg-input" MaxLength="20" placeholder="e.g. 0821234567" TextMode="Phone" autocomplete="tel" />
                    </div>
                </div>

                <div class="gg-field">
                    <label class="gg-field-label" for="Email">Email address</label>
                    <asp:TextBox runat="server" ID="Email" ClientIDMode="Static" CssClass="gg-input" TextMode="Email" MaxLength="254" placeholder="name@example.com" autocomplete="email" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="Email" CssClass="gg-field-error" ErrorMessage="Email address is required." Display="Dynamic" />
                </div>

                <div class="gg-field">
                    <label class="gg-field-label" for="Password">Password</label>
                    <asp:TextBox runat="server" ID="Password" ClientIDMode="Static" CssClass="gg-input" TextMode="Password" MaxLength="72" placeholder="Create a password" autocomplete="new-password" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="Password" CssClass="gg-field-error" ErrorMessage="Password is required." Display="Dynamic" />

                    <div class="gg-meter"><div id="passwordStrengthBar"></div></div>
                    <small id="passwordStrengthText" class="d-block mt-1" style="font-size: .78rem;"></small>
                    <ul class="gg-rules" id="passwordRules">
                        <li data-rule="len">At least 8 characters</li>
                        <li data-rule="upper">An uppercase letter</li>
                        <li data-rule="lower">A lowercase letter</li>
                        <li data-rule="digit">A number</li>
                    </ul>
                </div>

                <div class="gg-field mb-4">
                    <label class="gg-field-label" for="ConfirmPassword">Confirm password</label>
                    <asp:TextBox runat="server" ID="ConfirmPassword" ClientIDMode="Static" CssClass="gg-input" TextMode="Password" MaxLength="72" placeholder="Repeat your password" autocomplete="new-password" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="ConfirmPassword" CssClass="gg-field-error" ErrorMessage="Please confirm your password." Display="Dynamic" />
                    <small id="passwordMatchText" class="d-block mt-1" style="font-size: .8rem;"></small>
                </div>

                <asp:Button runat="server" ID="SubmitButton" OnClick="CreateUser_Click" Text="Create account" CssClass="gg-btn gg-btn-primary w-100 py-3" />

                <p class="gg-auth-foot mb-0">
                    Already have an account? <a href="Login.aspx">Sign in</a>
                </p>
            </div>
        </div>
    </div>

    <script>
        document.addEventListener('DOMContentLoaded', function () {
            // Re-run required-field validators as the user types
            ['FirstName', 'Surname', 'Email', 'Password', 'ConfirmPassword'].forEach(function (id) {
                const input = document.getElementById(id);
                if (!input) return;
                input.addEventListener('input', function () {
                    if (typeof Page_Validators !== 'undefined') {
                        for (let i = 0; i < Page_Validators.length; i++) {
                            if (Page_Validators[i].controltovalidate === input.id) ValidatorValidate(Page_Validators[i]);
                        }
                    }
                });
            });

            const pw = document.getElementById('Password');
            const confirm = document.getElementById('ConfirmPassword');
            const bar = document.getElementById('passwordStrengthBar');
            const text = document.getElementById('passwordStrengthText');
            const match = document.getElementById('passwordMatchText');
            const rules = document.querySelectorAll('#passwordRules li');

            // These rules mirror the server-side checks in Register.aspx.cs
            function evaluate(v) {
                return { len: v.length >= 8, upper: /[A-Z]/.test(v), lower: /[a-z]/.test(v), digit: /[0-9]/.test(v) };
            }

            function checkMatch() {
                if (!confirm || !pw) return;
                if (confirm.value === '') { match.textContent = ''; return; }
                const ok = confirm.value === pw.value;
                match.textContent = ok ? 'Passwords match' : 'Passwords do not match';
                match.style.color = ok ? 'var(--gg-accent)' : '#c0392b';
            }

            pw?.addEventListener('input', function () {
                const r = evaluate(pw.value);
                let score = 0;
                rules.forEach(function (li) {
                    const ok = r[li.dataset.rule];
                    li.classList.toggle('ok', ok);
                    if (ok) score += 25;
                });
                const extra = /[^a-zA-Z0-9]/.test(pw.value) && score === 100;   // symbol bonus label only
                bar.style.width = score + '%';
                const levels = [
                    [25, '#c0392b', 'Weak password'], [50, '#d68910', 'Fair password'],
                    [75, '#2b6cb0', 'Good password'], [100, '#2d6a4f', extra ? 'Very strong password' : 'Strong password']
                ];
                const lv = score === 0 ? null : levels.find(function (l) { return score <= l[0]; });
                bar.style.backgroundColor = lv ? lv[1] : 'transparent';
                text.textContent = lv ? lv[2] : '';
                text.style.color = lv ? lv[1] : '';
                checkMatch();
            });
            confirm?.addEventListener('input', checkMatch);
        });
    </script>
</asp:Content>
