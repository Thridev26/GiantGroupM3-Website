<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Privacy.aspx.cs" Inherits="GiantGroupM3.Privacy" MasterPageFile="~/Site.master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5 my-3" style="max-width: 850px;">
        <div class="card shadow-sm border-0 rounded-4 p-5">
            <h1 class="h3 fw-bold text-dark mb-2">Privacy Policy & Terms of Use</h1>
            <p class="text-muted small mb-4">Last updated: August 2026</p>

            <!-- Academic & Enterprise Disclaimer -->
            <div class="p-3 mb-4 bg-light rounded-3 border-start border-success border-4">
                <p class="text-secondary small mb-0">
                    <strong>Notice:</strong> This web application and platform are developed and operated strictly for academic coursework and educational purposes as part of university curriculum requirements. Operations, system demonstrations, and enterprise workflows are conducted with the full knowledge and authorization of the management and owners of The Giant Group.
                </p>
            </div>

            <h5 class="fw-bold text-success mt-3">1. Information We Collect</h5>
            <p class="text-secondary">
                We collect information you provide directly when creating accounts, submitting service or tree-felling quotes, interacting with our integrated AI assistant features, or uploading media and documents. This may include names, contact details, communications, and uploaded image or file assets.
            </p>

            <h5 class="fw-bold text-success mt-3">2. Use of Artificial Intelligence and Automated Processing</h5>
            <p class="text-secondary">
                Our platform integrates automated technologies and AI-driven assistant features designed to process queries, streamline customer interactions, and enhance system navigation. Inputs, text queries, and system interactions may be processed by automated tools to deliver responses and maintain system functionality.
            </p>

            <h5 class="fw-bold text-success mt-3">3. Media Uploads and Data Storage</h5>
            <p class="text-secondary">
                Users may upload images, documentation, or operational records to the platform. By uploading content, you grant permission for such data to be stored securely within our system databases for operational and evaluation purposes. We employ reasonable security measures to protect stored information; however, absolute security cannot be guaranteed over the internet.
            </p>

            <h5 class="fw-bold text-success mt-3">4. Limitation of Liability ("As-Is" Disclaimer)</h5>
            <p class="text-secondary">
                This platform is provided on an "as-is" and "as-available" basis without warranties of any kind, either express or implied. The developer, student creators, university entities, and The Giant Group accept no liability or responsibility for any direct, indirect, incidental, or consequential damages arising out of the use, misuse, or inability to use this platform, data loss, or system interruption.
            </p>

            <h5 class="fw-bold text-success mt-3">5. Changes to This Policy</h5>
            <p class="text-secondary">
                We reserve the right to modify or update this Privacy Policy at any time without prior notice. Continued use of the platform constitutes your agreement to the updated terms.
            </p>

            <h5 class="fw-bold text-success mt-3">6. Contact Us</h5>
            <p class="text-secondary">
                If you have any questions or concerns regarding this policy or the platform, please reach out to us via our <a href="~/Contact.aspx" runat="server" class="text-success fw-bold text-decoration-none">Contact page</a>.
            </p>
        </div>
    </div>
</asp:Content>