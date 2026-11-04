
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
            grid-template-columns: repeat(3, 1fr);
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

        <p class="analytics-note">
            Figures are retrieved from GroupWst1.
            Date filters apply to all six reports;
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
                "services", "quotes", "clients"
            ];

            showReport(
                allowed.indexOf(selected) >= 0
                    ? selected
                    : "income"
            );
        });
    </script>

</asp:Content>
