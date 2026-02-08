#!/usr/bin/osascript

# ICA Receipt Tracker Launcher (AppleScript)
# This creates a simple GUI window that manages the server

on run
    set appPath to (POSIX path of ((path to me as text) & "::"))
    
    # Start the .NET server in background
    do shell script "cd " & quoted form of appPath & " && dotnet run --urls 'http://localhost:5001' > /dev/null 2>&1 &"
    
    # Wait for server to start
    delay 3
    
    # Open browser
    do shell script "open 'http://localhost:5001'"
    
    # Show dialog with instructions
    display dialog "ICA Receipt Tracker is running!

The application is now open in your browser at:
http://localhost:5001

Click OK or close this window to stop the server." buttons {"Stop Server"} default button 1 with title "ICA Receipt Tracker" with icon note
    
    # When dialog closes, stop the server
    do shell script "pkill -f 'dotnet run.*5001'"
    
    display notification "ICA Receipt Tracker stopped" with title "ICA Receipt Tracker"
end run
