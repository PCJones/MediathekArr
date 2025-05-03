let selectedClientId = null;
let selectedClientDetails = null;
let sonarrHost = '';
let apiKey = '';

let selectedIndexerId = null;
let selectedIndexerDetails = null;

let useProwlarr = false;
let prowlarrHost = '';
let prowlarrApiKey = '';
let selectedAppProfileId = null;

function toggleProwlarrSettings(isChecked) {
    useProwlarr = isChecked;
    document.getElementById('prowlarrSettings').style.display = isChecked ? 'block' : 'none';
}

async function fetchAppProfiles() {
    updateStatus('Fetching Prowlarr app profiles...');

    const appProfiles = await fetchWithStatus(
        `/wizard/appprofiles?prowlarrHost=${encodeURIComponent(prowlarrHost)}&apiKey=${encodeURIComponent(prowlarrApiKey)}`,
        {},
        'App profiles fetched successfully.',
        'Failed to fetch app profiles'
    );

    if (appProfiles) {
        const appProfileSelect = document.getElementById('appProfile');
        appProfileSelect.innerHTML = '';

        appProfiles.forEach(profile => {
            const option = document.createElement('option');
            option.value = profile.id;
            option.textContent = profile.name;
            appProfileSelect.appendChild(option);
        });

        appProfileSelect.addEventListener('change', () => {
            selectedAppProfileId = appProfileSelect.value;
        });

        if (appProfiles.length > 0) {
            selectedAppProfileId = appProfiles[0].id;
        }
    }
}

function proceedToIndexerSelection() {
    if (useProwlarr) {
        prowlarrHost = sanitizeHost(document.getElementById('prowlarrHost').value);
        prowlarrApiKey = document.getElementById('prowlarrApiKey').value;
        if (!prowlarrHost || !prowlarrApiKey) {
            alert('Please enter the Prowlarr Host, and API Key.');
            return;
        }
        if (!selectedAppProfileId) {
            fetchAppProfiles();
            alert('Please select an App Profile.');
            return;
        }
    }

    document.getElementById('wizardStep3').style.display = 'none';
    document.getElementById('wizardStep3IndexerSelection').style.display = 'block';
    fetchIndexers();
}

async function updateOrCreateIndexer() {
    const payload = {
        configContract: "NewznabSettings",
        implementation: "Newznab",
        implementationName: "Newznab",
        enableRss: selectedIndexerDetails ? selectedIndexerDetails.enableRss : true,
        enableAutomaticSearch: selectedIndexerDetails ? selectedIndexerDetails.enableAutomaticSearch : true,
        enableInteractiveSearch: selectedIndexerDetails ? selectedIndexerDetails.enableInteractiveSearch : true,
        protocol: "usenet",
        priority: parseInt(document.getElementById('indexerPriority').textContent),
        name: document.getElementById('indexerName').textContent,
        fields: [
            { name: "baseUrl", value: document.getElementById('indexerBaseUrl').textContent },
            { name: "apiPath", value: document.getElementById('indexerApiPath').textContent },
            { name: "apiKey", value: "" },
            { name: "categories", value: [5030, 5040] },
            { name: "animeCategories", value: [] }
        ]
    };

    if (!useProwlarr) {
        payload.downloadClientId = selectedClientId;
    } else {
        payload.appProfileId = selectedAppProfileId;
        payload.enable = true;
        if (!selectedIndexerDetails) {
            payload.added = new Date().toISOString();
        }
    }

    const url = `/wizard/indexer${selectedIndexerId ? `/${selectedIndexerId}` : ''}?arrHost=${encodeURIComponent(useProwlarr ? prowlarrHost : sonarrHost)}&apiKey=${encodeURIComponent(useProwlarr ? prowlarrApiKey : apiKey)}&prowlarr=${useProwlarr}`;


    const method = selectedIndexerId ? "PUT" : "POST";

    const headers = {
        "Content-Type": "application/json",
        "X-Api-Key": useProwlarr ? prowlarrApiKey : apiKey
    };

    const response = await fetchWithStatus(
        url,
        {
            method: method,
            headers: headers,
            body: JSON.stringify(payload)
        },
        selectedIndexerId ? "Indexer updated successfully!" : "Indexer created successfully!",
        selectedIndexerId ? "Failed to update indexer" : "Failed to create indexer"
    );

    if (response) {
        if (useProwlarr) {
            const success = await setProwlarrIndexerDownloadClient(response.id);
            if (success) {
                showSuccessScreen();
            }
        }
        else {
            showSuccessScreen();
        }
    }
}

