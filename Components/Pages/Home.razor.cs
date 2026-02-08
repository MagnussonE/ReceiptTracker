using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using IcaReceiptTracker.Models;
using IcaReceiptTracker.Services;

namespace IcaReceiptTracker.Components.Pages;

public partial class Home
{
    [Inject] public required DataService DataService { get; set; }
    [Inject] public required IJSRuntime JS { get; set; }

    // State
    private string activeTab = "add";
    private string uploadMessage = "";
    private bool uploadSuccess = false;
    private List<ReceiptItem> pendingCategorizationItems = new();
    private string newCategory = "";
    private Receipt? currentReceipt;
    private ExpensePeriod? currentExpenses;
    private string viewStartDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
    private string viewEndDate = DateTime.Now.ToString("yyyy-MM-dd");
    private string? editingCategory = null;
    private string? movingItem = null;
    private string? movingFromCategory = null;
    private bool showClearDbConfirmation = false;
    private bool showDeleteReceiptsConfirmation = false;
    private List<Receipt> selectedReceiptsForDeletion = new();
    private string itemsStartDate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
    private string itemsEndDate = DateTime.Now.ToString("yyyy-MM-dd");
    private List<ReceiptItem>? itemsInDateRange = null;
    
    // Multiple receipt processing
    private Queue<QueuedFile> processingQueue = new();
    private bool isProcessing = false;
    private string currentProcessingFile = "";
    private int processedCount = 0;
    private int successCount = 0;
    private int duplicateCount = 0;
    private int errorCount = 0;

