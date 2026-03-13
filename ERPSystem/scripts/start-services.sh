#!/bin/bash

set -e
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"

# Optional build flag
BUILD_FLAG=""
if [ "$1" = "build" ]; then
  BUILD_FLAG="--build"
fi

echo "Removing existing containers..."
docker rm -f erp-kafka erp-auth erp-gateway erp-auth-mongo erp-article erp-article-sqlserver erp-client erp-client-sqlserver 2>/dev/null || true

echo "Starting shared infrastructure..."
bash "$ROOT_DIR/scripts/start-kafka.sh"

echo "Waiting for Kafka to be healthy..."
until [ "$(docker inspect --format='{{.State.Health.Status}}' erp-kafka 2>/dev/null)" = "healthy" ]; do
  echo "still waiting..."
  sleep 5
done

echo "Kafka is healthy!"

# Loop through all ERP service folders
for service in ERP.AuthService ERP.Gateway ERP.ArticleService ERP.ClientService; do
  [ -d "$ROOT_DIR/$service" ] || continue
  if [ -f "$ROOT_DIR/$service/docker-compose.yaml" ] || [ -f "$ROOT_DIR/$service/docker-compose.yml" ]; then
    echo "--------------------------------------"
    echo "Starting $service..."
    echo "--------------------------------------"
    cd "$ROOT_DIR/$service"
    # Start containers with or without rebuild
    docker compose down || true
    docker compose up $BUILD_FLAG -d

    # Wait for SQL Server if exists
    SQLSERVER_CONTAINER=$(docker compose ps -q sqlserver 2>/dev/null || true)
    if [ -n "$SQLSERVER_CONTAINER" ]; then
      echo "Waiting for sqlserver in $service to be healthy..."
      until [ "$(docker inspect --format='{{.State.Health.Status}}' "$SQLSERVER_CONTAINER" 2>/dev/null)" = "healthy" ]; do
        echo "still waiting for sqlserver..."
        sleep 5
      done
      echo "sqlserver in $service is healthy!"
    fi
    cd "$ROOT_DIR"
  else
    echo "!! Skipping $service (no docker-compose file)"
  fi
done

echo "--------------------------------------"
echo "All ERP services started!"
echo "--------------------------------------"
