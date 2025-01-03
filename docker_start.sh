#!/bin/bash

# Check if running in user mode
if [ "$1" == "--user-mode" ]; then
    shift
    
    # Start MediathekArrServer
    cd /app/MediathekArrServer
    dotnet MediathekArrServer.dll &
    SERVER_PID=$!

    # Start MediathekArrDownloader
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
        sleep 1
    done
fi

# Initial setup for PUID and PGID
if [ -z "$PUID" ] || [ -z "$PGID" ]; then
    echo "Mediathekarr: Running services as UID $PUID, GID $PGID"
    exec /bin/bash "$0" --user-mode
else
    echo "Mediathekarr: Starting with UID: $PUID, GID: $PGID"

    # Create group if it doesn't exist
    if ! getent group appgroup > /dev/null 2>&1; then
        groupadd -g "$PGID" appgroup
    fi

    # Create user if it doesn't exist
    if ! id -u appuser > /dev/null 2>&1; then
        useradd -u "$PUID" -g appgroup -m -s /bin/bash appuser
    fi

    # Adjust ownership of relevant directories
    chown -R appuser:appgroup /app /app/config /data/mediathek

    # Switch to the created user and re-execute the script
    exec gosu appuser /bin/bash "$0" --user-mode
fi
