#!/bin/bash


set -e

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"


# Optional build flag

BUILD_FLAG=""

if [ "$1" = "build" ] || [ "$1" = "--build" ]; then

  BUILD_FLAG="--build"

fi


# Create the external network if it doesn't exist

NETWORK_NAME="erp-network"

if ! docker network ls --format '{{.Name}}' | grep -q "^${NETWORK_NAME}$"; then

  echo "Creating external network: $NETWORK_NAME"

  docker network create "$NETWORK_NAME"

else

  echo "Network $NETWORK_NAME already exists"

fi


# Ordered list of services

SERVICES=(
  "ERP.Gateway"
  "ERP.AuthService"
  "ERP.ArticleService"
  "ERP.ClientService"
  "ERP.FournisseurService"
  "ERP.StockService"
  "ERP.InvoiceService"
)


for service in "${SERVICES[@]}"; do

  [ -d "$ROOT_DIR/$service" ] || { echo "!! $service directory not found — skipping"; continue; }

  if [ -f "$ROOT_DIR/$service/docker-compose.yaml" ] || [ -f "$ROOT_DIR/$service/docker-compose.yml" ]; then

    echo "--------------------------------------"

    echo "Starting $service..."

    echo "--------------------------------------"

    cd "$ROOT_DIR/$service"

    docker compose down || true

    docker compose up $BUILD_FLAG -d

    # Wait for SQL Server if it exists in this compose

    SQLSERVER_CONTAINER=$(docker compose ps -q sqlserver 2>/dev/null || true)

    if [ -n "$SQLSERVER_CONTAINER" ]; then

      echo "Waiting for sqlserver in $service to be healthy..."

      until [ "$(docker inspect --format='{{.State.Health.Status}}' "$SQLSERVER_CONTAINER" 2>/dev/null)" = "healthy" ]; do

        echo "still waiting for sqlserver..."

        sleep 5

      done

      echo "sqlserver in $service is healthy!"

    fi


    # Wait for MongoDB if it exists in this compose

    MONGO_CONTAINER=$(docker compose ps -q mongodb 2>/dev/null || true)

    if [ -n "$MONGO_CONTAINER" ]; then

      echo "Waiting for mongodb in $service to be healthy..."

      until [ "$(docker inspect --format='{{.State.Health.Status}}' "$MONGO_CONTAINER" 2>/dev/null)" = "healthy" ]; do

        echo "still waiting for mongodb..."

        sleep 5

      done

      echo "mongodb in $service is healthy!"

    fi

    cd "$ROOT_DIR"

  else

    echo "!! Skipping $service (no docker-compose file)"

  fi

done


echo "--------------------------------------"

echo "All ERP services started!"

echo "--------------------------------------"
