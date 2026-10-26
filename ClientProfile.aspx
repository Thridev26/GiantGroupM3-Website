<%@ Page Title="Update Profile" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ClientProfile.aspx.cs" Inherits="GiantGroupM3.ClientProfile" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .profile-wrap { max-width: 900px; margin: 0 auto; }
        .profile-card { padding: 1.5rem; }
        .profile-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
        .profile-field label { display:block; margin-bottom:.4rem; font-weight:600; color:var(--gg-text); }
        .profile-field .form-control, .profile-field .form-select { width:100%; }
        .profile-help { color:var(--gg-muted); font-size:.88rem; }
        @media(max-width:600px) { .profile-grid { grid-template-columns:1fr; } }
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
                <a runat="server" href="~/ClientPortal.aspx" class="gg-btn gg-btn-outline">Back to portal</a>
            </div>
        </div>
    </div>
</asp:Content>
