#!/usr/bin/env bash
#
# Stops a receiver script 
#
# Function to display usage
usage() {
    echo "Usage: $0 "
    echo "Starts the receiver service for a specific directory and a routing key"
    exit 1
}


RMQTRANSFERHOME=$(dirname "$(readlink -f "$0")")
cd $RMQTRANSFERHOME

rmdir work

pkill rmqfiletransfer 
