<%@ Page Title="Submit a Service Request" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="SubmitRequest.aspx.cs" Inherits="GiantGroupM3.Requests.SubmitRequest" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" integrity="sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=" crossorigin="" />
    <style>
        .request-wrap { max-width: 1000px; margin: 2rem auto; }
        .request-card { padding: 1.5rem; margin-bottom: 1.25rem; border: 1px solid var(--gg-border, #d9e2dc); border-radius: 12px; background: var(--gg-surface, #fff); }
        .request-card h2 { font-size: 1.2rem; margin-bottom: 1rem; }
        .request-help { color: var(--gg-muted, #637168); font-size: .9rem; }
        #siteMap { width: 100%; height: 380px; border-radius: 8px; z-index: 1; }
        .job-type-list label { margin-left: .35rem; margin-right: 1.1rem; }
        .request-actions { display: flex; flex-wrap: wrap; gap: .75rem; }
        .map-search-actions { display: flex; flex-wrap: wrap; gap: .5rem; margin-top: .5rem; }
        #geocodeStatus { min-height: 1.4rem; }
        @media (max-width: 575px) { #siteMap { height: 300px; } .request-card { padding: 1rem; } }
    </style>

    <div class="request-wrap px-3">
        <h1>Submit a Service Request</h1>
        <p class="request-help">Tell us what work you need and identify the site on the map. Fields marked * are required.</p>

        <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert alert-success" role="status">
            <strong>Your request was submitted successfully.</strong>
            Your request number is <strong>#<asp:Literal ID="litRequestId" runat="server" /></strong>.
            You can view it at any time in <a href="~/Requests/ViewRequests.aspx" runat="server">your request history</a>.
        </asp:Panel>
        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
            <asp:Literal ID="litError" runat="server" Mode="Encode" />
        </asp:Panel>

        <div class="request-card">
            <h2>1. Site details</h2>
            <div class="mb-3">
                <asp:Label ID="lblSiteAddress" runat="server" AssociatedControlID="txtSiteAddress" CssClass="form-label" Text="Site address *" />
                <asp:TextBox ID="txtSiteAddress" runat="server" CssClass="form-control" MaxLength="100" />
                <asp:RequiredFieldValidator ID="rfvSiteAddress" runat="server" ControlToValidate="txtSiteAddress" ErrorMessage="Enter the site address." CssClass="text-danger" Display="Dynamic" />
                <div class="form-text">Maximum 100 characters, matching the current database field. After entering the address, move to the next field and the map will look it up automatically.</div>
            </div>
            <div class="mb-2">
                <label class="form-label" for="siteMap">Confirm the site location on the map *</label>
                <p class="request-help">The map will search for your address when you leave the address field. You can adjust the marker within 500 metres of the located address. If the suggested address is wrong, correct the address before continuing.</p>
                <div id="siteMap" aria-label="Map for selecting the job site location"></div>
                <div id="geocodeStatus" class="request-help mt-2" role="status" aria-live="polite"></div>
                <asp:HiddenField ID="hfLatitude" runat="server" />
                <asp:HiddenField ID="hfLongitude" runat="server" />
                <p class="request-help mt-2 mb-0">Selected coordinates: <span id="selectedCoordinates">No location selected</span></p>
            </div>
        </div>

        <div class="request-card">
            <h2>2. Type of work</h2>
            <p class="request-help">Select one or more services.</p>
            <asp:CheckBoxList ID="cblJobTypes" runat="server" RepeatLayout="Flow" CssClass="job-type-list" />
            <asp:CustomValidator ID="cvJobTypes" runat="server" Display="Dynamic" CssClass="text-danger" ErrorMessage="Select at least one job type." OnServerValidate="ValidateJobTypes" />
        </div>

        <div class="request-card">
            <h2>3. Urgency</h2>
            <div class="mb-3">
                <asp:Label ID="lblUrgency" runat="server" AssociatedControlID="ddlUrgency" CssClass="form-label" Text="How urgent is this request? *" />
                <asp:DropDownList ID="ddlUrgency" runat="server" CssClass="form-select">
                    <asp:ListItem Text="-- Select urgency --" Value="" />
                    <asp:ListItem Text="High" Value="High" />
                    <asp:ListItem Text="Medium" Value="Medium" />
                    <asp:ListItem Text="Low" Value="Low" />
                    <asp:ListItem Text="Not specified" Value="Not Specified" />
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="rfvUrgency" runat="server" ControlToValidate="ddlUrgency" InitialValue="" ErrorMessage="Select an urgency level." CssClass="text-danger" Display="Dynamic" />
            </div>
            <div class="request-help">The request source and date received are recorded automatically. The site evaluation date will remain empty until an administrator sets it.</div>
        </div>

        <div class="request-actions mb-5">
            <asp:Button ID="btnSubmit" runat="server" Text="Submit Request" CssClass="btn btn-success" OnClick="btnSubmit_Click" />
            <a href="~/ClientPortal.aspx" runat="server" class="btn btn-outline-secondary">Cancel</a>
        </div>
    </div>

    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" integrity="sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo=" crossorigin=""></script>
    <script>
        (function () {
            var latField = document.getElementById('<%= hfLatitude.ClientID %>');
            var lngField = document.getElementById('<%= hfLongitude.ClientID %>');
            var addressField = document.getElementById('<%= txtSiteAddress.ClientID %>');
            var submit = document.getElementById('<%= btnSubmit.ClientID %>');
            var status = document.getElementById('geocodeStatus');
            var coordinates = document.getElementById('selectedCoordinates');
            var map = L.map('siteMap').setView([-29, 24], 5);
            var marker, circle, origin, accepted;
            var version = 0, locatedAddress = '', pendingAddress = '';
            var radius = 500;
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19,
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap contributors</a>'
            }).addTo(map);

            function message(text, error) {
                status.textContent = text;
                status.className = error ? 'text-danger mt-2' : 'request-help mt-2';
            }
            function clearLocation() {
                origin = accepted = null;
                locatedAddress = pendingAddress = '';
                if (marker) { map.removeLayer(marker); marker = null; }
                if (circle) { map.removeLayer(circle); circle = null; }
                latField.value = lngField.value = '';
                coordinates.textContent = 'No location selected';
            }
            function saveLocation(point) {
                accepted = point;
                latField.value = point.lat.toFixed(6);
                lngField.value = point.lng.toFixed(6);
                coordinates.textContent = latField.value + ', ' + lngField.value;
            }
            function adjust(point) {
                if (!origin || locatedAddress !== addressField.value.trim()) {
                    message('Enter and locate your site address first.', true);
                    return;
                }
                if (map.distance(origin, point) > radius) {
                    marker.setLatLng(accepted);
                    message('Location rejected: the marker must remain within 500 metres of the located address.', true);
                    return;
                }
                marker.setLatLng(point);
                saveLocation(point);
                message('Site location selected. Please confirm that it is correct.', false);
            }
            async function locate(previous) {
                var address = addressField.value.trim();
                if (address === locatedAddress || address === pendingAddress) return;
                var current = ++version;
                clearLocation();
                if (address.length < 5) {
                    if (address.length) message('Enter a more complete site address.', true);
                    return;
                }
                pendingAddress = address;
                message('Locating your address...', false);
                try {
                    var response = await fetch('<%= ResolveUrl("~/Requests/SubmitRequest.aspx/LocateAddress") %>', {
                        method: 'POST', credentials: 'same-origin',
                        headers: { 'Content-Type': 'application/json; charset=utf-8' },
                        body: JSON.stringify({ address: address })
                    });
                    if (!response.ok) throw new Error('Lookup failed');
                    var result = (await response.json()).d;
                    if (current !== version || address !== addressField.value.trim()) return;
                    if (!result || !result.Success) {
                        message((result && result.Message) || 'Address lookup failed. Please try again.', true);
                        return;
                    }
                    origin = L.latLng(result.Latitude, result.Longitude);
                    locatedAddress = address;
                    circle = L.circle(origin, { radius: radius, color: '#198754', fillOpacity: 0.08, interactive: false }).addTo(map);
                    var point = previous && map.distance(origin, previous) <= radius ? previous : origin;
                    marker = L.marker(point, { draggable: true }).addTo(map);
                    marker.on('dragend', function () { adjust(marker.getLatLng()); });
                    saveLocation(point);
                    map.setView(origin, 16);
                    message('Located: ' + result.DisplayName + '. You may adjust the marker within 500 metres.', false);
                } catch (error) {
                    if (current === version) message('Address lookup is unavailable. Leave the address field again to retry.', true);
                } finally {
                    if (current === version) pendingAddress = '';
                }
            }
            addressField.addEventListener('input', function () {
                ++version;
                clearLocation();
                message('Leave the address field to locate the new address.', false);
            });
            addressField.addEventListener('blur', function () { locate(null); });
            map.on('click', function (event) { adjust(event.latlng); });
            // Do not bypass the normal Web Forms validators; the server also checks the location.
            submit.addEventListener('click', function (event) {
                if (pendingAddress) {
                    event.preventDefault(); event.stopImmediatePropagation();
                    message('Please wait for the address lookup to finish before submitting.', true);
                }
            }, true);
            var previous = latField.value && lngField.value ? L.latLng(Number(latField.value), Number(lngField.value)) : null;
            if (addressField.value.trim()) locate(previous);
            window.setTimeout(function () { map.invalidateSize(); }, 100);
        })();
    </script>
</asp:Content>
