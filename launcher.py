#!/usr/bin/env python3
"""
ICA Receipt Tracker Launcher
A simple GUI application that manages the Blazor server lifecycle
"""

import os
import sys
import subprocess
import time
import signal
import webbrowser
import tkinter as tk
from tkinter import messagebox
from pathlib import Path

class ICAReceiptLauncher:
    def __init__(self):
        self.server_process = None
        self.app_dir = Path(__file__).parent.absolute()
        self.url = "http://localhost:5001"
        
        # Create the GUI
        self.root = tk.Tk()
        self.root.title("ICA Receipt Tracker")
        self.root.geometry("500x300")
        self.root.resizable(False, False)
        
        # Center the window
        self.center_window()
        
        # Create UI elements
        self.create_widgets()
        
        # Handle window close
        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)
        
    def center_window(self):
        """Center the window on screen"""
        self.root.update_idletasks()
        width = self.root.winfo_width()
        height = self.root.winfo_height()
        x = (self.root.winfo_screenwidth() // 2) - (width // 2)
        y = (self.root.winfo_screenheight() // 2) - (height // 2)
        self.root.geometry(f'{width}x{height}+{x}+{y}')
        
    def create_widgets(self):
        """Create the UI components"""
        # Title
        title_label = tk.Label(
            self.root,
            text="ICA Receipt Tracker",
            font=("Helvetica", 24, "bold"),
            pady=20
        )
        title_label.pack()
        
        # Status label
        self.status_label = tk.Label(
            self.root,
            text="Click 'Start' to launch the application",
            font=("Helvetica", 12),
            pady=10
        )
        self.status_label.pack()
        
        # URL label (initially hidden)
        self.url_label = tk.Label(
            self.root,
            text="",
            font=("Helvetica", 10),
            fg="blue",
            cursor="hand2"
        )
        self.url_label.pack(pady=5)
        self.url_label.bind("<Button-1>", lambda e: self.open_browser())
        
        # Button frame
        button_frame = tk.Frame(self.root)
        button_frame.pack(pady=20)
        
        # Start button
        self.start_button = tk.Button(
            button_frame,
            text="Start Server",
            command=self.start_server,
            width=15,
            height=2,
            bg="#007bff",
            fg="white",
            font=("Helvetica", 12, "bold")
        )
        self.start_button.pack(side=tk.LEFT, padx=10)
        
        # Stop button (initially disabled)
        self.stop_button = tk.Button(
            button_frame,
            text="Stop Server",
            command=self.stop_server,
            width=15,
            height=2,
            bg="#dc3545",
            fg="white",
            font=("Helvetica", 12, "bold"),
            state=tk.DISABLED
        )
        self.stop_button.pack(side=tk.LEFT, padx=10)
        
        # Instructions
        instructions = tk.Label(
            self.root,
            text="The application will open in your default browser.\nClose this window to stop the server.",
            font=("Helvetica", 10),
            fg="gray",
            pady=10
        )
        instructions.pack()
        
    def start_server(self):
        """Start the Blazor server"""
        try:
            self.status_label.config(text="Starting server...")
            self.start_button.config(state=tk.DISABLED)
            self.root.update()
            
            # Change to app directory
            os.chdir(self.app_dir)
            
            # Start the server
            self.server_process = subprocess.Popen(
                ["dotnet", "run", "--urls", self.url],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                cwd=self.app_dir
            )
            
            # Wait for server to start
            time.sleep(3)
            
            # Check if server is still running
            if self.server_process.poll() is not None:
                raise Exception("Server failed to start")
            
            # Open browser
            webbrowser.open(self.url)
            
            # Update UI
            self.status_label.config(text="Server is running!", fg="green")
            self.url_label.config(text=f"Click here to open: {self.url}")
            self.stop_button.config(state=tk.NORMAL)
            
        except Exception as e:
            messagebox.showerror("Error", f"Failed to start server:\n{str(e)}")
            self.status_label.config(text="Error starting server", fg="red")
            self.start_button.config(state=tk.NORMAL)
            
    def stop_server(self):
        """Stop the Blazor server"""
        if self.server_process:
            self.status_label.config(text="Stopping server...")
            self.root.update()
            
            try:
                # Terminate the process
                self.server_process.terminate()
                self.server_process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                # Force kill if it doesn't stop
                self.server_process.kill()
                
            self.server_process = None
            
            # Update UI
            self.status_label.config(text="Server stopped", fg="black")
            self.url_label.config(text="")
            self.start_button.config(state=tk.NORMAL)
            self.stop_button.config(state=tk.DISABLED)
            
    def open_browser(self):
        """Open the application URL in browser"""
        webbrowser.open(self.url)
        
    def on_closing(self):
        """Handle window close event"""
        if self.server_process:
            if messagebox.askokcancel("Quit", "Stop the server and quit?"):
                self.stop_server()
                self.root.destroy()
        else:
            self.root.destroy()
            
    def run(self):
        """Start the GUI application"""
        self.root.mainloop()

if __name__ == "__main__":
    app = ICAReceiptLauncher()
    app.run()
