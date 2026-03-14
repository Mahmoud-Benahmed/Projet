#!/usr/bin/env bash

# ── Colors ─────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

CONTAINERS=(erp-kafka erp-gateway erp-auth erp-auth-mongo erp-article erp-article-sqlserver erp-client erp-client-sqlserver)
echo -e "${BLUE}Removing existing containers...${NC}"

for container in "${CONTAINERS[@]}"; do
  if docker ps -a --filter "name=^/${container}$" --format '{{.Names}}'; then
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

echo -e "${BLUE}Cleaning unused Docker resources...${NC}"
docker system prune -f >/dev/null 2>&1

echo -e "${GREEN}Docker cleanup complete.${NC}"
