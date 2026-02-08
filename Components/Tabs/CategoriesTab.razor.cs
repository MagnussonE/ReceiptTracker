using Microsoft.AspNetCore.Components;
using IcaReceiptTracker.Models;

namespace IcaReceiptTracker.Components.Tabs;

public partial class CategoriesTab
{
    [Parameter] public List<Category>? Categories { get; set; }
    [Parameter] public List<string> AllCategoryNames { get; set; } = new();
    [Parameter] public int ReceiptsCount { get; set; }
    [Parameter] public int CategoriesCount { get; set; }
    [Parameter] public bool ShowClearDbConfirmation { get; set; }
    [Parameter] public string? EditingCategory { get; set; }
    [Parameter] public string? MovingItem { get; set; }
    [Parameter] public string? MovingFromCategory { get; set; }
    
    [Parameter] public EventCallback OnShowClearDbConfirmationRequested { get; set; }
    [Parameter] public EventCallback OnCancelClearDbRequested { get; set; }
    [Parameter] public EventCallback OnClearAllDataRequested { get; set; }
    [Parameter] public EventCallback OnClearReceiptsOnlyRequested { get; set; }
    [Parameter] public EventCallback<string> OnStartEditCategoryRequested { get; set; }
    [Parameter] public EventCallback<string> OnSaveCategoryRenameRequested { get; set; }
    [Parameter] public EventCallback OnCancelCategoryEditRequested { get; set; }
    [Parameter] public EventCallback<string> OnDeleteCategoryRequested { get; set; }
    [Parameter] public EventCallback<(string itemName, string fromCategory)> OnStartMoveItemRequested { get; set; }
    [Parameter] public EventCallback<(string itemName, string toCategory)> OnExecuteMoveRequested { get; set; }
    [Parameter] public EventCallback OnCancelMoveRequested { get; set; }
    [Parameter] public EventCallback<(string itemName, string categoryName)> OnRemoveItemRequested { get; set; }

    private HashSet<string> expandedCategories = new();
    private string categoryNewName = "";
    private string moveToCategory = "";

    private async Task OnShowClearDbConfirmation()
    {
        await OnShowClearDbConfirmationRequested.InvokeAsync();
    }

    private async Task OnCancelClearDb()
    {
        await OnCancelClearDbRequested.InvokeAsync();
    }

    private async Task OnClearAllData()
    {
        await OnClearAllDataRequested.InvokeAsync();
    }

    private async Task OnClearReceiptsOnly()
    {
        await OnClearReceiptsOnlyRequested.InvokeAsync();
    }

    private async Task OnStartEditCategory(string categoryName)
    {
        categoryNewName = categoryName;
        await OnStartEditCategoryRequested.InvokeAsync(categoryName);
    }

    private async Task OnSaveCategoryRename()
    {
        await OnSaveCategoryRenameRequested.InvokeAsync(categoryNewName);
        categoryNewName = "";
    }

    private async Task OnCancelCategoryEdit()
    {
        categoryNewName = "";
        await OnCancelCategoryEditRequested.InvokeAsync();
    }

    private async Task OnDeleteCategory(string categoryName)
    {
        await OnDeleteCategoryRequested.InvokeAsync(categoryName);
        expandedCategories.Remove(categoryName);
    }

    private async Task OnStartMoveItem(string itemName, string fromCategory)
    {
        moveToCategory = "";
        await OnStartMoveItemRequested.InvokeAsync((itemName, fromCategory));
    }

    private async Task OnExecuteMove()
    {
        if (!string.IsNullOrWhiteSpace(moveToCategory) && MovingItem != null)
        {
            await OnExecuteMoveRequested.InvokeAsync((MovingItem, moveToCategory));
            moveToCategory = "";
        }
    }

    private async Task OnCancelMove()
    {
        moveToCategory = "";
        await OnCancelMoveRequested.InvokeAsync();
    }

    private async Task OnRemoveItem(string itemName, string categoryName)
    {
        await OnRemoveItemRequested.InvokeAsync((itemName, categoryName));
    }

    private void ToggleCategoryExpansion(string categoryKey)
    {
        if (expandedCategories.Contains(categoryKey))
        {
            expandedCategories.Remove(categoryKey);
        }
        else
        {
            expandedCategories.Add(categoryKey);
        }
    }
}
