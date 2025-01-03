#!/bin/bash

# Start MediathekArrServer from its subfolder
cd /app/MediathekArrServer
dotnet MediathekArrServer.dll &
SERVER_PID=$!

# Start MediathekArrDownloader from its subfolder
cd /app/MediathekArrDownloader
dotnet MediathekArrDownloader.dll &
DOWNLOADER_PID=$!
# Monitor processes
while true; do
    if ! kill -0 $SERVER_PID 2>/dev/null; then
        echo "MediathekArrServer crashed. Exiting."
        exit 1
    fi
    if ! kill -0 $DOWNLOADER_PID 2>/dev/null; then
        echo "MediathekArrDownloader crashed. Exiting."
        exit 1
    fi
    sleep 60
done