function showSuccessScreen() {
    const statusLog = document.getElementById('statusLog');
    document.querySelector('main').innerHTML = `
        <div style="text-align: center; margin-top: 50px;">
            <h1>Setup Completed!</h1>
            <p>Your MediathekArr setup is now fully configured and ready to use.</p>
            <a href="/" style="padding: 10px 20px; background-color: green; color: white; text-decoration: none; border-radius: 5px;">Go to Dashboard</a>
        </div>
    `;
    document.querySelector('main').prepend(statusLog);
    updateStatus("Setup completed successfully!");
}

async function fetchIndexers() {
    updateStatus('Fetching existing Indexers...');

    const mediathekArrIndexerPort = document.getElementById('indexerBaseUrl').textContent.split(':').pop();

    const url = useProwlarr
        ? `/wizard/indexers?arrHost=${encodeURIComponent(prowlarrHost)}&apiKey=${encodeURIComponent(prowlarrApiKey)}&portFilter=${mediathekArrIndexerPort}&prowlarr=true`
        : `/wizard/indexers?arrHost=${encodeURIComponent(sonarrHost)}&portFilter=${mediathekArrIndexerPort}&apiKey=${encodeURIComponent(apiKey)}`;
    const headers = {};

    const indexers = await fetchWithStatus(
        url,
        { headers: headers },
        'Existing indexers found. Please select one.',
        'Failed to fetch indexers'
    );

    if (indexers) {
        const existingIndexersDiv = document.getElementById('existingIndexers');
        existingIndexersDiv.innerHTML = '';

        if (indexers.length === 0) {
            existingIndexersDiv.innerHTML = '<p>No existing MediathekArr indexers found.</p>';
            updateStatus('No existing MediathekArr indexers found.');
        } else {
            const list = document.createElement('ul');
            indexers.forEach(indexer => {
                const listItem = document.createElement('li');
                listItem.innerHTML = `
                    <label>
                        <input type="radio" name="indexer" value="${indexer.id}" />
                        ${indexer.name} (Base URL: ${indexer.baseUrl})
                    </label>
                `;
                listItem.querySelector('input').addEventListener('change', () => {
                    selectedIndexerId = indexer.id;
                    selectedIndexerDetails = indexer;
                    document.getElementById('proceedWithIndexer').style.display = 'inline';
                });
                list.appendChild(listItem);
            });
            existingIndexersDiv.appendChild(list);
        }
    }
}

function proceedToNewIndexer() {
    document.getElementById('wizardStep3').style.display = 'none';
    document.getElementById('wizardStep4').style.display = 'block';
    updateStatus('Proceeding to create a new indexer...');
}
async function proceedWithSelectedIndexer() {
    if (!selectedIndexerId || !selectedIndexerDetails) {
        alert('Please select an indexer.');
        return;
    }

    updateStatus('Testing the selected indexer...');

    document.getElementById('wizardStep3').style.display = 'none';
    document.getElementById('wizardStep4').style.display = 'block';

    document.getElementById('indexerName').textContent = selectedIndexerDetails.name;
    document.getElementById('indexerBaseUrl').textContent = `http://${selectedClientDetails.host}:5007`;
    document.getElementById('indexerPriority').textContent = selectedIndexerDetails.priority;
    document.getElementById('indexerApiPath').textContent = selectedIndexerDetails.apiPath || '/api';
    document.getElementById('indexerEnableRss').textContent = selectedIndexerDetails.enableRss;
    document.getElementById('indexerEnableAutomaticSearch').textContent = selectedIndexerDetails.enableAutomaticSearch;
    document.getElementById('indexerEnableInteractiveSearch').textContent = selectedIndexerDetails.enableInteractiveSearch;

    if (!useProwlarr) {
        document.getElementById('indexerDownloadClientIdContainer').style.display = 'block';
        document.getElementById('indexerDownloadClientId').textContent = selectedIndexerDetails.downloadClientId;

        if (selectedIndexerDetails.downloadClientId !== selectedClientId) {
            document.getElementById('downloadClientChangeAlert').style.display = 'block';
            selectedIndexerDetails.downloadClientId = selectedClientId;
        }
    } else {
        document.getElementById('indexerDownloadClientIdContainer').style.display = 'none';
        document.getElementById('downloadClientChangeAlert').style.display = 'none';
    }

    await testIndexerSettings();

    if (useProwlarr) {
        const success = await setProwlarrIndexerDownloadClient(response.id);
        if (success) {
            showSuccessScreen();
        }
    }
    else {
        showSuccessScreen();
    }
}


