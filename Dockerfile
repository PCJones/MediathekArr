FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build-env
ARG TARGETARCH
WORKDIR /src

COPY MediathekArrLib/MediathekArrLib.csproj MediathekArrLib/
COPY MediathekArrServer/MediathekArrServer.csproj MediathekArrServer/
COPY MediathekArr/MediathekArrDownloader.csproj MediathekArr/
RUN dotnet restore MediathekArrServer/MediathekArrServer.csproj -a $TARGETARCH && \
    dotnet restore MediathekArr/MediathekArrDownloader.csproj -a $TARGETARCH

COPY . ./
RUN dotnet publish MediathekArrServer/MediathekArrServer.csproj -c Release -a $TARGETARCH --no-restore -o /app/out/MediathekArrServer && \
    dotnet publish MediathekArr/MediathekArrDownloader.csproj -c Release -a $TARGETARCH --no-restore -o /app/out/MediathekArrDownloader

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble

# Install runtime dependencies (ffmpeg, gosu, MKVToolNix)
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates wget gosu ffmpeg && \
    wget -qO /etc/apt/keyrings/gpg-pub-moritzbunkus.gpg https://mkvtoolnix.download/gpg-pub-moritzbunkus.gpg && \
    echo "deb [signed-by=/etc/apt/keyrings/gpg-pub-moritzbunkus.gpg] https://mkvtoolnix.download/ubuntu/ noble main" > /etc/apt/sources.list.d/mkvtoolnix.download.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends mkvtoolnix && \
    apt-get purge -y --auto-remove wget && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the built apps from the build environment
COPY --from=build-env /app/out/MediathekArrServer /app/MediathekArrServer
COPY --from=build-env /app/out/MediathekArrDownloader /app/MediathekArrDownloader

# Copy the startup script
COPY --chmod=755 docker_start.sh /app/docker_start.sh

# Create required directories
RUN mkdir -p /data/mediathek/incomplete /data/mediathek/complete

ENV ASPNETCORE_ENVIRONMENT=Production
ENV CONFIG_PATH=/app/config

# Use the shell script to start both processes
ENTRYPOINT ["/app/docker_start.sh"]
