<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="VerifyOtp.aspx.cs" Inherits="GiantGroupM3.Account.VerifyOtp" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Verify OTP - The Giant Group</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link rel="shortcut icon" type="image/x-icon" href="~/Content/New%20Giant%20Tree%20Icon%20(1).ico" runat="server" />
</head>
<body class="bg-light d-flex align-items-center justify-content-center vh-100">
    <form id="form1" runat="server">
        <div class="card p-4 shadow-sm" style="width: 400px;">
            <h3 class="mb-3 fw-bold">Verify OTP</h3>
            <p class="text-muted small">Please enter the 6-digit code sent to your email.</p>

            <div class="alert alert-danger" id="ErrorBox" runat="server" visible="false">
                <asp:Literal runat="server" ID="FailureText" />
            </div>

            <div class="mb-3">
                <label class="form-label">OTP Code</label>
                <asp:TextBox runat="server" ID="OtpCode" CssClass="form-control" placeholder="Enter code" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="OtpCode" CssClass="text-danger small mt-1 d-block" ErrorMessage="OTP is required." />
            </div>

            <div class="d-grid">
                <asp:Button runat="server" ID="VerifyButton" OnClick="VerifyOtp_Click" Text="Verify Code" CssClass="btn btn-success py-2 fw-bold" />
            </div>
        </div>
    </form>
</body>
</html>