    // Expansion toggles (kept for compatibility - components manage their own state)
    private HashSet<string> expandedReceipts = new();
    private HashSet<string> expandedCategories = new();
    private HashSet<string> expandedExpenseCategories = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadExpenses();
    }
    
    private void SwitchTab(string tab)
    {
        activeTab = tab;
    }
    
    private string FormatCurrency(decimal amount)
    {
        // Always format as SEK with Swedish number format (comma as decimal separator)
        return $"{amount:N2} kr";
    }

    // Receipt selection for deletion
    private void ToggleReceiptSelection(Receipt receipt)
    {
        if (selectedReceiptsForDeletion.Contains(receipt))
        {
            selectedReceiptsForDeletion.Remove(receipt);
        }
        else
        {
            selectedReceiptsForDeletion.Add(receipt);
        }
    }

    private void ClearReceiptSelection()
    {
        selectedReceiptsForDeletion.Clear();
    }

    private void ShowDeleteReceiptsConfirmation()
    {
        showDeleteReceiptsConfirmation = true;
    }

    private void CancelDeleteReceipts()
    {
        showDeleteReceiptsConfirmation = false;
    }

    private async Task DeleteSelectedReceipts()
    {
        DataService.DeleteReceipts(selectedReceiptsForDeletion);
        showDeleteReceiptsConfirmation = false;
        selectedReceiptsForDeletion.Clear();
        await LoadExpenses();
        uploadMessage = "Receipts deleted successfully!";
        uploadSuccess = true;
    }

    // ViewExpensesTab handlers
    private async Task HandleDateRangeChanged(string dateRange)
    {
        var parts = dateRange.Split('|');
        if (parts.Length == 2)
        {
            viewStartDate = parts[0];
            viewEndDate = parts[1];
            await LoadExpenses();
        }
    }

    // ItemsTab handlers
    private void HandleItemsStartDateChanged(string newDate)
    {
        itemsStartDate = newDate;
    }

    private void HandleItemsEndDateChanged(string newDate)
    {
        itemsEndDate = newDate;
    }

    // Items by date range
    private void LoadItemsByDateRange()
    {
        if (DateTime.TryParse(itemsStartDate, out var startDate) && 
            DateTime.TryParse(itemsEndDate, out var endDate))
        {
            itemsInDateRange = DataService.GetItemsByDateRange(startDate, endDate);
        }
    }

    // CategoriesTab handlers
    private void StartEditCategory(string categoryName)
    {
        editingCategory = categoryName;
    }

    private async Task HandleCategoryRename(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName) && editingCategory != null)
        {
            DataService.RenameCategory(editingCategory, newName);
            editingCategory = null;
            await LoadExpenses();
        }
    }

    private void CancelCategoryEdit()
    {
        editingCategory = null;
    }

    private async Task DeleteCategoryConfirm(string categoryName)
    {
        DataService.DeleteCategory(categoryName);
        await LoadExpenses();
    }

    private void HandleStartMoveItem((string itemName, string fromCategory) data)
    {
        movingItem = data.itemName;
        movingFromCategory = data.fromCategory;
    }

    private async Task HandleExecuteMove((string itemName, string toCategory) data)
    {
        if (!string.IsNullOrWhiteSpace(data.toCategory) && data.itemName != null && movingFromCategory != null)
        {
            DataService.MoveItemToCategory(data.itemName, movingFromCategory, data.toCategory);
            CancelMove();
            await LoadExpenses();
        }
    }

    private void CancelMove()
    {
        movingItem = null;
        movingFromCategory = null;
    }

    private async Task HandleRemoveItem((string itemName, string categoryName) data)
    {
        DataService.RemoveItemFromCategory(data.itemName, data.categoryName);
        await LoadExpenses();
    }

    // Expense loading
    private async Task LoadExpenses()
    {
        if (DateTime.TryParse(viewStartDate, out var startDate) && 
            DateTime.TryParse(viewEndDate, out var endDate))
        {
            // Ensure start date is not after end date
            if (startDate > endDate)
            {
                (startDate, endDate) = (endDate, startDate);
                viewStartDate = startDate.ToString("yyyy-MM-dd");
                viewEndDate = endDate.ToString("yyyy-MM-dd");
            }
            
            currentExpenses = DataService.GetExpensesByDateRange(startDate, endDate);
        }
        else
        {
            // Fallback to default 30 days
            var end = DateTime.Now;
            var start = end.AddDays(-30);
            currentExpenses = DataService.GetExpensesByDateRange(start, end);
        }
    }

    // File upload and receipt processing
    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        // Reset counters if starting a new batch
        if (!isProcessing && processingQueue.Count == 0)
        {
            processedCount = 0;
            successCount = 0;
            duplicateCount = 0;
            errorCount = 0;
        }
        
        // Read all files into memory immediately
        foreach (var file in e.GetMultipleFiles(100)) // Max 100 files
        {
            try
            {
                // Read file into memory right away to avoid stale reference issues
                using var stream = file.OpenReadStream(maxAllowedSize: 10485760); // 10MB limit
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                
                processingQueue.Enqueue(new QueuedFile 
                { 
                    FileName = file.Name, 
                    FileData = memoryStream.ToArray()
                });
            }
            catch (Exception ex)
            {
                uploadMessage = $"Failed to read {file.Name}: {ex.Message}";
                uploadSuccess = false;
                errorCount++;
            }
        }
        
        uploadMessage = $"Added {e.FileCount} file(s) to queue";
        uploadSuccess = true;
        StateHasChanged();
        
        // Start processing if not already processing
        if (!isProcessing)
        {
            await ProcessNextReceipt();
        }
    }

    private async Task ProcessNextReceipt()
    {
        // If we're categorizing, wait for user to finish
        if (pendingCategorizationItems.Any())
        {
            return;
        }
        
        // If queue is empty, we're done
        if (!processingQueue.Any())
        {
            isProcessing = false;
            currentProcessingFile = "";
            
            if (processedCount > 0)
            {
                uploadMessage = $"Batch complete! Processed {processedCount} receipt(s)";
                uploadSuccess = true;
                await LoadExpenses();
            }
            StateHasChanged();
            return;
        }
        
        isProcessing = true;
        var queuedFile = processingQueue.Dequeue();
        currentProcessingFile = queuedFile.FileName;
        StateHasChanged();
        
        try
        {
            uploadMessage = $"Processing {queuedFile.FileName}...";
            uploadSuccess = false;
            StateHasChanged();
            
            // Create stream from the byte array we stored earlier
            using var stream = new MemoryStream(queuedFile.FileData);

            var parser = new ReceiptParserService(DataService);
            currentReceipt = await parser.ParsePdfReceiptAsync(stream);

            if (currentReceipt.Items.Count == 0)
            {
                uploadMessage = $"❌ Failed to parse {queuedFile.FileName}: No items found. This receipt may have an unsupported format.";
                uploadSuccess = false;
                errorCount++;
                processedCount++;
                
                // Continue to next receipt after a brief delay
                await Task.Delay(1000);
                await ProcessNextReceipt();
                return;
            }

            // Check for duplicate receipt
            if (DataService.IsDuplicateReceipt(currentReceipt))
            {
                uploadMessage = $"⚠️ Already uploaded: {queuedFile.FileName} (same store, date, and total) - skipped";
                uploadSuccess = false;
                duplicateCount++;
                processedCount++;
                currentReceipt = null;
                
                // Continue to next receipt after a brief delay
                await Task.Delay(1000);
                await ProcessNextReceipt();
                return;
            }

            pendingCategorizationItems = currentReceipt.Items
                .Where(i => string.IsNullOrEmpty(i.Category))
                .ToList();

            if (!pendingCategorizationItems.Any())
            {
                DataService.AddReceipt(currentReceipt);
                uploadMessage = $"✓ {queuedFile.FileName}: {currentReceipt.Items.Count} items, {FormatCurrency(currentReceipt.Total)}";
                uploadSuccess = true;
                successCount++;
                processedCount++;
                currentReceipt = null;
                
                // Continue to next receipt
                await Task.Delay(500);
                await ProcessNextReceipt();
            }
            else
            {
                uploadMessage = $"📋 {queuedFile.FileName}: {currentReceipt.Items.Count} items ({FormatCurrency(currentReceipt.Total)}). Please categorize {pendingCategorizationItems.Count} new item(s).";
                uploadSuccess = true;
                StateHasChanged();
                // Processing will continue after categorization is complete
            }
        }
        catch (Exception ex)
        {
            uploadMessage = $"❌ Failed to process {queuedFile.FileName}: {ex.Message}";
            uploadSuccess = false;
            errorCount++;
            processedCount++;
            
            // Continue to next receipt after a brief delay
            await Task.Delay(1000);
            await ProcessNextReceipt();
        }
    }

    private void AssignCategory(string category)
    {
        newCategory = category;
    }

    private async Task SaveCategory()
    {
        if (string.IsNullOrWhiteSpace(newCategory) || !pendingCategorizationItems.Any())
            return;

        var item = pendingCategorizationItems[0];
        item.Category = newCategory;
        DataService.AddItemToCategory(item.Name, newCategory);
        
        pendingCategorizationItems.RemoveAt(0);
        newCategory = "";

        if (!pendingCategorizationItems.Any() && currentReceipt != null)
        {
            DataService.AddReceipt(currentReceipt);
            uploadMessage = $"✓ Receipt added: {currentReceipt.Items.Count} items, {FormatCurrency(currentReceipt.Total)}";
            uploadSuccess = true;
            successCount++;
            processedCount++;
            currentReceipt = null;
            
            // Continue processing the queue
            await ProcessNextReceipt();
        }
    }

    // Settings actions
    private void ShowClearDbConfirmation()
    {
        showClearDbConfirmation = true;
    }

    private void CancelClearDb()
    {
        showClearDbConfirmation = false;
    }

    private async Task ClearAllData()
    {
        DataService.ClearAllData();
        showClearDbConfirmation = false;
        await LoadExpenses();
        uploadMessage = "All data cleared!";
        uploadSuccess = true;
    }

    private async Task ClearReceiptsOnly()
    {
        DataService.ClearReceiptsOnly();
        showClearDbConfirmation = false;
        await LoadExpenses();
        uploadMessage = "All receipts cleared! Categories preserved.";
        uploadSuccess = true;
    }
}
