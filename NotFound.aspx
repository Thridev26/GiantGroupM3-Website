<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="NotFound.aspx.cs" Inherits="GiantGroupM3.NotFound" MasterPageFile="~/Site.master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5 my-5">
        <div class="row justify-content-center text-center">
            <div class="col-md-6">
                
                <!-- Crisp, Bold 404 Header -->
                <h1 class="display-1 fw-bold text-success mb-2" style="letter-spacing: -3px;">404</h1>
                <h2 class="h3 fw-bold text-dark mb-3">Branch Not Found</h2>
                
                <p class="text-muted mb-4 px-4">
                    The page you are looking for might have been removed, renamed, or is temporarily unavailable.
                </p>

                <!-- Action Buttons -->
                <div class="d-flex justify-content-center gap-3 mb-4">
                    <a href="~/Default.aspx" runat="server" class="btn text-white px-4 py-2 rounded-pill fw-semibold shadow-sm" style="background-color: #2d6a4f;">
                        Return Home
                    </a>
                    <button onclick="history.back(); return false;" class="btn btn-outline-secondary px-4 py-2 rounded-pill fw-semibold">
                        Go Back
                    </button>
                </div>

                <!-- Subtle Help Link -->
                <div class="text-muted small">
                    Need help? <a href="~/Contact.aspx" runat="server" class="text-success text-decoration-none fw-semibold">Contact Support</a>
                </div>

            </div>
        </div>
    </div>
</asp:Content>