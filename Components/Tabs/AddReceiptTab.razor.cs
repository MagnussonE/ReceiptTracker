using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using IcaReceiptTracker.Models;

namespace IcaReceiptTracker.Components.Tabs;

public partial class AddReceiptTab
{
    [Parameter] public required List<ReceiptItem> PendingCategorizationItems { get; set; }
    [Parameter] public required string NewCategory { get; set; }
    [Parameter] public required List<string> ExistingCategories { get; set; }
    [Parameter] public required Queue<QueuedFile> ProcessingQueue { get; set; }
    [Parameter] public required bool IsProcessing { get; set; }
    [Parameter] public required string CurrentProcessingFile { get; set; }
    [Parameter] public required string UploadMessage { get; set; }
    [Parameter] public required bool UploadSuccess { get; set; }
    [Parameter] public required int ProcessedCount { get; set; }
    [Parameter] public required int SuccessCount { get; set; }
    [Parameter] public required int DuplicateCount { get; set; }
    [Parameter] public required int ErrorCount { get; set; }
    
    [Parameter] public EventCallback<InputFileChangeEventArgs> OnFileUpload { get; set; }
    [Parameter] public EventCallback<string> OnCategorySelected { get; set; }
    [Parameter] public EventCallback OnSaveCategory { get; set; }

    private string FormatCurrency(decimal amount)
    {
        return $"{amount:N2} kr";
    }
    
    private async Task HandleCategorySelected(string category)
    {
        await OnCategorySelected.InvokeAsync(category);
    }
}
