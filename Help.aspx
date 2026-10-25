<%@ Page Title="Help" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Help.aspx.cs" Inherits="GiantGroupM3.Help" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5 my-3">
        <div class="row justify-content-center">
            <div class="col-lg-9">

                <!-- Page Header -->
                <div class="text-center mb-5">
                    <h1 class="fw-bold text-dark">
                        <asp:Label ID="lblHeading" runat="server" Text="Help &amp; FAQ" />
                    </h1>
                    <p class="text-muted lead">
                        <asp:Label ID="lblSubheading" runat="server" />
                    </p>
                    <asp:Label ID="lblAudience" runat="server" Visible="false" CssClass="badge rounded-pill bg-success px-3 py-2" />
                </div>

                <!-- Guests: point to the full guide without describing the private pages -->
                <asp:Panel ID="pnlGuestNotice" runat="server" Visible="false" CssClass="alert alert-success border-0 rounded-4 shadow-sm d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4" role="note">
                    <div>
                        <strong>Have an account?</strong>
                        <span class="text-secondary">Sign in to see the full step-by-step guide for your account.</span>
                    </div>
                    <a runat="server" href="~/Account/Login.aspx" class="btn btn-success fw-bold px-4">Sign in</a>
                </asp:Panel>

                <!-- Accordion / FAQ Sections (built from the signed-in role in code-behind) -->
                <div class="accordion shadow-sm rounded-4 overflow-hidden" id="helpAccordion">
                    <asp:Repeater ID="rptFaq" runat="server">
                        <ItemTemplate>
                            <div class="accordion-item border-0 border-bottom">
                                <h2 class="accordion-header" id="heading<%# Container.ItemIndex %>">
                                    <button class='accordion-button fw-bold text-success bg-light <%# Container.ItemIndex == 0 ? "" : "collapsed" %>' type="button"
                                        data-bs-toggle="collapse" data-bs-target="#collapse<%# Container.ItemIndex %>"
                                        aria-expanded='<%# Container.ItemIndex == 0 ? "true" : "false" %>' aria-controls="collapse<%# Container.ItemIndex %>">
                                        <i class='fa <%#: Eval("Icon") %> me-2' aria-hidden="true"></i> <%#: Eval("Question") %>
                                    </button>
                                </h2>
                                <div id="collapse<%# Container.ItemIndex %>" class='accordion-collapse collapse <%# Container.ItemIndex == 0 ? "show" : "" %>'
                                    aria-labelledby="heading<%# Container.ItemIndex %>" data-bs-parent="#helpAccordion">
                                    <%-- Answers are trusted, hard-coded strings from Help.aspx.cs (never user input), so they are not encoded. --%>
                                    <div class="accordion-body text-secondary"><%# Eval("Answer") %></div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>

                <!-- Support Callout Box -->
                <div class="card bg-light border-0 rounded-4 p-4 mt-5 text-center">
                    <h5 class="fw-bold text-dark mb-2">Still need assistance?</h5>
                    <p class="text-muted mb-3 small">Our support team is always ready to help you resolve any system issues.</p>
                    <div>
                        <a runat="server" href="~/Contact.aspx" class="btn text-white px-4 py-2 rounded-pill fw-bold shadow-sm" style="background-color: #2d6a4f;">
                            Contact Support
                        </a>
                    </div>
                </div>

            </div>
        </div>
    </div>
</asp:Content>