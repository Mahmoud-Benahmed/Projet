#!/usr/bin/env bash

# ── Colors ─────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

CONTAINERS=(
  erp-kafka
  erp-auth
  erp-user
  erp-gateway
  erp-auth-mongo
  erp-user-sqlserver
)

echo -e "${BLUE}Removing existing containers...${NC}"

for container in "${CONTAINERS[@]}"; do
  if docker ps -a --format '{{.Names}}' | grep -q "^${container}$"; then
    echo -e "${BLUE}Removing ${container}...${NC}"
    docker rm -f "$container" >/dev/null 2>&1

    # Verify removal
    if docker ps -a --format '{{.Names}}' | grep -q "^${container}$"; then
      echo -e "${RED}ERROR: Failed to remove ${container}${NC}"
      exit 1
    else
      echo -e "${GREEN}SUCCESS: ${container} removed${NC}"
    fi
  else
    echo -e "${YELLOW}WARNING: ${container} does not exist${NC}"
  fi
done

echo -e "${GREEN}Container cleanup completed successfully.${NC}"
