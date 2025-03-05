#!/bin/bash

# Function to adjust ownership dynamically for mounted directories
adjust_mounted_volumes() {
    echo "Adjusting ownership for mounted volumes..."
    while read -r device mountpoint fstype options dump pass; do
        # Exclude system directories and the root `/` mount
        if [[ "$mountpoint" == "/"* ]] && \
           [[ "$mountpoint" != "/" ]] && \
           [[ "$mountpoint" != "/proc"* ]] && \
           [[ "$mountpoint" != "/sys"* ]] && \
           [[ "$mountpoint" != "/dev"* ]] && \
           [[ "$mountpoint" != "/run"* ]] && \
           [[ "$mountpoint" != "/etc"* ]] && \
           [[ "$mountpoint" != "/lib"* ]] && \
           [[ "$mountpoint" != "/usr"* ]]; then
           
            echo "Checking mounted directory: $mountpoint"
            if [ -d "$mountpoint" ]; then
                echo "Setting ownership for $mountpoint to $user_name:$group_name"
                chown -R "$user_name":"$group_name" "$mountpoint"
            else
                echo "Skipped non-directory mountpoint: $mountpoint"
            fi
        fi
    done < /proc/self/mounts
}

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

    # Determine group name based on PGID
    existing_group=$(getent group "$PGID" | cut -d: -f1)
    if [ -z "$existing_group" ]; then
        # Create the group if it doesn't exist
        group_name="appgroup"
        if ! groupadd -g "$PGID" "$group_name"; then
            echo "Failed to create group $group_name with GID $PGID"
            exit 1
        fi
        echo "Created group $group_name with GID $PGID"
    else
        # Use existing group name
        group_name="$existing_group"
        echo "Using existing group $group_name with GID $PGID"
    fi

    # Determine user name based on PUID
    existing_user=$(getent passwd "$PUID" | cut -d: -f1)
    if [ -z "$existing_user" ]; then
        # Create the user if it doesn't exist
        user_name="appuser"
        if ! useradd -u "$PUID" -g "$group_name" -m -s /bin/bash "$user_name"; then
            echo "Failed to create user $user_name with UID $PUID"
            exit 1
        fi
        echo "Created user $user_name with UID $PUID"
    else
        # Use existing user name
        user_name="$existing_user"
        echo "Using existing user $user_name with UID $PUID"
    fi

    # Adjust ownership of relevant directories
    chown -R "$user_name":"$group_name" /app /app/config /data/mediathek

    # Adjust ownership dynamically for all mounted volumes
    adjust_mounted_volumes

    # Switch to the determined user and re-execute the script
    exec gosu "$user_name" /bin/bash "$0" --user-mode
fi