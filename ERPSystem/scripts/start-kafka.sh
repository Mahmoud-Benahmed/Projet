#!/usr/bin/env bash
set -e

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# ── Navigate to kafka directory ───────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
KAFKA_DIR="$SCRIPT_DIR/../kafka"

if [ ! -d "$KAFKA_DIR" ]; then
  echo -e "${RED}!! Kafka directory not found at $KAFKA_DIR${NC}"
  exit 1
fi
cd "$KAFKA_DIR"

# ── Tear down existing kafka stack cleanly ────────────────────────────────────
echo -e "${BLUE}Tearing down existing Kafka stack...${NC}"
docker compose down --remove-orphans >/dev/null 2>&1 || true

# ── Start Kafka ───────────────────────────────────────────────────────────────
echo -e "${BLUE}Starting Kafka...${NC}"
if docker compose up --build -d; then
  echo -e "${GREEN}Kafka started successfully.${NC}"
else
  echo -e "${RED}Failed to start Kafka.${NC}"
  exit 1
fi

# ── Wait for healthy ──────────────────────────────────────────────────────────
echo -e "${YELLOW}Waiting for Kafka to be healthy...${NC}"
RETRIES=0
MAX_RETRIES=24  # 24 x 5s = 2 minutes max

until docker inspect erp-kafka --format='{{.State.Health.Status}}' 2>/dev/null | grep -q "healthy"; do
  RETRIES=$((RETRIES + 1))
  if [ "$RETRIES" -ge "$MAX_RETRIES" ]; then
    echo -e "${RED}!! Kafka did not become healthy after $((MAX_RETRIES * 5))s — aborting.${NC}"
    exit 1
  fi
  echo -e "${YELLOW}Still waiting... ($RETRIES/$MAX_RETRIES)${NC}"
  sleep 5
done

echo -e "${GREEN}Kafka is healthy!${NC}"