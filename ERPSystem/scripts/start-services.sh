#!/bin/bash
set -e
ROOT_DIR=$(pwd)

echo "Removing existing containers..."
docker rm -f erp-kafka erp-auth erp-user erp-gateway erp-auth-mongo erp-user-sqlserver 2>/dev/null || true

echo "Starting shared infrastructure..."
bash "$ROOT_DIR/start-kafka.sh"

echo "Waiting for Kafka to be healthy..."
until docker inspect erp-kafka --format='{{.State.Health.Status}}' 2>/dev/null | grep -q "healthy"; do
  echo "still waiting..."
  sleep 5
done
echo "Kafka is healthy!"

for service in ERP.AuthService ERP.UserService ERP.Gateway; do
  [ -d "$service" ] || continue
  if [ -f "$service/docker-compose.yaml" ] || [ -f "$service/docker-compose.yml" ]; then
    echo "--------------------------------------"
    echo "Starting $service..."
    echo "--------------------------------------"
    cd "$ROOT_DIR/$service"
    docker compose down || true
    docker compose up --build -d   # ← build and start together
    cd "$ROOT_DIR"
  else
    echo "!! Skipping $service (no docker-compose file)"
  fi
done

echo "--------------------------------------"
echo "All ERP services started!"
echo "--------------------------------------"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
