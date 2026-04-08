#!/bin/bash


# ── Colors ─────────────────────────────

GREEN='\033[0;32m'

YELLOW='\033[1;33m'

BLUE='\033[0;34m'

RED='\033[0;31m'

NC='\033[0m' # No Color


# ── Flags ──────────────────────────────

DELETE=false

REMOVE_NETWORK=false


for arg in "$@"; do

  case $arg in

    --delete|-d) DELETE=true ;;

    --network|-n) REMOVE_NETWORK=true ;;

  esac

done


# ── Identify all ERP containers ─────────

ERP_CONTAINERS=$(docker ps -a --format '{{.Names}}' | grep '^erp-' || true)


if [ -z "$ERP_CONTAINERS" ]; then

  echo -e "${YELLOW}No ERP containers found.${NC}"

else

  if [ "$DELETE" = true ]; then

    echo -e "${BLUE}Removing ERP containers...${NC}"

    for container in $ERP_CONTAINERS; do

      echo -e "${BLUE}Removing $container...${NC}"

      docker rm -f "$container" >/dev/null 2>&1

      if docker ps -a --format '{{.Names}}' | grep -q "^$container$"; then

        echo -e "${RED}ERROR: Failed to remove $container${NC}"

        exit 1

      else

        echo -e "${GREEN}SUCCESS: $container removed${NC}"

      fi

    done

    echo -e "${BLUE}Cleaning unused Docker resources...${NC}"

    docker system prune -f >/dev/null 2>&1

    echo -e "${GREEN}Docker cleanup complete.${NC}"

  else

    echo -e "${BLUE}Stopping ERP containers...${NC}"

    for container in $ERP_CONTAINERS; do

      echo -e "${BLUE}Stopping $container...${NC}"

      docker stop "$container" >/dev/null 2>&1

      if docker ps --format '{{.Names}}' | grep -q "^$container$"; then

        echo -e "${RED}ERROR: Failed to stop $container${NC}"

        exit 1

      else

        echo -e "${GREEN}SUCCESS: $container stopped${NC}"

      fi

    done

    echo -e "${GREEN}All ERP containers stopped.${NC}"

  fi

fi


# ── Remove ERP network if requested ────

if [ "$REMOVE_NETWORK" = true ]; then

  ERP_NETWORKS=$(docker network ls --format '{{.Name}}' | grep 'erp-network' || true)

  if [ -n "$ERP_NETWORKS" ]; then

    for net in $ERP_NETWORKS; do

      echo -e "${BLUE}Removing network $net...${NC}"

      docker network rm "$net" >/dev/null 2>&1

      if docker network ls --format '{{.Name}}' | grep -q "^$net$"; then

        echo -e "${RED}ERROR: Failed to remove network $net${NC}"

        exit 1

      else

        echo -e "${GREEN}SUCCESS: Network $net removed${NC}"

      fi

    done

  else

    echo -e "${YELLOW}No ERP networks found.${NC}"

  fi

fi
