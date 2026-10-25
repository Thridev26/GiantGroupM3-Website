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
                <p class="request-help">The map will search for your address when you leave the address field. You can also search again below, click the map to move the marker, or drag the marker to adjust it.</p>
                <div id="siteMap" aria-label="Map for selecting the job site location"></div>
                <div class="map-search-actions">
                    <button type="button" id="btnFindAddress" class="btn btn-outline-success btn-sm">Find address on map</button>
                </div>
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
            var findAddressButton = document.getElementById('btnFindAddress');
            var geocodeStatus = document.getElementById('geocodeStatus');
            var coordinateText = document.getElementById('selectedCoordinates');
            var map = L.map('siteMap').setView([-29, 24], 5);
            var marker = null;
            var lastGeocodeAt = 0;
            var geocodeInProgress = false;

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19,
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap contributors</a>'
            }).addTo(map);

            function saveLocation(latlng) {
                latField.value = latlng.lat.toFixed(6);
                lngField.value = latlng.lng.toFixed(6);
                coordinateText.textContent = latField.value + ', ' + lngField.value;
            }

            function setGeocodeStatus(message, isError) {
                geocodeStatus.textContent = message;
                geocodeStatus.className = isError ? 'text-danger mt-2' : 'request-help mt-2';
            }

            function placeMarker(latlng) {
                if (marker) {
                    marker.setLatLng(latlng);
                } else {
                    marker = L.marker(latlng, { draggable: true }).addTo(map);
                    marker.on('dragend', function () {
                        saveLocation(marker.getLatLng());
                        setGeocodeStatus('Marker adjusted manually. Please check that it is at the correct site.', false);
                    });
                }
                saveLocation(latlng);
            }

            async function findAddressOnMap() {
                var address = (addressField.value || '').trim();
                if (address.length < 5) {
                    setGeocodeStatus('Enter a more complete site address before searching.', true);
                    return;
                }
                if (geocodeInProgress) return;

                // Nominatim's public service should not be used for keystroke-by-keystroke autocomplete.
                // Search on blur or when the client clicks the button, with a minimum interval between calls.
                var wait = 1100 - (Date.now() - lastGeocodeAt);
                if (wait > 0) await new Promise(function (resolve) { window.setTimeout(resolve, wait); });

                geocodeInProgress = true;
                findAddressButton.disabled = true;
                setGeocodeStatus('Searching for this address…', false);
                try {
                    var query = address + ', South Africa';
                    var url = 'https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=za&q=' + encodeURIComponent(query);
                    lastGeocodeAt = Date.now();
                    var response = await fetch(url, { headers: { 'Accept': 'application/json', 'Accept-Language': 'en' } });
                    if (!response.ok) throw new Error('Address lookup service returned HTTP ' + response.status);
                    var results = await response.json();
                    if (!results || results.length === 0) {
                        setGeocodeStatus('We could not find that address. Try adding the suburb or city, then search again or place the marker manually.', true);
                        return;
                    }

                    var result = results[0];
                    var lat = Number(result.lat);
                    var lon = Number(result.lon);
                    if (!Number.isFinite(lat) || !Number.isFinite(lon) || lat < -90 || lat > 90 || lon < -180 || lon > 180) {
                        throw new Error('The address lookup returned invalid coordinates.');
                    }

                    var location = L.latLng(lat, lon);
                    map.setView(location, 17);
                    placeMarker(location);
                    setGeocodeStatus('Suggested location: ' + (result.display_name || address) + '. Check the marker and drag it if needed.', false);
                } catch (error) {
                    console.error('Address lookup failed:', error);
                    setGeocodeStatus('Address lookup is temporarily unavailable. You can still click the map to place the marker manually.', true);
                } finally {
                    geocodeInProgress = false;
                    findAddressButton.disabled = false;
                }
            }

            // Automatically look up the address after the client finishes entering it and leaves the field.
            addressField.addEventListener('blur', function () {
                if (addressField.value.trim().length >= 5) findAddressOnMap();
            });
            findAddressButton.addEventListener('click', findAddressOnMap);

            map.on('click', function (event) {
                placeMarker(event.latlng);
                setGeocodeStatus('Marker placed manually. Please check that it is at the correct site.', false);
            });

            // Web Forms can update layout after validation; ensure Leaflet measures the map correctly.
            window.setTimeout(function () { map.invalidateSize(); }, 100);
        })();
    </script>
</asp:Content>
