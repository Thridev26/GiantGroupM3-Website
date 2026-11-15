<%@ Page Title="Update Profile" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ClientProfile.aspx.cs" Inherits="GiantGroupM3.ClientProfile" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .profile-wrap { max-width: 900px; margin: 0 auto; }
        .profile-card { padding: 1.5rem; }
        .profile-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
        .profile-field label { display:block; margin-bottom:.4rem; font-weight:600; color:var(--gg-text); }
        .profile-field .form-control, .profile-field .form-select { width:100%; }
        .profile-help { color:var(--gg-muted); font-size:.88rem; }
        .profile-field-wide { grid-column: 1 / -1; }
        .profile-field-wide .form-control { max-width: calc(50% - .5rem); }
        @media(max-width:600px) { .profile-grid { grid-template-columns:1fr; } .profile-field-wide .form-control { max-width: 100%; } }
    </style>

    <div class="profile-wrap my-4 px-lg-2">
        <div class="gg-banner mb-4">
            <h1 class="mb-2">Update your profile</h1>
            <p class="mb-0">Keep your contact details up to date so we can reach you about your service requests.</p>
        </div>

        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="gg-alert mb-3" role="status">
            <asp:Literal ID="litMessage" runat="server" />
        </asp:Panel>

        <asp:ValidationSummary ID="vsProfile" runat="server" ValidationGroup="Profile"
            CssClass="gg-alert mb-3" HeaderText="Please correct the following:" />

        <div class="gg-surface profile-card">
            <div class="profile-grid">
                <div class="profile-field">
                    <label for="<%= txtFirstName.ClientID %>">First name</label>
                    <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-control" MaxLength="40" />
                    <asp:RequiredFieldValidator ID="rfvFirstName" runat="server" ControlToValidate="txtFirstName"
                        ValidationGroup="Profile" ErrorMessage="First name is required." Display="Dynamic" CssClass="text-danger" />
                </div>
                <div class="profile-field">
                    <label for="<%= txtSurname.ClientID %>">Surname</label>
                    <asp:TextBox ID="txtSurname" runat="server" CssClass="form-control" MaxLength="40" />
                    <asp:RequiredFieldValidator ID="rfvSurname" runat="server" ControlToValidate="txtSurname"
                        ValidationGroup="Profile" ErrorMessage="Surname is required." Display="Dynamic" CssClass="text-danger" />
                </div>
                <div class="profile-field">
                    <label for="<%= txtEmail.ClientID %>">Email address</label>
                    <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" TextMode="Email" MaxLength="50" />
                    <asp:RequiredFieldValidator ID="rfvEmail" runat="server" ControlToValidate="txtEmail"
                        ValidationGroup="Profile" ErrorMessage="Email address is required." Display="Dynamic" CssClass="text-danger" />
                    <asp:RegularExpressionValidator ID="revEmail" runat="server" ControlToValidate="txtEmail"
                        ValidationGroup="Profile" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$"
                        ErrorMessage="Enter a valid email address." Display="Dynamic" CssClass="text-danger" />
                </div>
                <div class="profile-field">
                    <label for="<%= txtPhone.ClientID %>">Phone number</label>
                    <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" MaxLength="15" />
                    <asp:RequiredFieldValidator ID="rfvPhone" runat="server" ControlToValidate="txtPhone"
                        ValidationGroup="Profile" ErrorMessage="Phone number is required." Display="Dynamic" CssClass="text-danger" />
                </div>
            </div>

            <p class="profile-help mt-3 mb-4">Your client type and account status are managed by Giant Group and cannot be changed here.</p>

            <div class="d-flex flex-wrap gap-2">
                <asp:Button ID="btnSave" runat="server" Text="Save changes" CssClass="gg-btn gg-btn-primary"
                    ValidationGroup="Profile" OnClick="btnSave_Click" />
            </div>
        </div>

        <asp:Panel ID="pnlPasswordMessage" runat="server" Visible="false" CssClass="gg-msg mt-4 mb-3" role="status">
            <asp:Literal ID="litPasswordMessage" runat="server" />
        </asp:Panel>

        <asp:ValidationSummary ID="vsPassword" runat="server" ValidationGroup="Password"
            CssClass="gg-alert mt-4 mb-3" HeaderText="Please correct the following:" />

        <div class="gg-surface profile-card mt-4">
            <h2 class="h5 mb-1">Change your password</h2>
            <p class="profile-help mb-3">For your security, enter your current password first. Your new password must be 8 to 72 characters long and include an uppercase letter, a lowercase letter and a number.</p>
            <div class="profile-grid">
                <div class="profile-field profile-field-wide">
                    <label for="<%= txtCurrentPassword.ClientID %>">Current password</label>
                    <asp:TextBox ID="txtCurrentPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="72" autocomplete="current-password" />
                    <asp:RequiredFieldValidator ID="rfvCurrentPassword" runat="server" ControlToValidate="txtCurrentPassword"
                        ValidationGroup="Password" ErrorMessage="Enter your current password." Display="Dynamic" CssClass="text-danger" />
                </div>
                <div class="profile-field">
                    <label for="<%= txtNewPassword.ClientID %>">New password</label>
                    <asp:TextBox ID="txtNewPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="72" autocomplete="new-password" />
                    <asp:RequiredFieldValidator ID="rfvNewPassword" runat="server" ControlToValidate="txtNewPassword"
                        ValidationGroup="Password" ErrorMessage="Enter a new password." Display="Dynamic" CssClass="text-danger" />
                </div>
                <div class="profile-field">
                    <label for="<%= txtConfirmPassword.ClientID %>">Confirm new password</label>
                    <asp:TextBox ID="txtConfirmPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="72" autocomplete="new-password" />
                    <asp:RequiredFieldValidator ID="rfvConfirmPassword" runat="server" ControlToValidate="txtConfirmPassword"
                        ValidationGroup="Password" ErrorMessage="Confirm your new password." Display="Dynamic" CssClass="text-danger" />
                    <asp:CompareValidator ID="cvConfirmPassword" runat="server" ControlToValidate="txtConfirmPassword" ControlToCompare="txtNewPassword"
                        ValidationGroup="Password" ErrorMessage="The new passwords do not match." Display="Dynamic" CssClass="text-danger" />
                </div>
            </div>
            <div class="d-flex flex-wrap gap-2 mt-4">
                <asp:Button ID="btnChangePassword" runat="server" Text="Update password" CssClass="gg-btn gg-btn-primary"
                    ValidationGroup="Password" OnClick="btnChangePassword_Click" />
            </div>
        </div>
    </div>
</asp:Content>