async function retryIndexerSettings() {
    document.getElementById('indexerBaseUrl').textContent = document.getElementById('editIndexerBaseUrl').value;
    const newBaseUrl = document.getElementById('editIndexerBaseUrl').value;

    updateStatus(`Retrying with updated base URL (${newBaseUrl})...`);

    document.getElementById('editIndexerSettings').style.display = "none";
    await testIndexerSettings();
}

async function testIndexerSettings() {
    const testButton = document.getElementById('testIndexerSettingsButton');
    const confirmButton = document.getElementById('confirmIndexerSettingsButton');
    const indexerName = document.getElementById('indexerName').innerText;
    testButton.disabled = true; // Disable the button
    confirmButton.style.display = 'none'; // Hide the confirmation button initially

    updateStatus("Starting indexer settings test...");

    const testPayload = {
        configContract: "NewznabSettings",
        implementation: "Newznab",
        implementationName: "Newznab",
        enableRss: true,
        enableAutomaticSearch: true,
        enableInteractiveSearch: true,
        protocol: "usenet",
        priority: 25,
        ...(selectedIndexerId ? { id: selectedIndexerId } : {}),
        name: indexerName,
        fields: [
            { name: "baseUrl", value: document.getElementById('indexerBaseUrl').textContent },
            { name: "apiPath", value: document.getElementById('indexerApiPath').textContent },
            { name: "apiKey", value: "" },
            { name: "categories", value: [5030, 5040] },
            { name: "animeCategories", value: [] }
        ]
    };

    if (!useProwlarr) {
        testPayload.downloadClientId = selectedClientId;
    } else {
        testPayload.appProfileId = selectedAppProfileId;
        testPayload.enable = true;
    }

    const url = `/wizard/indexer/test?arrHost=${encodeURIComponent(useProwlarr ? prowlarrHost : sonarrHost)}&apiKey=${encodeURIComponent(useProwlarr ? prowlarrApiKey : apiKey)}&prowlarr=${useProwlarr}`;


    const headers = {
        "Content-Type": "application/json",
        "X-Api-Key": useProwlarr ? prowlarrApiKey : apiKey
    };

    const response = await fetchWithStatus(
        url,
        {
            method: "POST",
            headers: headers,
            body: JSON.stringify(testPayload)
        },
        "Test successful! Indexer settings are valid.",
        "Test failed"
    );

    if (response) {
        document.getElementById('testIndexerResult').textContent = "Indexer settings are valid!";
        document.getElementById('testIndexerResult').style.color = "green";

        // Show the confirmation button
        confirmButton.style.display = 'block';
    } else {
        document.getElementById('testIndexerResult').textContent = "Connection failed. Please edit the settings below.";
        document.getElementById('testIndexerResult').style.color = "red";
        document.getElementById('editIndexerSettings').style.display = "block";
    }

    testButton.disabled = false; // Re-enable the button
}

function sanitizeHost(host) {
    return host.endsWith('/') ? host.slice(0, -1) : host;
}

function updateStatus(message) {
    const statusLog = document.getElementById('statusLog');
    const logEntry = document.createElement('p');
    logEntry.textContent = message;
    statusLog.appendChild(logEntry);
    statusLog.scrollTop = statusLog.scrollHeight; // Auto-scroll to the latest message
}

async function fetchWithStatus(url, options, successMessage, errorMessage) {
    try {
        const response = await fetch(url, options);
        if (!response.ok) {
            const errorResponse = await response.json();
            updateStatus(`${errorMessage}: ${errorResponse.error.message || 'Unknown error'}`);
            return null;
        }
        updateStatus(successMessage);
        return await response.json();
    } catch (error) {
        console.error(`${errorMessage}:`, error.message);
        updateStatus(`${errorMessage}.`);
        return null;
    }
}

