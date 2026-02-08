using Microsoft.AspNetCore.Components;
using IcaReceiptTracker.Models;

namespace IcaReceiptTracker.Components.Tabs;

public partial class ItemsTab
{
    [Parameter] public List<ReceiptItem>? Items { get; set; }
    [Parameter] public string StartDate { get; set; } = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
    [Parameter] public string EndDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    [Parameter] public EventCallback OnSearchRequested { get; set; }
    [Parameter] public EventCallback<string> OnStartDateChanged { get; set; }
    [Parameter] public EventCallback<string> OnEndDateChanged { get; set; }

    private async Task OnSearch()
    {
        await OnSearchRequested.InvokeAsync();
    }

    private async Task HandleStartDateChange(ChangeEventArgs e)
    {
        var newDate = e.Value?.ToString() ?? DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
        await OnStartDateChanged.InvokeAsync(newDate);
    }

    private async Task HandleEndDateChange(ChangeEventArgs e)
    {
        var newDate = e.Value?.ToString() ?? DateTime.Now.ToString("yyyy-MM-dd");
        await OnEndDateChanged.InvokeAsync(newDate);
    }

    private string FormatCurrency(decimal amount)
    {
        return $"{amount:N2} kr";
    }
}
