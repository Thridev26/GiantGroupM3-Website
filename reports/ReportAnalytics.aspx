
<%@ Page Title="Report Analytics"
    Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="ReportAnalytics.aspx.cs"
    Inherits="GiantGroupM3.ReportAnalytics" %>

<asp:Content ID="Content1"
    ContentPlaceHolderID="MainContent"
    runat="server">

    <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.8/dist/chart.umd.min.js"></script>

    <style>
        .analytics-page {
            max-width: 1400px;
            margin: auto;
            padding: 35px 15px;
        }

        .analytics-heading {
            color: #1b4332;
            font-weight: 800;
        }

        .analytics-subtitle {
            color: #5a6f64;
            margin-bottom: 25px;
        }

        .analytics-card {
            background: #fff;
            border: 1px solid #d8e8dd;
            border-radius: 16px;
            padding: 24px;
            margin-bottom: 22px;
            box-shadow: 0 5px 20px rgba(27,67,50,.05);
        }

        .analytics-label {
            color: #1b4332;
            display: block;
            font-weight: 600;
            margin-bottom: 8px;
        }

        .analytics-button {
            background: #2d6a4f;
            border: 0;
            border-radius: 8px;
            color: #fff;
            font-weight: 600;
            padding: 11px 24px;
        }

        .analytics-button:hover {
            background: #1b4332;
            color: #fff;
        }

        .analytics-tabs {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 12px;
            margin-bottom: 24px;
        }

        .analytics-tab {
            border: 1px solid #d8e8dd;
            background: #fff;
            color: #1b4332;
            border-radius: 10px;
            padding: 15px 10px;
            font-weight: 700;
            cursor: pointer;
            transition: .2s;
        }

        .analytics-tab:hover {
            border-color: #2d6a4f;
            background: #e2f0e8;
        }

        .analytics-tab.active {
            background: #1b4332;
            color: #fff;
        }

        .report-panel {
            display: none;
        }

        .report-panel.active {
            display: block;
        }

        .report-panel h3 {
            color: #1b4332;
            font-weight: 700;
            margin-bottom: 10px;
        }

        .report-description {
            color: #5a6f64;
            margin-bottom: 22px;
        }

        .analytics-chart {
            height: 430px;
            position: relative;
            width: 100%;
        }

        .analytics-note {
            margin-top: 18px;
            font-size: 13px;
            color: #5a6f64;
        }

        .analytics-kpi {
            background: #f2f7f4;
            border: 1px solid #d8e8dd;
            border-radius: 10px;
            padding: 15px;
            color: #456253;
            font-size: 13px;
            min-height: 105px;
        }
        .analytics-kpi strong {
            display: block;
            margin-top: 10px;
            font-size: 21px;
            color: #1b4332;
            overflow-wrap: anywhere;
        }

        @media (max-width: 767px) {
            .analytics-tabs {
                grid-template-columns: repeat(2, 1fr);
            }

            .analytics-chart {
                height: 320px;
            }
        }
    </style>

    <div class="analytics-page">

        <h2 class="analytics-heading">
            <i class="bi bi-graph-up-arrow me-2"></i>
            Giant Group Report Analytics
        </h2>

        <p class="analytics-subtitle">
            Owner dashboard for financial performance,
            service profitability, quotations and client value.
        </p>

        <!-- FILTERS -->
        <div class="analytics-card">
            <div class="row g-3 align-items-end">

                <div class="col-md-3">
                    <label class="analytics-label">
                        Start Date
                    </label>
                    <asp:TextBox
                        ID="txtStartDate"
                        runat="server"
                        TextMode="Date"
                        CssClass="form-control" />
                </div>

                <div class="col-md-3">
                    <label class="analytics-label">
                        End Date
                    </label>
                    <asp:TextBox
                        ID="txtEndDate"
                        runat="server"
                        TextMode="Date"
                        CssClass="form-control" />
                </div>

                <div class="col-md-3">
                    <label class="analytics-label">
                        Client Type
                    </label>
                    <asp:DropDownList
                        ID="ddlClientType"
                        runat="server"
                        CssClass="form-select">
                        <asp:ListItem Text="All clients" Value="All" />
                    </asp:DropDownList>
                </div>

                <div class="col-md-3">
                    <asp:Button
                        ID="btnLoad"
                        runat="server"
                        Text="Generate Analytics"
                        CssClass="analytics-button"
                        OnClick="btnLoad_Click" />
                </div>

            </div>

            <asp:Label
                ID="lblMessage"
                runat="server"
                ForeColor="Red" />
        </div>

        <!-- DATA FIELDS -->
        <asp:HiddenField ID="hfIncomeData"
            runat="server" ClientIDMode="Static" />

        <asp:HiddenField ID="hfExpenseData"
            runat="server" ClientIDMode="Static" />

        <asp:HiddenField ID="hfProfitData"
            runat="server" ClientIDMode="Static" />

        <asp:HiddenField ID="hfServiceData"
            runat="server" ClientIDMode="Static" />

        <asp:HiddenField ID="hfQuoteData"
            runat="server" ClientIDMode="Static" />

        <asp:HiddenField ID="hfClientData"
            runat="server" ClientIDMode="Static" />

        <asp:HiddenField ID="hfAreaData"
            runat="server" ClientIDMode="Static" />

        <asp:HiddenField ID="hfWebsiteData"
            runat="server" ClientIDMode="Static" />

        <asp:HiddenField ID="hfActiveReport"
            runat="server" ClientIDMode="Static"
            Value="income" />

        <!-- REPORT TABS -->
        <div class="analytics-tabs">

            <button type="button" class="analytics-tab active"
                data-report="income">
                Income
            </button>

            <button type="button" class="analytics-tab"
                data-report="expenses">
                Expenses
            </button>

            <button type="button" class="analytics-tab"
                data-report="profits">
                Profits
            </button>

            <button type="button" class="analytics-tab"
                data-report="services">
                Service Profitability
            </button>

            <button type="button" class="analytics-tab"
                data-report="quotes">
                Quote Conversion
            </button>

            <button type="button" class="analytics-tab"
                data-report="clients">
                Client Value Analysis
            </button>

            <button type="button" class="analytics-tab"
                data-report="areas">Popular Areas</button>
            <button type="button" class="analytics-tab"
                data-report="website">Website Impact</button>

        </div>

        <!-- INCOME -->
        <div class="analytics-card report-panel active"
            id="panel-income">

            <h3>Income Over Time</h3>
            <p class="report-description">
                Payments received during the selected period.
            </p>

            <div class="analytics-chart">
                <canvas id="incomeChart"></canvas>
            </div>
        </div>

        <!-- EXPENSES -->
        <div class="analytics-card report-panel"
            id="panel-expenses">

            <h3>Expense Breakdown</h3>
            <p class="report-description">
                Fuel and dumping expenses recorded against jobs.
            </p>

            <div class="analytics-chart">
                <canvas id="expenseChart"></canvas>
            </div>
        </div>

        <!-- PROFITS -->
        <div class="analytics-card report-panel"
            id="panel-profits">

            <h3>Income, Expenses and Profit</h3>
            <p class="report-description">
                Compare income and operating costs by job date.
            </p>

            <div class="analytics-chart">
                <canvas id="profitChart"></canvas>
            </div>
        </div>

        <!-- SERVICE PROFITABILITY -->
        <div class="analytics-card report-panel"
            id="panel-services">

            <h3>Profitability by Service</h3>
            <p class="report-description">
                Compare the profit or loss generated
                by each service type.
            </p>

            <div class="analytics-chart">
                <canvas id="serviceChart"></canvas>
            </div>
        </div>

        <!-- QUOTE CONVERSION -->
        <div class="analytics-card report-panel"
            id="panel-quotes">

            <h3>Quote Conversion Rate</h3>
            <p class="report-description">
                Percentage of requests resulting
                in accepted quotations, grouped by request source.
            </p>

            <div class="analytics-chart">
                <canvas id="quoteChart"></canvas>
            </div>
        </div>

        <!-- CLIENT VALUE -->
        <div class="analytics-card report-panel"
            id="panel-clients">

            <h3>Top 10 Clients by Payments Received</h3>
            <p class="report-description">
                Highest-value clients for the selected
                request period and client type.
            </p>

            <div class="analytics-chart">
                <canvas id="clientChart"></canvas>
            </div>
        </div>

        <!-- POPULAR AREAS -->
        <div class="report-panel" id="panel-areas">
            <div class="analytics-card">
                <h3>Most Popular Service Areas</h3>
                <p class="report-description">
                    Top 10 areas by number of job requests received during
                    the selected period. Areas are derived from the last
                    comma-separated part of the site address.
                </p>
                <div class="row g-3">
                    <div class="col-lg-6">
                        <h4>Requests by Area</h4>
                        <div class="analytics-chart">
                            <canvas id="areaRequestsChart"></canvas>
                        </div>
                    </div>
                    <div class="col-lg-6">
                        <h4>Estimated Profit from Completed Jobs</h4>
                        <div class="analytics-chart">
                            <canvas id="areaProfitChart"></canvas>
                        </div>
                    </div>
                </div>
                <p class="analytics-note">Profit estimate: recorded payments
                    minus fuel and dumping costs for completed jobs. This is
                    not full accounting profit.</p>
            </div>
            <div class="analytics-card">
                <h4>Area Summary</h4>
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead>
                            <tr>
                                <th>Area</th><th>Requests</th>
                                <th>Completed Jobs</th>
                                <th>Payments Received</th>
                                <th>Estimated Profit</th>
                            </tr>
                        </thead>
                        <tbody id="areaResultsBody"></tbody>
                    </table>
                </div>
                <p class="analytics-note">Payments include all payments
                    recorded to date for jobs associated with requests
                    submitted in the selected date range.</p>
            </div>
        </div>

        <!-- WEBSITE IMPACT -->
        <div class="report-panel" id="panel-website">
            <div class="analytics-card">
                <h3>Website Request &amp; Revenue Impact</h3>
                <p class="report-description">
                    Compares job requests marked Website against all other
                    request sources. Helps assess how much recorded business
                    originates through the website.
                </p>
                <div class="row g-3" id="websiteSummaryKpis">
                    <div class="col-md-3">
                        <div class="analytics-kpi">
                            <div>Website Requests</div>
                            <strong id="websiteRequestsKpi">0</strong>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="analytics-kpi">
                            <div>Website Request Share</div>
                            <strong id="websiteShareKpi">0%</strong>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="analytics-kpi">
                            <div>Payments from Website Requests</div>
                            <strong id="websitePaymentsKpi">R 0.00</strong>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="analytics-kpi">
                            <div>Estimated Completed-Job Profit</div>
                            <strong id="websiteProfitKpi">R 0.00</strong>
                        </div>
                    </div>
                </div>
                <div class="row g-3 mt-2">
                    <div class="col-lg-6">
                        <h4>Website vs Other Requests</h4>
                        <div class="analytics-chart">
                            <canvas id="websiteRequestsChart"></canvas>
                        </div>
                    </div>
                    <div class="col-lg-6">
                        <h4>Payments Received by Request Source</h4>
                        <div class="analytics-chart">
                            <canvas id="websitePaymentsChart"></canvas>
                        </div>
                    </div>
                    <div class="col-12">
                        <h4>Monthly Website-Originated Requests</h4>
                        <div class="analytics-chart">
                            <canvas id="websiteMonthlyChart"></canvas>
                        </div>
                    </div>
                </div>
                <p class="analytics-note">
                    A request is considered online when its requestSource is
                    Website. Payments are all recorded payments to date for
                    jobs linked to requests from the selected period.
                    This measures recorded website-originated business,
                    not proof that marketing caused the requests.
                </p>
            </div>
        </div>

        <p class="analytics-note">
            Figures are retrieved from GroupWst1.
            Date filters apply to all eight reports;
            Client Type applies to Client Value Analysis only.
        </p>

    </div>

    <script type="text/javascript">
        document.addEventListener("DOMContentLoaded", function () {

            function readData(id) {
                var field = document.getElementById(id);
                try {
                    return field && field.value
                        ? JSON.parse(field.value)
                        : { labels: [], values: [] };
                }
                catch (e) {
                    console.error("Invalid chart data:", id, e);
                    return { labels: [], values: [] };
                }
            }

            var income = readData("hfIncomeData");
            var expenses = readData("hfExpenseData");
            var profits = readData("hfProfitData");
            var services = readData("hfServiceData");
            var quotes = readData("hfQuoteData");
            var clients = readData("hfClientData");
            var areas = readData("hfAreaData");
            var website = readData("hfWebsiteData");

            var green = "#2d6a4f";
            var dark = "#1b4332";
            var light = "#52b788";
            var orange = "#e0a84a";

            function money(value) {
                return "R " + Number(value || 0).toLocaleString(
                    "en-ZA", {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2
                });
            }

            function dataset(label, values, color) {
                return {
                    label: label,
                    data: values || [],
                    backgroundColor: color,
                    borderColor: color,
                    borderWidth: 2,
                    borderRadius: 5
                };
            }

            function commonOptions(horizontal, percent) {
                return {
                    responsive: true,
                    maintainAspectRatio: false,
                    indexAxis: horizontal ? "y" : "x",
                    plugins: {
                        legend: { position: "bottom" },
                        tooltip: {
                            callbacks: {
                                label: function (ctx) {
                                    var v = horizontal
                                        ? ctx.parsed.x
                                        : ctx.parsed.y;

                                    return ctx.dataset.label + ": " +
                                        (percent
                                            ? Number(v).toFixed(2) + "%"
                                            : money(v));
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: !horizontal
                        },
                        x: {
                            beginAtZero: horizontal,
                            max: percent && horizontal ? 100 : undefined
                        }
                    }
                };
            }

            if (typeof Chart === "undefined") {
                console.error("Chart.js failed to load.");
                return;
            }

            var charts = {};

            // 1. Income
            charts.income = new Chart(
                document.getElementById("incomeChart"),
                {
                    type: "bar",
                    data: {
                        labels: income.labels || [],
                        datasets: [
                            dataset("Income", income.values, green)
                        ]
                    },
                    options: commonOptions(false, false)
                }
            );

            // 2. Expenses
            charts.expenses = new Chart(
                document.getElementById("expenseChart"),
                {
                    type: "doughnut",
                    data: {
                        labels: expenses.labels || [],
                        datasets: [{
                            label: "Expenses",
                            data: expenses.values || [],
                            backgroundColor: [dark, light],
                            borderWidth: 3,
                            borderColor: "#ffffff"
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        cutout: "60%",
                        plugins: {
                            legend: { position: "bottom" },
                            tooltip: {
                                callbacks: {
                                    label: function (ctx) {
                                        return ctx.label + ": " +
                                            money(ctx.parsed);
                                    }
                                }
                            }
                        }
                    }
                }
            );

            // 3. Profits
            charts.profits = new Chart(
                document.getElementById("profitChart"),
                {
                    type: "line",
                    data: {
                        labels: profits.labels || [],
                        datasets: [
                            dataset("Income",
                                profits.income, light),
                            dataset("Expenses",
                                profits.expenses, orange),
                            dataset("Profit",
                                profits.profit, green)
                        ]
                    },
                    options: commonOptions(false, false)
                }
            );

            // 4. Service profitability
            charts.services = new Chart(
                document.getElementById("serviceChart"),
                {
                    type: "bar",
                    data: {
                        labels: services.labels || [],
                        datasets: [{
                            label: "Profit",
                            data: services.values || [],
                            backgroundColor:
                                (services.values || []).map(
                                    function (v) {
                                        return v < 0
                                            ? "#cc5555"
                                            : green;
                                    }
                                ),
                            borderRadius: 5
                        }]
                    },
                    options: commonOptions(true, false)
                }
            );

            // 5. Quote conversion
            charts.quotes = new Chart(
                document.getElementById("quoteChart"),
                {
                    type: "bar",
                    data: {
                        labels: quotes.labels || [],
                        datasets: [
                            dataset("Conversion Rate (%)",
                                quotes.values, green)
                        ]
                    },
                    options: commonOptions(true, true)
                }
            );

            // 6. Client value
            charts.clients = new Chart(
                document.getElementById("clientChart"),
                {
                    type: "bar",
                    data: {
                        labels: clients.labels || [],
                        datasets: [
                            dataset("Payments Received",
                                clients.values, green)
                        ]
                    },
                    options: commonOptions(true, false)
                }
            );


            // 7. Most popular areas — requests and completed-job cash profit
            charts.areas = new Chart(
                document.getElementById("areaRequestsChart"),
                {
                    type: "bar",
                    data: {
                        labels: areas.labels || [],
                        datasets: [dataset("Requests", areas.requests || [], green)]
                    },
                    options: {
                        indexAxis: "y",
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                callbacks: {
                                    label: function (ctx) { return "Requests: " + ctx.parsed.x; }
                                }
                            }
                        },
                        scales: {
                            x: { beginAtZero: true, ticks: { precision: 0 } },
                            y: { beginAtZero: true }
                        }
                    }
                }
            );

            var areaProfits = new Chart(
                document.getElementById("areaProfitChart"),
                {
                    type: "bar",
                    data: {
                        labels: areas.labels || [],
                        datasets: [{
                            label: "Estimated Profit",
                            data: areas.profit || [],
                            backgroundColor: (areas.profit || []).map(
                                function (v) { return v < 0 ? "#c95b5b" : green; }
                            )
                        }]
                    },
                    options: commonOptions(true, false)
                }
            );

            // Accessible summary table: use textContent, not HTML interpolation
            var areaBody = document.getElementById("areaResultsBody");
            (areas.labels || []).forEach(function (label, i) {
                var tr = document.createElement("tr");
                [
                    label,
                    (areas.requests || [])[i] || 0,
                    (areas.completedJobs || [])[i] || 0,
                    money((areas.payments || [])[i]),
                    money((areas.profit || [])[i])
                ].forEach(function (value) {
                    var td = document.createElement("td");
                    td.textContent = String(value);
                    tr.appendChild(td);
                });
                areaBody.appendChild(tr);
            });
            if (!(areas.labels || []).length) {
                var tr = document.createElement("tr");
                var td = document.createElement("td");
                td.colSpan = 5;
                td.textContent = "No requests found for the selected period.";
                tr.appendChild(td);
                areaBody.appendChild(tr);
            }

            // 8. Website source impact
            document.getElementById("websiteRequestsKpi").textContent =
                ((website.requests || [])[0] || 0).toLocaleString("en-ZA");
            document.getElementById("websiteShareKpi").textContent =
                Number(website.websiteRequestShare || 0).toFixed(1) + "%";
            document.getElementById("websitePaymentsKpi").textContent =
                money((website.payments || [])[0]);
            document.getElementById("websiteProfitKpi").textContent =
                money((website.profit || [])[0]);

            charts.website = new Chart(
                document.getElementById("websiteRequestsChart"),
                {
                    type: "bar",
                    data: {
                        labels: website.labels || ["Website", "Other Sources"],
                        datasets: [dataset("Requests", website.requests || [0, 0], green)]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
                    }
                }
            );

            var sourcePaymentsChart = new Chart(
                document.getElementById("websitePaymentsChart"),
                {
                    type: "bar",
                    data: {
                        labels: website.labels || ["Website", "Other Sources"],
                        datasets: [
                            dataset("Payments Received", website.payments || [0, 0], light),
                            dataset("Estimated Completed-Job Profit", website.profit || [0, 0], green)
                        ]
                    },
                    options: commonOptions(false, false)
                }
            );

            var websiteMonthlyChart = new Chart(
                document.getElementById("websiteMonthlyChart"),
                {
                    type: "line",
                    data: {
                        labels: website.websiteMonthlyLabels || [],
                        datasets: [{
                            label: "Website Requests",
                            data: website.websiteMonthlyRequests || [],
                            borderColor: green,
                            backgroundColor: light,
                            pointRadius: 5,
                            tension: 0.25,
                            fill: false
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { position: "bottom" } },
                        scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
                    }
                }
            );

            // Tab switching
            function showReport(name) {
                var panels =
                    document.querySelectorAll(".report-panel");
                var buttons =
                    document.querySelectorAll(".analytics-tab");

                panels.forEach(function (panel) {
                    panel.classList.toggle(
                        "active",
                        panel.id === "panel-" + name
                    );
                });

                buttons.forEach(function (button) {
                    button.classList.toggle(
                        "active",
                        button.getAttribute("data-report") === name
                    );
                });

                document.getElementById("hfActiveReport").value = name;

                if (charts[name]) {
                    charts[name].resize();
                    charts[name].update();
                }
                if (name === "areas") {
                    areaProfits.resize();
                    areaProfits.update();
                }
                if (name === "website") {
                    sourcePaymentsChart.resize();
                    sourcePaymentsChart.update();
                    websiteMonthlyChart.resize();
                    websiteMonthlyChart.update();
                }
            }

            document.querySelectorAll(".analytics-tab")
                .forEach(function (button) {
                    button.addEventListener("click", function () {
                        showReport(
                            button.getAttribute("data-report")
                        );
                    });
                });

            var selected =
                document.getElementById("hfActiveReport").value;

            var allowed = [
                "income", "expenses", "profits",
                "services", "quotes", "clients",
                "areas", "website"
            ];

            showReport(
                allowed.indexOf(selected) >= 0
                    ? selected
                    : "income"
            );
        });
    </script>

</asp:Content>