async function verifySonarrDetails() {
    sonarrHost = sanitizeHost(document.getElementById('sonarrHost').value);
    apiKey = document.getElementById('apiKey').value;

    if (!sonarrHost || !apiKey) {
        alert('Please enter both Sonarr Host and API Key.');
        return;
    }

    updateStatus('Verifying Sonarr details...');

    const response = await fetchWithStatus(
        `/wizard/downloadclients?sonarrHost=${encodeURIComponent(sonarrHost)}&apiKey=${encodeURIComponent(apiKey)}`,
        {},
        'Sonarr details verified successfully.',
        'Failed to verify Sonarr details'
    );

    if (response) {
        document.getElementById('wizardStep0').style.display = 'none';
        document.getElementById('wizardStep1').style.display = 'block';
        fetchSabnzbdClients();
    }
}

async function fetchSabnzbdClients() {
    updateStatus('Fetching existing Sabnzbd clients...');

    const clients = await fetchWithStatus(
        `/wizard/downloadclients?sonarrHost=${encodeURIComponent(sonarrHost)}&apiKey=${encodeURIComponent(apiKey)}`,
        {},
        'Existing clients found. Please select one.',
        'Failed to fetch Sabnzbd clients'
    );

    if (clients) {
        const existingClientsDiv = document.getElementById('existingClients');
        existingClientsDiv.innerHTML = '';

        if (clients.length === 0) {
            existingClientsDiv.innerHTML = '<p>No existing MediathekArr download clients found.</p>';
            updateStatus('No existing MediathekArr download clients found.');
        } else {
            const list = document.createElement('ul');
            clients.forEach(client => {
                const listItem = document.createElement('li');
                listItem.innerHTML = `
                    <label>
                        <input type="radio" name="sabnzbdClient" value="${client.id}" />
                        ${client.name} (Host: ${client.host}, Port: ${client.port})
                    </label>
                `;
                listItem.querySelector('input').addEventListener('change', () => {
                    selectedClientId = client.id;
                    selectedClientDetails = client;
                    document.getElementById('proceedWithClient').style.display = 'inline';
                });
                list.appendChild(listItem);
            });
            existingClientsDiv.appendChild(list);
        }
    }
}

async function proceedWithSelectedClient() {
    if (!selectedClientId || !selectedClientDetails) {
        alert('Please select a client.');
        return;
    }

    updateStatus('Testing the selected client...');

    if (selectedClientDetails.category !== 'tv') {
        document.getElementById('categoryChangeAlert').style.display = 'block';
        document.getElementById('categoryChangeAlert').innerText = `The selected client category is "${selectedClientDetails.category}" and not "tv". It will be changed to "tv" for migration to 1.0.`;
        updateStatus(document.getElementById('categoryChangeAlert').innerText)
        selectedClientDetails.category = 'tv';
    }

    if (selectedClientDetails.priority !== 50 && selectedClientDetails.priority !== "50") {
        updateStatus(`The selected client priority is ${selectedClientDetails.priority} and not 50. It will be changed to 50.`);
        document.getElementById('priorityChangeAlert').style.display = 'block';
        selectedClientDetails.priority = 50;
    }

    document.getElementById('wizardStep1').style.display = 'none';
    document.getElementById('wizardStep2').style.display = 'block';

    document.getElementById('clientName').textContent = selectedClientDetails.name;
    document.getElementById('clientHost').textContent = selectedClientDetails.host;
    document.getElementById('clientPort').textContent = selectedClientDetails.port;
    document.getElementById('clientUrlBase').textContent = selectedClientDetails.urlBase || 'download';
    document.getElementById('clientApiKey').textContent = selectedClientDetails.apiKey;
    document.getElementById('clientCategory').textContent = selectedClientDetails.category;
    document.getElementById('clientPriority').textContent = selectedClientDetails.priority;

    useProwlarr = false;
    await testClientSettings(false);
}

function proceedToNewClient() {
    document.getElementById('wizardStep1').style.display = 'none';
    document.getElementById('wizardStep2').style.display = 'block';
    updateStatus('Proceeding to create a new client...');
}

