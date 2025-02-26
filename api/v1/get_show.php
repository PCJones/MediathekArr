<?php
require 'db.php';
require 'token_manager.php';

$db = initializeDatabase();
$apiKey = getApiKey($db);

header("Access-Control-Allow-Origin: https://jones-sanity.vercel.app");
header("Access-Control-Allow-Methods: GET, OPTIONS, PATCH, DELETE, POST, PUT");
header("Access-Control-Allow-Headers: Content-Type, Authorization");
header("Access-Control-Allow-Credentials: true");

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit();
}
header('Content-Type: application/json');

// Helper function to determine if cache is expired
function isCacheExpired($row) {
    try {
        $now = new DateTime();
        $cacheExpiry = new DateTime($row['cache_expiry']);
        return $now > $cacheExpiry;
    } catch (Exception $e) {
        return true; // If date parsing fails, consider cache expired
    }
}

// Main function to fetch series information
function getSeriesData($db, $tvdbId, $apiKey, $debug = false) {
    try {
        // Fetch from cache
        $stmt = $db->prepare("SELECT * FROM series_cache WHERE series_id = :tvdb_id");
        $stmt->bindValue(':tvdb_id', (int)$tvdbId, PDO::PARAM_INT);
        $stmt->execute();
        $seriesData = $stmt->fetch(PDO::FETCH_ASSOC);

        $cached = false;
        $cacheExpiry = null;

        if ($seriesData) {
            $cached = !isCacheExpired($seriesData);
            $cacheExpiry = $seriesData['cache_expiry'];
        }

        // Return cached data if available and not expired
        if ($cached) {
            $episodesStmt = $db->prepare("SELECT * FROM episodes WHERE series_id = :tvdb_id");
            $episodesStmt->bindValue(':tvdb_id', (int)$tvdbId, PDO::PARAM_INT);
            $episodesStmt->execute();
            $episodes = $episodesStmt->fetchAll(PDO::FETCH_ASSOC);

            $response = [
                "status" => "success",
                "data" => [
                    "id" => $tvdbId,
                    "name" => $seriesData['name'],
                    "german_name" => $seriesData['german_name'],
                    "aliases" => json_decode($seriesData['aliases']),
                    "episodes" => array_map(function ($episode) {
                        return [
                            "name" => $episode['name'],
                            "aired" => $episode['aired'],
                            "runtime" => $episode['runtime'],
                            "seasonNumber" => $episode['season_number'],
                            "episodeNumber" => $episode['episode_number'],
                            "absoluteNumber" => $episode['absolute_number'],
                        ];
                    }, $episodes)
                ]
            ];

            if ($debug) {
                $response['debug'] = [
                    "cached" => true,
                    "cache_expiry" => $cacheExpiry
                ];
            }

            return $response;
        } else {
            // Fetch new data if cache is expired or unavailable
            return fetchAndCacheSeriesData($db, $tvdbId, $apiKey, $debug);
        }
    } catch (Exception $e) {
        return ["status" => "error", "message" => "Error retrieving series data: " . $e->getMessage()];
    }
}

function fetchTranslationTitles($tvdbId, $defaultEnglishName, $defaultGermanName) {
    $apiUrl = "https://umlautadaptarr.pcjones.de/api/v1/tvshow_german.php?tvdbid=$tvdbId";
    $apiResponse = file_get_contents($apiUrl);
    $data = json_decode($apiResponse, true);
    return [
        'englishTitle' => $data['originalTitle'] ?? $defaultEnglishName,
        'germanTitle'  => $data['germanTitle'] ?? $defaultGermanName,
    ];
}

