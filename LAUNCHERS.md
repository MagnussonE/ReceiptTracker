# ICA Receipt Tracker Launchers

This folder contains multiple ways to launch the ICA Receipt Tracker application.

## Option 1: Python GUI Launcher (Recommended)

**File:** `launcher.py`

A simple GUI application with Start/Stop buttons.

**To run:**
```bash
python3 launcher.py
```

**Features:**
- ✅ Simple GUI with Start/Stop buttons
- ✅ Shows server status
- ✅ Opens browser automatically
- ✅ Clickable URL link
- ✅ Stops server when window closes

---

## Option 2: Shell Script Launcher

**File:** `launcher.sh`

A terminal-based launcher script.

**To run:**
```bash
./launcher.sh
```

**Features:**
- ✅ Runs in terminal
- ✅ Opens browser automatically
- ✅ Press Ctrl+C to stop

---

## Option 3: AppleScript Launcher (macOS only)

**File:** `ICAReceiptLauncher.applescript`

A native macOS dialog-based launcher.

**To run:**
```bash
osascript ICAReceiptLauncher.applescript
```

**Features:**
- ✅ Native macOS dialog
- ✅ Opens browser automatically
- ✅ Click OK to stop

---

## Manual Start

If you prefer to run the app directly:

```bash
cd /Users/em_rudholm/icareceipttracker
dotnet run
```

Then open: http://localhost:5001

Press Ctrl+C to stop.

---

## Creating a macOS App Bundle

To create a double-clickable application:

1. Open **Automator**
2. Create new **Application**
3. Add **Run Shell Script** action
4. Paste this:
   ```bash
   cd /Users/em_rudholm/icareceipttracker
   python3 launcher.py
   ```
5. Save as "ICA Receipt Tracker.app"
6. Done! Double-click to launch

---

## Troubleshooting

**Server won't start:**
- Make sure port 5001 is not in use
- Check if .NET is installed: `dotnet --version`

**Python launcher error:**
- Make sure Python 3 is installed: `python3 --version`
- tkinter should come with Python on macOS

**Permission denied:**
```bash
chmod +x launcher.sh launcher.py
```
