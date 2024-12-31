#!/usr/bin/env bash
#
# Start a receiver script and restarts it when crashing
#
# Function to display usage
usage() {
    echo "Usage: $0 <directory path> <routing-key>"
    echo "Starts the receiver service for a specific directory and a routing key"
    exit 1
}

# Check if at least two parameters (stoneName and registryName) are provided
if [[ $# -lt 2 ]]; then
    usage
    exit 1
fi

RMQTRANSFERHOME=$(dirname "$(readlink -f "$0")")
cd $RMQTRANSFERHOME
cd $RMQTRANSFERHOME

touch work
while [ -f work ]
do

nowTS=`date +%Y-%m-%d-%H-%M`

./rmqfiletransfer receivefiles --directory=$1  --mqrkey=$2  > ./file-directory-transfer-${nowTS}-${2}.log 2>&1

if [ ! -f work ]; then
   exit 0
else
   sleep 5s
fi
done
