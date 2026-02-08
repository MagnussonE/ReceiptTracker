# ICA Receipt Tracker

A Blazor Server web application for tracking and analyzing ICA grocery receipt expenses in Sweden. Parse PDF receipts, categorize items, visualize spending patterns, and gain insights into your grocery shopping habits.

## Features

### 📄 Receipt Management
- **PDF Receipt Parsing**: Automatically extracts items, prices, dates, and store information from ICA PDF receipts
- **Dual Format Support**: Handles multiple ICA receipt PDF formats
- **Batch Upload**: Upload multiple receipts at once with progress tracking
- **Duplicate Detection**: Automatically identifies and skips duplicate receipts based on receipt number, date, store, and total
- **Receipt Number Tracking**: Extracts and stores ICA receipt numbers for reliable duplicate detection

### 🏷️ Category Management
- **Item Categorization**: Assign categories to grocery items for better expense tracking
- **Category Reuse**: Previously categorized items are automatically assigned to their categories
- **Interactive Categorization**: Step-by-step prompts for uncategorized items during upload
- **Category CRUD Operations**: Create, rename, delete, and reorganize categories
- **Item Movement**: Move items between categories with ease

### 📊 Expense Analytics
- **Date Range Filtering**: View expenses for custom date ranges (default: last 30 days)
- **Pie Chart Visualization**: Visual breakdown of spending by category using Chart.js
- **Category Breakdown**: Expandable category views showing:
  - Total spending per category
  - Individual items with quantity, average price, and total spent
  - Purchase frequency for each item
- **Receipt Timeline**: Chronological list of all receipts with expandable item details
- **Item-Level Analytics**: View all items purchased within a date range with aggregated statistics

### 💰 Currency & Localization
- **Swedish Currency (SEK)**: All amounts displayed in Swedish Kronor
- **Swedish Number Format**: Uses comma as decimal separator (e.g., "123,45 kr")
- **English Interface**: All UI text in English for international accessibility

## Tech Stack

- **.NET 9.0**: Latest .NET framework
- **Blazor Server**: Interactive server-side UI framework
- **C#**: Primary programming language
- **Chart.js 4.4.1**: Data visualization library for pie charts
- **UglyToad.PdfPig**: PDF parsing library for extracting receipt data
- **Bootstrap**: Responsive UI framework
- **CSS**: Custom styling with scoped component styles

## Architecture

### Clean Component-Based Structure

The application follows a modular component architecture with separated concerns:

```
Components/
├── Pages/
│   ├── Home.razor              # Main page container (86 lines)
│   ├── Home.razor.cs           # Shared state & handlers (428 lines)
│   └── Home.razor.css          # Scoped component styles (987 lines)
├── Tabs/                       # Tab components (self-contained)
│   ├── AddReceiptTab           # File upload & categorization
│   ├── ViewExpensesTab         # Expense overview & charts
│   ├── ItemsTab                # Item-level analytics
│   └── CategoriesTab           # Category management
├── Layout/                     # Navigation & layout
└── App.razor                   # Root application component

Models/
├── Receipt.cs                  # Receipt entity
├── ReceiptItem.cs              # Receipt item entity
├── Category.cs                 # Category with items
├── ExpensePeriod.cs            # Date-ranged expense data
└── QueuedFile.cs               # File upload queue item

Services/
├── DataService.cs              # Data persistence & CRUD operations
├── ReceiptParserService.cs     # PDF parsing logic
└── DateService.cs              # Date normalization utilities

wwwroot/
├── css/tabs.css                # Global tab styles (987 lines)
├── js/chartHelper.js           # Chart.js interop
└── lib/                        # Third-party libraries
```

### Key Design Patterns

- **Component-Based Architecture**: Each tab is a self-contained Blazor component with its own logic
- **EventCallback Pattern**: Clean parent-child communication via parameters
- **Service Layer**: Separation of business logic from UI components
- **Repository Pattern**: DataService abstracts data persistence
- **Scoped & Global CSS**: Strategic use of both for maintainability

## Prerequisites

- **.NET 9.0 SDK** or later
- **Modern web browser** (Chrome, Firefox, Safari, Edge)
- **macOS, Windows, or Linux**

## Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/MagnussonE/ReceiptTracker.git
   cd ReceiptTracker
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

## Running the Application

### Standard Run

```bash
dotnet run --urls "http://localhost:5556"
```

Then open your browser to: **http://localhost:5556**

> **Note**: Port 5556 is used because port 5000 is blocked by macOS ControlCenter.

### Using Launcher Scripts

The project includes several launcher scripts for convenience:

#### Shell Script (macOS/Linux)
```bash
./launcher.sh
```

#### Python Launcher
```bash
python launcher.py
```

#### AppleScript Launcher (macOS)
```bash
osascript LaunchApp.applescript
```

See `LAUNCHERS.md` for detailed information about each launcher.

## Usage

### 1. Upload Receipts

