#!/bin/bash

# ICA Receipt Tracker Launcher
# This script starts the Blazor app and opens it in the default browser

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Change to the application directory
cd "$SCRIPT_DIR"

# Start the Blazor app in the background and capture its PID
dotnet run --urls "http://localhost:5001" &
SERVER_PID=$!

# Wait for the server to start
echo "Starting ICA Receipt Tracker..."
sleep 3

# Open the browser
open "http://localhost:5001"

echo "ICA Receipt Tracker is running at http://localhost:5001"
echo "Server PID: $SERVER_PID"
echo "Press Ctrl+C to stop the server"

# Function to cleanup on exit
cleanup() {
    echo ""
    echo "Shutting down ICA Receipt Tracker..."
    kill $SERVER_PID 2>/dev/null
    wait $SERVER_PID 2>/dev/null
    echo "Server stopped."
    exit 0
}

# Trap SIGINT (Ctrl+C) and SIGTERM
trap cleanup SIGINT SIGTERM

# Wait for the server process
wait $SERVER_PID
