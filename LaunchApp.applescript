#!/usr/bin/osascript

# Simple ICA Receipt Tracker Launcher
# Save this as an Application in Script Editor for easy double-click launch

tell application "Terminal"
    activate
    do script "cd /Users/em_rudholm/icareceipttracker && python3 launcher.py"
end tell
