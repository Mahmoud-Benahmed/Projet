#!/usr/bin/env bash

# ── Colors ─────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}Removing existing containers...${NC}"
docker rm -f erp-kafka >/dev/null 2>&1 || true

echo -e "${BLUE}Starting Kafka container...${NC}"
if docker compose up --build -d; then
  echo -e "${GREEN}Kafka container started.${NC}"
else
  echo -e "${RED}Failed to start Kafka container.${NC}"
  exit 1
fi

echo -e "${YELLOW}Waiting for Kafka to be healthy...${NC}"

until docker inspect erp-kafka --format='{{.State.Health.Status}}' 2>/dev/null | grep -q "healthy"; do
  echo -e "${YELLOW}Still waiting...${NC}"
  sleep 5
done

echo -e "${GREEN}Kafka is healthy!${NC}"
