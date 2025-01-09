<?php

define('DB_FILE', './db/tvdb_cache.sqlite');

function initializeDatabase() {
    $isFirstRun = !file_exists(DB_FILE);
    
    $db = new PDO('sqlite:' . DB_FILE);
    $db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

    if ($isFirstRun) {
        createTables($db);
        displayApiKeyForm($db);
    }

    return $db;
}

function createTables($db) {
    // Create table to store the API key
    $createApiKeyTableQuery = "CREATE TABLE IF NOT EXISTS api_key (
        id INTEGER PRIMARY KEY,
        key TEXT NOT NULL
    )";

    // Create table to store the API token and its expiration
    $createTokenTableQuery = "CREATE TABLE IF NOT EXISTS api_token (
        id INTEGER PRIMARY KEY,
        token TEXT NOT NULL,
        expiration_date TEXT NOT NULL
    )";

    $createSeriesCacheTableQuery = "CREATE TABLE IF NOT EXISTS series_cache (
        series_id INTEGER PRIMARY KEY,
        name TEXT,
        german_name TEXT,
        aliases TEXT,
        last_updated TEXT,
        next_aired TEXT,
        last_aired TEXT,
        cache_expiry TEXT
    )";

    $createEpisodesTableQuery = "CREATE TABLE IF NOT EXISTS episodes (
        id INTEGER PRIMARY KEY,
        series_id INTEGER,
        name TEXT,
        aired TEXT,
        runtime INTEGER,
        season_number INTEGER,
        episode_number INTEGER,
        absolute_number INTEGER,
        FOREIGN KEY(series_id) REFERENCES series_cache(series_id)
    )";

    $db->exec($createApiKeyTableQuery);
    $db->exec($createTokenTableQuery);
    $db->exec($createSeriesCacheTableQuery);
    $db->exec($createEpisodesTableQuery);
}

function displayApiKeyForm($db) {
    if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['api_key'])) {
        $apiKey = trim($_POST['api_key']);
        
        if ($apiKey) {
            // Store the API key in the database
            $stmt = $db->prepare("INSERT INTO api_key (id, key) VALUES (1, :key)");
            $stmt->execute(['key' => $apiKey]);
            echo "API key saved successfully. You can now use the application.";
            exit;
        } else {
            echo "Please enter a valid API key.";
        }
    }

    echo '<!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <title>Set TVDB API Key</title>
    </head>
    <body>
        <h1>Enter TVDB API Key</h1>
        <form method="post">
            <label for="api_key">API Key:</label>
            <input type="text" id="api_key" name="api_key" required>
            <button type="submit">Save API Key</button>
        </form>
    </body>
    </html>';
    exit;
}

function getApiKey($db) {
    // Retrieve the API key from the database
    $stmt = $db->query("SELECT key FROM api_key WHERE id = 1");
    $result = $stmt->fetch(PDO::FETCH_ASSOC);

    if ($result) {
        return $result['key'];
    } else {
        // Show API key form if not set
        displayApiKeyForm($db);
    }
}
?>
