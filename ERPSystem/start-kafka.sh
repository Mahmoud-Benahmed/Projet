#!/bin/bash

echo " Removing existing containers..."
docker rm -f erp-kafka

echo " Starting kafka container..."
docker compose up --build -d

echo " Waiting for Kafka to be healthy..."
until docker inspect erp-kafka --format='{{.State.Health.Status}}' 2>/dev/null | grep -q "healthy"; do
  echo " still waiting..."
  sleep 5
done
echo " Kafka is healthy!"