1. Navigate to the **Add Receipt** tab
2. Click "Choose Files" and select one or more ICA PDF receipts
3. Receipts are processed sequentially
4. For uncategorized items, you'll be prompted to:
   - Enter a new category name, or
   - Select from existing categories

### 2. View Expenses

1. Switch to the **View Expenses** tab
2. Adjust the date range using the "From" and "To" date pickers
3. View:
   - Total spending for the period
   - Pie chart breakdown by category
   - Expandable category details with item-level data
   - List of all receipts with expandable item lists
4. Select receipts and delete them if needed

### 3. Analyze Items

1. Go to the **Items by Date** tab
2. Select a date range
3. Click "Search" to view:
   - All items purchased in the period
   - Purchase frequency
   - Total quantity
   - Average price
   - Total spent per item

### 4. Manage Categories

1. Open the **Categories** tab
2. Operations available:
   - **Rename**: Edit category names
   - **Delete**: Remove categories (items become uncategorized)
   - **Expand**: View all items in a category
   - **Move Items**: Transfer items between categories
   - **Remove Items**: Uncategorize specific items
3. Use "Clear Database" to:
   - Clear receipts only (keeps categories)
   - Clear everything (receipts + categories)

## Data Storage

All data is stored locally in JSON files:

- **Location**: `~/.ica-receipt-tracker/`
- **Files**:
  - `receipts.json` - All receipt data
  - `categories.json` - Category assignments

**Privacy**: Your data never leaves your machine. No cloud storage or external services are used.

## Receipt Parsing

### Supported ICA Receipt Formats

The parser supports two ICA PDF receipt formats:

**Format 1**: Date/time on same line as labels
```
Allégatan 21 Datum 2026-02-08 Tid 15:01
Kvitto nr 4843
```

**Format 2**: Date/time 6 lines after labels
```
Datum
Tid
Org nr
Kvitto nr
Kassa
Kassör
2026-02-01    ← Actual date
15:01         ← Actual time
```

### Extracted Information

- **Receipt Number**: ICA kvitto number (primary duplicate detection)
- **Date**: Transaction date from receipt
- **Store**: ICA store location
- **Items**: Product name, quantity, unit price
- **Total**: Receipt total amount

## Development

### Project Structure

The application is organized into logical layers:

- **Presentation Layer**: Blazor components (`Components/`)
- **Business Logic**: Services (`Services/`)
- **Data Model**: Entities (`Models/`)
- **Static Assets**: CSS, JS, images (`wwwroot/`)

### Building

```bash
# Clean build
dotnet clean
dotnet build

# Release build
dotnet build -c Release

# Run tests (if available)
dotnet test
```

### Hot Reload

Blazor Server supports hot reload during development:

```bash
dotnet watch run --urls "http://localhost:5556"
```

Changes to `.razor` and `.cs` files will automatically reload.

## Troubleshooting

### Port Already in Use

If port 5556 is occupied:

```bash
# Find process using port 5556
lsof -ti :5556

# Kill the process
lsof -ti :5556 | xargs kill -9

# Or use a different port
dotnet run --urls "http://localhost:5557"
```

### Receipt Parsing Issues

If a receipt fails to parse:

1. Check `pdf_debug.log` in the project root for detailed extraction logs
2. Ensure the PDF is from ICA Sweden
3. Verify the PDF is not encrypted or image-based (must be text-based)
4. Open an issue with the PDF format details

### Data Reset

To reset all data:

1. Go to **Categories** tab
2. Click "Clear Database"
3. Choose "Clear Everything" to delete all receipts and categories

Or manually delete:
```bash
rm -rf ~/.ica-receipt-tracker/
```

## Limitations

- **ICA Sweden Only**: Parser is designed for Swedish ICA receipt formats
- **Text-Based PDFs**: Cannot parse image/scanned receipts
- **Single User**: No multi-user support (local storage only)
- **No Cloud Sync**: Data is stored locally on your machine

## Roadmap

Potential future enhancements:

- [ ] Support for other Swedish grocery chains (Coop, Willys, Hemköp)
- [ ] Export data to CSV/Excel
- [ ] Budget tracking and alerts
- [ ] Month-over-month comparison charts
- [ ] Shopping patterns analysis (frequency, day of week, etc.)
- [ ] Receipt image upload with OCR
- [ ] Multi-user support with authentication
- [ ] Mobile-responsive design improvements

## Contributing

Contributions are welcome! Please feel free to:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is provided as-is without any specific license. Feel free to use and modify for personal purposes.

## Author

**Eddie Magnusson**
- GitHub: [@MagnussonE](https://github.com/MagnussonE)
- Email: eddiemagnusson95@gmail.com

## Acknowledgments

- **UglyToad.PdfPig**: Excellent PDF parsing library
- **Chart.js**: Beautiful and flexible charting library
- **ICA Sweden**: For providing detailed text-based PDF receipts
- **Blazor Community**: For excellent documentation and support

---

**Made with ❤️ in Sweden 🇸🇪**
