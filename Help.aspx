<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Help.aspx.cs" Inherits="GiantGroupM3.Help" MasterPageFile="~/Site.master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5 my-3">
        <div class="row justify-content-center">
            <div class="col-lg-9">
                
                <!-- Page Header -->
                <div class="text-center mb-5">
                    <h1 class="fw-bold text-dark">System Help & User Guide</h1>
                    <p class="text-muted lead">Everything you need to know about navigating and using The Giant Group Enterprise Portal.</p>
                </div>

                <!-- Accordion / FAQ Sections -->
                <div class="accordion shadow-sm rounded-4 overflow-hidden" id="helpAccordion">
                    
                    <!-- Item 1 -->
                    <div class="accordion-item border-0 border-bottom">
                        <h2 class="accordion-header" id="headingOne">
                            <button class="accordion-button fw-bold text-success bg-light" type="button" data-bs-toggle="collapse" data-bs-target="#collapseOne" aria-expanded="true" aria-controls="collapseOne">
                                <i class="fa fa-tree me-2"></i> How do I request a tree-felling service or quote?
                            </button>
                        </h2>
                        <div id="collapseOne" class="accordion-collapse collapse show" aria-labelledby="headingOne" data-bs-parent="#helpAccordion">
                            <div class="accordion-body text-secondary">
                                You can submit a service or quote request by navigating to our <a runat="server" href="~/Contact.aspx" class="text-success fw-bold text-decoration-none">Contact page</a> and filling out the details. Our team will review your asset allocation or tree-felling requirements and get back to you promptly.
                            </div>
                        </div>
                    </div>

                    <!-- Item 2 -->
                    <div class="accordion-item border-0 border-bottom">
                        <h2 class="accordion-header" id="headingTwo">
                            <button class="accordion-button collapsed fw-bold text-success bg-light" type="button" data-bs-toggle="collapse" data-bs-target="#collapseTwo" aria-expanded="false" aria-controls="collapseTwo">
                                <i class="fa fa-comments me-2"></i> How does the AI Chatbot assistant work?
                            </button>
                        </h2>
                        <div id="collapseTwo" class="accordion-collapse collapse" aria-labelledby="headingTwo" data-bs-parent="#helpAccordion">
                            <div class="accordion-body text-secondary">
                                The interactive assistant widget is available on the bottom right of every page. You can click it anytime to ask quick questions about our enterprise services, navigate system modules, or get immediate support as a guest or logged-in user.
                            </div>
                        </div>
                    </div>

                    <!-- Item 3 -->
                    <div class="accordion-item border-0 border-bottom">
                        <h2 class="accordion-header" id="headingThree">
                            <button class="accordion-button collapsed fw-bold text-success bg-light" type="button" data-bs-toggle="collapse" data-bs-target="#collapseThree" aria-expanded="false" aria-controls="collapseThree">
                                <i class="fa fa-user-shield me-2"></i> Account Roles & Security
                            </button>
                        </h2>
                        <div id="collapseThree" class="accordion-collapse collapse" aria-labelledby="headingThree" data-bs-parent="#helpAccordion">
                            <div class="accordion-body text-secondary">
                                The system utilizes role-based authentication. Guests can view public information and request quotes, while authenticated administrators and managers have access to internal management tools and records.
                            </div>
                        </div>
                    </div>

                    <!-- Item 4: Job Progress Tracking -->
                    <div class="accordion-item border-0 border-bottom">
                        <h2 class="accordion-header" id="headingFour">
                            <button class="accordion-button collapsed fw-bold text-success bg-light" type="button" data-bs-toggle="collapse" data-bs-target="#collapseFour" aria-expanded="false" aria-controls="collapseFour">
                                <i class="fa fa-chart-line me-2"></i> How can I track the progress of my tree-felling job?
                            </button>
                        </h2>
                        <div id="collapseFour" class="accordion-collapse collapse" aria-labelledby="headingFour" data-bs-parent="#helpAccordion">
                            <div class="accordion-body text-secondary">
                                Once you are logged into your account, you can navigate to the Jobs menu and select <strong>View Job Progress</strong> to see real-time status updates, milestone completion, and scheduled timelines for your site operations.
                            </div>
                        </div>
                    </div>

                    <!-- Item 5: Site Photos & Media -->
                    <div class="accordion-item border-0 border-bottom">
                        <h2 class="accordion-header" id="headingFive">
                            <button class="accordion-button collapsed fw-bold text-success bg-light" type="button" data-bs-toggle="collapse" data-bs-target="#collapseFive" aria-expanded="false" aria-controls="collapseFive">
                                <i class="fa fa-camera me-2"></i> How do I upload or view site photos for my property?
                            </button>
                        </h2>
                        <div id="collapseFive" class="accordion-collapse collapse" aria-labelledby="headingFive" data-bs-parent="#helpAccordion">
                            <div class="accordion-body text-secondary">
                                Under the <strong>Job Requests</strong> dropdown menu, authorized users and staff can access <strong>Add Site Photos</strong> to upload pre-cutting or post-cutting imagery of the property for accurate quotation and record-keeping.
                            </div>
                        </div>
                    </div>

                    <!-- Item 6: Equipment and Staff Allocation -->
                    <div class="accordion-item border-0 border-bottom">
                        <h2 class="accordion-header" id="headingSix">
                            <button class="accordion-button collapsed fw-bold text-success bg-light" type="button" data-bs-toggle="collapse" data-bs-target="#collapseSix" aria-expanded="false" aria-controls="collapseSix">
                                <i class="fa fa-tools me-2"></i> How are equipment and staff allocated to a tree-felling project?
                            </button>
                        </h2>
                        <div id="collapseSix" class="accordion-collapse collapse" aria-labelledby="headingSix" data-bs-parent="#helpAccordion">
                            <div class="accordion-body text-secondary">
                                Our management team uses the <strong>Allocation</strong> module to assign specialized machinery (such as chainsaws, chippers, and trucks) and certified personnel to each site, ensuring safe and efficient operations.
                            </div>
                        </div>
                    </div>

                    <!-- Item 7: Quotations and Estimates -->
                    <div class="accordion-item border-0 border-bottom">
                        <h2 class="accordion-header" id="headingSeven">
                            <button class="accordion-button collapsed fw-bold text-success bg-light" type="button" data-bs-toggle="collapse" data-bs-target="#collapseSeven" aria-expanded="false" aria-controls="collapseSeven">
                                <i class="fa fa-file-invoice-dollar me-2"></i> Where can I view and review my formal quotations?
                            </button>
                        </h2>
                        <div id="collapseSeven" class="accordion-collapse collapse" aria-labelledby="headingSeven" data-bs-parent="#helpAccordion">
                            <div class="accordion-body text-secondary">
                                You can view detailed cost breakdowns by navigating to the <strong>Quotations</strong> tab and selecting <strong>View Quote</strong>. Our team compiles these estimates based on tree size, hazard level, and site cleanup requirements.
                            </div>
                        </div>
                    </div>

                    <!-- Item 8: Invoicing and Payments -->
                    <div class="accordion-item border-0">
                        <h2 class="accordion-header" id="headingEight">
                            <button class="accordion-button collapsed fw-bold text-success bg-light" type="button" data-bs-toggle="collapse" data-bs-target="#collapseEight" aria-expanded="false" aria-controls="collapseEight">
                                <i class="fa fa-credit-card me-2"></i> How do I handle invoices and make payments?
                            </button>
                        </h2>
                        <div id="collapseEight" class="accordion-collapse collapse" aria-labelledby="headingEight" data-bs-parent="#helpAccordion">
                            <div class="accordion-body text-secondary">
                                Clients and managers can access the <strong>Invoicing</strong> menu to review issued bills, check payment statuses, and use the <strong>Manage & Make Payments</strong> portal to securely settle accounts once services are completed.
                            </div>
                        </div>
                    </div>

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