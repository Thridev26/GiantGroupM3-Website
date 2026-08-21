<%@ Page Title="Register" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="GiantGroupM3.Account.Register" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <div class="row justify-content-center align-items-center" style="min-height: 75vh;">
        <div class="col-md-6 col-lg-5">
            <div class="card shadow-lg border-0 rounded-4 p-4 p-md-5 bg-white position-relative overflow-hidden">
                
                <!-- Decorative Top Accent Bar matching the Login page -->
                <div class="position-absolute top-0 start-0 w-100" style="height: 6px; background-color: #2e7d32;"></div>

                <!-- Brand / Header Section -->
                <div class="text-center mb-4 mt-2">
                    <h2 class="fw-bold text-dark mb-1">Create Account</h2>
                    <p class="text-muted small">Sign up to join The Giant Group portal</p>
                </div>

                <!-- Error Message Alert -->
<asp:PlaceHolder runat="server" ID="ErrorPlaceHolder" Visible="false">
    <div class="alert alert-danger py-2 small text-center mb-3 rounded-3" role="alert">
        <asp:Literal runat="server" ID="FailureText" />
    </div>
</asp:PlaceHolder>

                <div class="form-horizontal">
                    
                    <!-- First Name Field -->
                    <div class="mb-3">
                        <label class="form-label">First Name</label>
                        <asp:TextBox runat="server" ID="FirstName" CssClass="form-control" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="FirstName" CssClass="text-danger small mt-1 d-block" ErrorMessage="The first name field is required." Display="Dynamic" />
                    </div>

                    <!-- Surname Field -->
                    <div class="mb-3">
                        <label class="form-label">Surname</label>
                        <asp:TextBox runat="server" ID="Surname" CssClass="form-control" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="Surname" CssClass="text-danger small mt-1 d-block" ErrorMessage="The surname field is required." Display="Dynamic" />
                    </div>

                    <div class="mb-3">
    <label class="form-label">Client Type</label>
    <asp:DropDownList ID="ClientType" runat="server" CssClass="form-control">
        <asp:ListItem Text="Residential" Value="Residential" Selected="True" />
        <asp:ListItem Text="Commercial" Value="Commercial" />
    </asp:DropDownList>
</div>

                    <!-- Phone Number Field -->
                    <div class="mb-3">
                        <label class="form-label">Phone Number</label>
                        <asp:TextBox runat="server" ID="PhoneNumber" CssClass="form-control" />
                    </div>

                    <!-- Email Field -->
                    <div class="mb-3">
                        <label class="form-label">Email Address</label>
                        <asp:TextBox runat="server" ID="Email" CssClass="form-control" TextMode="Email" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="Email" CssClass="text-danger small mt-1 d-block" ErrorMessage="The email field is required." Display="Dynamic" />
                    </div>

                    <!-- Password Field -->
                    <div class="mb-3">
                        <label class="form-label">Password</label>
                        <asp:TextBox runat="server" ID="Password" CssClass="form-control" TextMode="Password" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="Password" CssClass="text-danger small mt-1 d-block" ErrorMessage="The password field is required." Display="Dynamic" />
                    </div>

                    <!-- Confirm Password Field -->
                    <div class="mb-3">
                        <label class="form-label">Confirm Password</label>
                        <asp:TextBox runat="server" ID="ConfirmPassword" CssClass="form-control" TextMode="Password" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="ConfirmPassword" CssClass="text-danger small mt-1 d-block" ErrorMessage="The confirm password field is required." Display="Dynamic" />
                        <asp:CompareValidator runat="server" ControlToValidate="ConfirmPassword" ControlToCompare="Password" Operator="Equal" Type="String" CssClass="text-danger small mt-1 d-block" ErrorMessage="The password and confirmation password do not match." Display="Dynamic" />
                    </div>

                    <!-- Submit Button -->
                    <div class="d-grid gap-2 mb-3">
                        <asp:Button runat="server" OnClick="CreateUser_Click" Text="Register" CssClass="btn btn-success w-100 py-2 fw-bold" />
                    </div>

                    <!-- Back to Login Link -->
                    <div class="text-center mt-4">
                        <p class="text-muted small mb-0">
                            Already have an account? 
                            <a href="Login.aspx" class="text-decoration-none fw-semibold" style="color: #2e7d32;">Sign in</a>
                        </p>
                    </div>

                </div>
            </div>
        </div>
    </div>

    <!-- Custom Hover & Focus Styles -->
    <style>
        .btn:hover {
            background-color: #256328 !important;
        }
        .form-control:focus {
            border-color: #2e7d32 !important;
            box-shadow: 0 0 0 0.25rem rgba(46, 125, 50, 0.15) !important;
        }
    </style>
</asp:Content>