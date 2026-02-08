using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using IcaReceiptTracker.Models;

namespace IcaReceiptTracker.Components.Tabs;

public partial class ViewExpensesTab
{
    [Inject] public required IJSRuntime JS { get; set; }

    [Parameter] public ExpensePeriod? Expenses { get; set; }
    [Parameter] public string StartDate { get; set; } = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
    [Parameter] public string EndDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    [Parameter] public List<Receipt> SelectedReceipts { get; set; } = new();
    [Parameter] public bool ShowDeleteConfirmation { get; set; }
    [Parameter] public EventCallback<string> OnDateRangeChanged { get; set; }
    [Parameter] public EventCallback OnDeleteReceipts { get; set; }
    [Parameter] public EventCallback OnCancelDelete { get; set; }
    [Parameter] public EventCallback<Receipt> OnReceiptSelectionToggled { get; set; }
    [Parameter] public EventCallback OnSelectionCleared { get; set; }
    [Parameter] public EventCallback OnDeleteConfirmationRequested { get; set; }

    private HashSet<string> expandedReceipts = new();
    private HashSet<string> expandedCategories = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Render pie chart whenever expenses change
        if (Expenses != null && Expenses.ByCategory.Any())
        {
            await RenderPieChart();
        }
    }

    private async Task OnStartDateChanged(ChangeEventArgs e)
    {
        StartDate = e.Value?.ToString() ?? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
        await OnDateRangeChanged.InvokeAsync($"{StartDate}|{EndDate}");
    }

    private async Task OnEndDateChanged(ChangeEventArgs e)
    {
        EndDate = e.Value?.ToString() ?? DateTime.Now.ToString("yyyy-MM-dd");
        await OnDateRangeChanged.InvokeAsync($"{StartDate}|{EndDate}");
    }

    private async Task OnDeleteConfirmed()
    {
        await OnDeleteReceipts.InvokeAsync();
    }

    private async Task OnDeleteCancelled()
    {
        await OnCancelDelete.InvokeAsync();
    }

    private async Task OnToggleReceiptSelection(Receipt receipt)
    {
        await OnReceiptSelectionToggled.InvokeAsync(receipt);
    }

    private async Task OnClearSelection()
    {
        await OnSelectionCleared.InvokeAsync();
    }

    private async Task OnShowDeleteConfirmation()
    {
        await OnDeleteConfirmationRequested.InvokeAsync();
    }

    private void ToggleReceiptExpansion(string receiptKey)
    {
        if (expandedReceipts.Contains(receiptKey))
        {
            expandedReceipts.Remove(receiptKey);
        }
        else
        {
            expandedReceipts.Add(receiptKey);
        }
    }

    private void ToggleCategoryExpansion(string categoryId)
    {
        if (expandedCategories.Contains(categoryId))
        {
            expandedCategories.Remove(categoryId);
        }
        else
        {
            expandedCategories.Add(categoryId);
        }
    }

    private string FormatCurrency(decimal amount)
    {
        return $"{amount:N2} kr";
    }

    private async Task RenderPieChart()
    {
        if (Expenses == null || !Expenses.ByCategory.Any())
            return;

        var labels = Expenses.ByCategory.Keys.ToArray();
        var data = Expenses.ByCategory.Values.ToArray();
        var colors = GenerateColors(labels.Length);

        await JS.InvokeVoidAsync("chartHelper.destroyChart", "expensePieChart");
        await JS.InvokeVoidAsync("chartHelper.createPieChart", "expensePieChart", labels, data, colors);
    }

    private string[] GenerateColors(int count)
    {
        var colors = new List<string>
        {
            "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF",
            "#FF9F40", "#FF6384", "#C9CBCF", "#4BC0C0", "#FF6384",
            "#36A2EB", "#FFCE56"
        };

        while (colors.Count < count)
        {
            var r = Random.Shared.Next(100, 255);
            var g = Random.Shared.Next(100, 255);
            var b = Random.Shared.Next(100, 255);
            colors.Add($"#{r:X2}{g:X2}{b:X2}");
        }

        return colors.Take(count).ToArray();
    }
}
