FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy and restore dependencies
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# Set working directory
WORKDIR /app

# Set up environment variables for user IDs
ARG PUID=1000
ARG PGID=1000
ENV PUID=${PUID} \
    PGID=${PGID}

# Create a user and group with specified IDs
RUN addgroup --gid ${PGID} appgroup && \
    adduser --disabled-password --gecos "" --uid ${PUID} --gid ${PGID} appuser

# Copy the built app from the build environment
COPY --from=build-env /app/out .

# Change ownership to non-root user
RUN chown -R appuser:appgroup /app
USER appuser
ENTRYPOINT ["dotnet", "MediathekArr.dll"]