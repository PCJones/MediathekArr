<?php

function getToken($db) {
    // Check if token is stored and still valid
    $stmt = $db->query("SELECT token, expiration_date FROM api_token WHERE id = 1");
    $result = $stmt->fetch(PDO::FETCH_ASSOC);

    if ($result && new DateTime() < new DateTime($result['expiration_date'])) {
        return $result['token'];
    } else {
        // If no valid token, refresh the token
        $apiKey = getApiKey($db);
        return refreshToken($db, $apiKey);
    }
}

function refreshToken($db, $apiKey) {
    $curl = curl_init('https://api4.thetvdb.com/v4/login');
    curl_setopt($curl, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($curl, CURLOPT_POST, true);
    curl_setopt($curl, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
    curl_setopt($curl, CURLOPT_POSTFIELDS, json_encode(['apikey' => $apiKey]));
    
    $response = curl_exec($curl);
    $data = json_decode($response, true);
    
    if ($data && $data['status'] == 'success') {
        $token = $data['data']['token'];
        $expirationDate = date('Y-m-d H:i:s', time() + 86400); // Assuming token expires after 24 hours

        // Update or insert the new token and expiration into the api_token table
        $db->exec("DELETE FROM api_token WHERE id = 1"); // Clear existing token
        $stmt = $db->prepare("INSERT INTO api_token (id, token, expiration_date) VALUES (1, :token, :expiration_date)");
        $stmt->execute(['token' => $token, 'expiration_date' => $expirationDate]);

        return $token;
    } else {
        // Handle error or retry logic
        return null;
    }
}
?>