// Function to fetch and cache data from TVDB
function fetchAndCacheSeriesData($db, $tvdbId, $apiKey, $debug = false) {
    $token = getToken($db, $apiKey);
    if (!$token) {
        return ["status" => "error", "message" => "Failed to retrieve valid token from TVDB"];
    }

    $curl = curl_init("https://api4.thetvdb.com/v4/series/$tvdbId/extended?meta=episodes&short=true");
    curl_setopt($curl, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($curl, CURLOPT_HTTPHEADER, [
        "Authorization: Bearer $token",
        "Accept: application/json"
    ]);
    $response = curl_exec($curl);

    // Check for Curl errors
    if (curl_errno($curl)) {
        $error_msg = curl_error($curl);
        curl_close($curl);
        return ["status" => "error", "message" => "Curl error: " . $error_msg];
    }
    curl_close($curl);

    // Decode response and check for errors
    $data = json_decode($response, true);
    if (!$data || $data['status'] !== 'success') {
        return ["status" => "error", "message" => "Failed to fetch data from TVDB"];
    }

    try {
        $series = $data['data'];
		$seriesName = $series['name'];
		$translations = fetchTranslationTitles($tvdbId, $seriesName, $seriesName);
		$englishName = $translations['englishTitle'];
		$germanName  = $translations['germanTitle'];

        $rawAliases = $series['aliases'] ?? [];
        // Normalize aliases into an array
        $germanAliases = [];
        if (is_array($rawAliases)) {
            foreach ($rawAliases as $alias) {
                if (isset($alias['language']) && $alias['language'] === 'deu') {
                    $germanAliases[] = $alias;
                }
            }
        } elseif (is_object($rawAliases)) {
            foreach ((array)$rawAliases as $alias) {
                if (isset($alias['language']) && $alias['language'] === 'deu') {
                    $germanAliases[] = $alias;
                }
            }
        } // If neither, default to an empty array
        $germanAliases = $germanAliases ?: [];
		
        $nextAired = !empty($series['nextAired']) ? new DateTime($series['nextAired']) : new DateTime('1970-01-01');
        $lastAired = !empty($series['lastAired']) ? new DateTime($series['lastAired']) : new DateTime('1970-01-01');
        $lastUpdated = new DateTime($series['lastUpdated']);
        
        $cacheExpiry = new DateTime();
        if ($lastUpdated->diff($cacheExpiry)->days < 7 ||
            ($nextAired != new DateTime('1970-01-01') && $nextAired->diff($cacheExpiry)->days < 6) ||
            ($lastAired != new DateTime('1970-01-01') && $lastAired->diff($cacheExpiry)->days < 3)) {
            $cacheExpiry->modify('+1 days');
        } else {
            $cacheExpiry->modify('+2 days');
        }

        // Cache series data
        $db->beginTransaction();
        $db->exec("DELETE FROM series_cache WHERE series_id = $tvdbId");
        $stmt = $db->prepare("INSERT INTO series_cache (series_id, name, german_name, aliases, last_updated, next_aired, last_aired, cache_expiry) VALUES (:tvdb_id, :name, :german_name, :aliases, :last_updated, :next_aired, :last_aired, :cache_expiry)");
        $stmt->execute([
            'tvdb_id' => $tvdbId,
            'name' => $englishName,
            'german_name' => $germanName,
            'aliases' => json_encode($germanAliases),
            'last_updated' => $series['lastUpdated'],
            'next_aired' => $nextAired->format('Y-m-d H:i:s'),
            'last_aired' => $lastAired->format('Y-m-d H:i:s'),
            'cache_expiry' => $cacheExpiry->format('Y-m-d H:i:s')
        ]);

        $db->exec("DELETE FROM episodes WHERE series_id = $tvdbId");
        $episodesStmt = $db->prepare("INSERT INTO episodes (id, series_id, name, aired, runtime, season_number, episode_number, absolute_number) VALUES (:id, :tvdb_id, :name, :aired, :runtime, :season_number, :episode_number, :absolute_number)");
        foreach ($series['episodes'] as $episode) {
            $episodesStmt->execute([
                'id' => $episode['id'],
                'tvdb_id' => $tvdbId,
                'name' => $episode['name'],
                'aired' => $episode['aired'],
                'runtime' => $episode['runtime'],
                'season_number' => $episode['seasonNumber'],
                'episode_number' => $episode['number'],
                'absolute_number' => $episode['absoluteNumber']
            ]);
        }
        $db->commit();
	
        $response = [
            "status" => "success",
            "data" => [
                "id" => $tvdbId,
                "name" => $englishName,
                "german_name" => $germanName,
                "aliases" => $germanAliases,
                "episodes" => array_map(function ($episode) {
                    return [
                        "name" => $episode['name'],
                        "aired" => $episode['aired'],
                        "runtime" => $episode['runtime'],
                        "seasonNumber" => $episode['seasonNumber'],
                        "episodeNumber" => $episode['number'],
                        "absoluteNumber" => $episode['absoluteNumber'],
                    ];
                }, $series['episodes'])
            ]
        ];

        if ($debug) {
            $response['debug'] = [
                "cached" => false,
                "cache_expiry" => $cacheExpiry->format('Y-m-d H:i:s')
            ];
        }

        return $response;
    } catch (Exception $e) {
        $db->rollBack();
        return ["status" => "error", "message" => "Database error: " . $e->getMessage()];
    }
}

// Process request
$tvdbId = filter_input(INPUT_GET, 'tvdbid', FILTER_VALIDATE_INT);
$debug = filter_input(INPUT_GET, 'debug', FILTER_VALIDATE_BOOLEAN);

if ($tvdbId) {
    echo json_encode(getSeriesData($db, $tvdbId, $apiKey, $debug));
} else {
    echo json_encode(["status" => "error", "message" => "TVDB ID is required and must be an integer"]);
}
?>