async function testClientSettings(tryAlternate = true) {
    const testButton = document.getElementById('testClientSettingsButton');
    testButton.disabled = true; // Disable the button

    updateStatus("Starting client settings test...");

    const testPayload = {
        enable: true,
        protocol: "usenet",
        priority: 50,
        ...(selectedClientId ? { id: selectedClientId } : {}),
        removeCompletedDownloads: true,
        removeFailedDownloads: true,
        name: "MediathekArr Downloader",
        fields: [
            { name: "host", value: document.getElementById('clientHost').textContent },
            { name: "port", value: parseInt(document.getElementById('clientPort').textContent) },
            { name: "useSsl", value: false },
            { name: "urlBase", value: "download" },
            { name: "apiKey", value: "x" },
            { name: "username" },
            { name: "password" },
            { name: "tvCategory", value: "tv" },
            { name: "recentTvPriority", value: -100 },
            { name: "olderTvPriority", value: -100 }
        ],
        implementationName: "SABnzbd",
        implementation: "Sabnzbd",
        configContract: "SabnzbdSettings",
        infoLink: "https://wiki.servarr.com/sonarr/supported#sabnzbd",
        tags: []
    };

    const response = await fetchWithStatus(
        `/wizard/downloadclient/test?sonarrHost=${encodeURIComponent(sonarrHost)}&apiKey=${encodeURIComponent(apiKey)}`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(testPayload)
        },
        "Test successful! Client settings are valid.",
        "Test failed"
    );

    if (response) {
        document.getElementById('testResult').textContent = "Client settings are valid!";
        document.getElementById('testResult').style.color = "green";

        // Show the confirmation button
        document.getElementById('confirmClientSettingsButton').style.display = 'block';
    } else if (tryAlternate) {
        const fallbackResponse = await tryAlternateHost(testPayload);
        if (!fallbackResponse) {
            document.getElementById('testResult').textContent = "Test/Connection failed. Please edit the settings below.";
            document.getElementById('testResult').style.color = "red";
            document.getElementById('editClientSettings').style.display = "block";
        }
    } else {
        document.getElementById('testResult').textContent = "Connection failed. Please edit the settings below.";
        document.getElementById('testResult').style.color = "red";
        document.getElementById('editClientSettings').style.display = "block";
    }

    testButton.disabled = false; // Re-enable the button
}

async function setProwlarrIndexerDownloadClient(indexerId) {
    updateStatus('Waiting 15 seconds for Prowlarr to sync with Sonarr...');
    await new Promise(resolve => setTimeout(resolve, 15000));
    const prowlarrPort = new URL(prowlarrHost).port || 9696;

    updateStatus('Fetching Sonarr indexers...');
    const sonarrIndexers = await fetchWithStatus(
        `/wizard/indexers?apiKey=${encodeURIComponent(apiKey)}&arrHost=${encodeURIComponent(sonarrHost)}&portFilter=${prowlarrPort}`,
        {},
        'Sonarr indexers fetched successfully.',
        'Failed to fetch Sonarr indexers'
    );

    // TODO if Failed to fetch Sonarr indexers we still continue to setup completed

    if (sonarrIndexers) {
        const matchingIndexers = sonarrIndexers.filter(indexer =>
            indexer.baseUrl && indexer.baseUrl.endsWith(`/${indexerId}/`)
        );

        if (matchingIndexers.length !== 1) {
            updateStatus('Unable to find the Prowlarr indexer inside Sonarr. Please set the download client to the MediathekArr Downloader by hand.');
            alert('Unable to find the Prowlarr indexer inside Sonarr. Please set the download client to the MediathekArr Downloader by hand.');
            return false;
        }

        const foundIndexer = matchingIndexers[0];

        // Create update payload
        const updatePayload = {
            id: foundIndexer.id,
            configContract: "NewznabSettings",
            implementation: "Newznab",
            implementationName: "Newznab",
            enableRss: foundIndexer ? foundIndexer.enableRss : true,
            enableAutomaticSearch: foundIndexer ? foundIndexer.enableAutomaticSearch : true,
            enableInteractiveSearch: foundIndexer ? foundIndexer.enableInteractiveSearch : true,
            protocol: "usenet",
            priority: parseInt(foundIndexer.priority),
            fields: [
                { name: "baseUrl", value: foundIndexer.baseUrl },
                { name: "apiPath", value: foundIndexer.apiPath   },
                { name: "apiKey", value: "********" },
                { name: "categories", value: [5030, 5040] },
                { name: "animeCategories", value: [] }
            ],
            name: foundIndexer.name,
            downloadClientId: selectedClientId
        };

        console.log(updatePayload);

        const updateResponse = await fetchWithStatus(
            `/wizard/indexer/${foundIndexer.id}?apiKey=${apiKey}&arrHost=${sonarrHost}`,
            {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(updatePayload)
            },
            'Indexer updated with download client ID successfully.',
            'Failed to update indexer with download client ID'
        );

        console.log('xxxxxxxxx');
        console.log(updateResponse);

        if (!updateResponse) {
            alert('Failed to update the indexer with the download client ID.');
            return false;
        }
        else {
            return true;
        }
    }

    alert('Failed to update the indexer with the download client ID.');
    return false;
}


