#!/bin/sh
HOSTNAME="binaryvibrance.net"

docker stop mmo-message-bus
docker stop mmo-mongo-character
docker stop mmo-mongo-auth

# RABBIT MQ
# With Management
docker run -d --hostname mmo-message-bus.$HOSTNAME --rm --name mmo-message-bus -p 15672:15672 -p 5672:5672 rabbitmq:3.8-management
# Without Management
#docker run -d --hostname mmo-message-bus.$HOSTNAME --name mmo-message-bus -p 5672:5672 rabbitmq:3.8

# MONGODB
# Auth database
docker run -d --hostname mmo-auth-db.$HOSTNAME --rm --name mmo-mongo-auth -p 27017:27017 mongo:5.0-rc
# Character databse
docker run -d --hostname mmo-character-db.$HOSTNAME --rm --name mmo-mongo-character -p 27018:27017 mongo:5.0-rc

docker ps