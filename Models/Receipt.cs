namespace IcaReceiptTracker.Models;

public class Receipt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; }
    public string Store { get; set; } = string.Empty;
    public string? ReceiptNumber { get; set; }
    public List<ReceiptItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class ReceiptItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class Category
{
    public string Name { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
}

public class ExpensePeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Receipt> Receipts { get; set; } = new();
    public decimal Total { get; set; }
    public Dictionary<string, decimal> ByCategory { get; set; } = new();
}