async function updateOrCreateDownloadClient() {
    const payload = {
        enable: true,
        protocol: "usenet",
        priority: 50,
        removeCompletedDownloads: true,
        removeFailedDownloads: true,
        name: "MediathekArr Downloader",
        fields: [
            { name: "host", value: document.getElementById('clientHost').textContent },
            { name: "port", value: parseInt(document.getElementById('clientPort').textContent) },
            { name: "useSsl", value: false },
            { name: "urlBase", value: "download" },
            { name: "apiKey", value: "x" },
            { name: "username" },
            { name: "password" },
            { name: "tvCategory", value: "tv" },
            { name: "recentTvPriority", value: -100 },
            { name: "olderTvPriority", value: -100 }
        ],
        implementationName: "SABnzbd",
        implementation: "Sabnzbd",
        configContract: "SabnzbdSettings",
        infoLink: "https://wiki.servarr.com/sonarr/supported#sabnzbd",
        tags: []
    };

    const url = `/wizard/downloadclient${selectedClientId ? `/${selectedClientId}` : ''}?sonarrHost=${encodeURIComponent(sonarrHost)}&apiKey=${encodeURIComponent(apiKey)}`;

    const method = selectedClientId ? "PUT" : "POST";

    const response = await fetchWithStatus(
        url,
        {
            method: method,
            headers: {
                "Content-Type": "application/json",
                "X-Api-Key": apiKey
            },
            body: JSON.stringify(payload)
        },
        selectedClientId ? "Client updated successfully!" : "Client created successfully!",
        selectedClientId ? "Failed to update client" : "Failed to create client"
    );

    if (response) {
        // Set selectedClientId for newly created client
        if (!selectedClientId && response.id) {
            selectedClientId = response.id;
        }

        // Proceed to the next step (Indexer)
        document.getElementById('wizardStep2').style.display = 'none';
        document.getElementById('wizardStep3').style.display = 'block';
    }
}


async function tryAlternateHost(testPayload) {
    const alternateHost = "mediathekarr";
    updateStatus(`Testing with alternate host: ${alternateHost}...`);
    testPayload.fields.find(field => field.name === "host").value = alternateHost;

    const response = await fetchWithStatus(
        `/wizard/downloadclient/test?sonarrHost=${encodeURIComponent(sonarrHost)}&apiKey=${encodeURIComponent(apiKey)}`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Api-Key": apiKey
            },
            body: JSON.stringify(testPayload)
        },
        "Test successful with alternate host!",
        "Test failed with alternate host"
    );

    if (response) {
        document.getElementById('clientHost').textContent = alternateHost;
        document.getElementById('testResult').textContent = "Client settings are valid with alternate host!";
        document.getElementById('testResult').style.color = "green";
        return true;
    }

    return false;
}

async function retryClientSettings() {
    const newHost = document.getElementById('editHost').value;
    const newPort = document.getElementById('editPort').value;

    updateStatus(`Retrying with updated host (${newHost}) and port (${newPort})...`);

    document.getElementById('clientHost').textContent = newHost;
    document.getElementById('clientPort').textContent = newPort;

    document.getElementById('editClientSettings').style.display = "none";
    await testClientSettings(false);
}
