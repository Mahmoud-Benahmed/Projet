#!/bin/bash

set -e

ROOT_DIR=$(pwd)

echo "🔍 Searching for ERP.* services..."

for dir in ERP.*/ ; do
  service=${dir%/}

  # Skip if not a directory
  [ -d "$service" ] || continue

  # Check if docker compose file exists
  if [ -f "$service/docker-compose.yaml" ] || [ -f "$service/docker-compose.yml" ]; then

    echo "--------------------------------------"
    echo " Stopping $service (if running)..."
    echo "--------------------------------------"

    cd "$ROOT_DIR/$service"

    # Stop existing containers (safe even if none are running)
    docker compose down || true

    echo "🚀 Rebuilding and starting $service..."
    docker compose up --build -d

    cd "$ROOT_DIR"
  else
    echo "!! Skipping $service (no docker compose file)"
  fi
done

echo "--------------------------------------"
echo "✅ All ERP services restarted successfully!"
echo "--------------------------------------"
