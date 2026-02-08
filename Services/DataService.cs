using System.Text.Json;
using IcaReceiptTracker.Models;

namespace IcaReceiptTracker.Services;

public class DataService
{
    private readonly string _dataPath;
    private readonly string _receiptsFile;
    private readonly string _categoriesFile;
    
    private List<Receipt> _receipts = new();
    private List<Category> _categories = new();

    public DataService()
    {
        _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ica-receipt-tracker");
        _receiptsFile = Path.Combine(_dataPath, "receipts.json");
        _categoriesFile = Path.Combine(_dataPath, "categories.json");
        
        Directory.CreateDirectory(_dataPath);
        LoadData();
    }

    private void LoadData()
    {
        if (File.Exists(_receiptsFile))
        {
            var json = File.ReadAllText(_receiptsFile);
            _receipts = JsonSerializer.Deserialize<List<Receipt>>(json) ?? new();
        }

        if (File.Exists(_categoriesFile))
        {
            var json = File.ReadAllText(_categoriesFile);
            _categories = JsonSerializer.Deserialize<List<Category>>(json) ?? new();
        }
    }

    private void SaveData()
    {
        var receiptsJson = JsonSerializer.Serialize(_receipts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_receiptsFile, receiptsJson);

        var categoriesJson = JsonSerializer.Serialize(_categories, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_categoriesFile, categoriesJson);
    }

    public List<Receipt> GetAllReceipts() => _receipts;

    public bool IsDuplicateReceipt(Receipt receipt)
    {
        // First check by receipt number if available (most reliable)
        if (!string.IsNullOrEmpty(receipt.ReceiptNumber))
        {
            var duplicateByNumber = _receipts.Any(r => 
                r.ReceiptNumber == receipt.ReceiptNumber && 
                r.Store == receipt.Store);
            
            if (duplicateByNumber)
                return true;
        }
        
        // Fallback: Check if a receipt with the same date, store, and total already exists
        return _receipts.Any(r => 
            r.Date.Date == receipt.Date.Date && 
            r.Store == receipt.Store && 
            Math.Abs(r.Total - receipt.Total) < 0.01m);
    }

    public void AddReceipt(Receipt receipt)
    {
        _receipts.Add(receipt);
        SaveData();
    }

    public void DeleteReceipt(Receipt receipt)
    {
        _receipts.Remove(receipt);
        SaveData();
    }

    public void DeleteReceipts(List<Receipt> receipts)
    {
        foreach (var receipt in receipts)
        {
            _receipts.Remove(receipt);
        }
        SaveData();
    }

    public string? GetCategory(string itemName)
    {
        var category = _categories.FirstOrDefault(c => c.Items.Contains(itemName, StringComparer.OrdinalIgnoreCase));
        return category?.Name;
    }

    public void AddItemToCategory(string itemName, string categoryName)
    {
        var category = _categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        
        if (category == null)
        {
            category = new Category { Name = categoryName };
            _categories.Add(category);
        }

        if (!category.Items.Contains(itemName, StringComparer.OrdinalIgnoreCase))
        {
            category.Items.Add(itemName);
        }

        SaveData();
    }

    public List<string> GetAllCategories() => _categories.Select(c => c.Name).ToList();

    public List<Category> GetCategoriesWithItems() => _categories;

    public void RenameCategory(string oldName, string newName)
    {
        var category = _categories.FirstOrDefault(c => c.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase));
        if (category != null)
        {
            category.Name = newName;
            
            // Update all receipts with this category
            foreach (var receipt in _receipts)
            {
                foreach (var item in receipt.Items.Where(i => i.Category?.Equals(oldName, StringComparison.OrdinalIgnoreCase) == true))
                {
                    item.Category = newName;
                }
            }
            
            SaveData();
        }
    }

    public void DeleteCategory(string categoryName)
    {
        var category = _categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        if (category != null)
        {
            _categories.Remove(category);
            
            // Clear category from all receipt items
            foreach (var receipt in _receipts)
            {
                foreach (var item in receipt.Items.Where(i => i.Category?.Equals(categoryName, StringComparison.OrdinalIgnoreCase) == true))
                {
                    item.Category = "";
                }
            }
            
            SaveData();
        }
    }

    public void MoveItemToCategory(string itemName, string oldCategory, string newCategory)
    {
        // Remove from old category
        var oldCat = _categories.FirstOrDefault(c => c.Name.Equals(oldCategory, StringComparison.OrdinalIgnoreCase));
        if (oldCat != null)
        {
            oldCat.Items.RemoveAll(i => i.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (oldCat.Items.Count == 0)
            {
                _categories.Remove(oldCat);
            }
        }

        // Add to new category
        AddItemToCategory(itemName, newCategory);
        
        // Update all receipts with this item
        foreach (var receipt in _receipts)
        {
            foreach (var item in receipt.Items.Where(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase)))
            {
                item.Category = newCategory;
            }
        }
        
        SaveData();
    }

    public void RemoveItemFromCategory(string itemName, string categoryName)
    {
        var category = _categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        if (category != null)
        {
            category.Items.RemoveAll(i => i.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            
            if (category.Items.Count == 0)
            {
                _categories.Remove(category);
            }
            
            // Clear category from all receipt items
            foreach (var receipt in _receipts)
            {
                foreach (var item in receipt.Items.Where(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase)))
                {
                    item.Category = "";
                }
            }
            
            SaveData();
        }
    }

    public ExpensePeriod GetExpensesByPeriod(int year, int month)
    {
        var startDate = new DateTime(year, month, 25);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var receiptsInPeriod = _receipts
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .OrderBy(r => r.Date)
            .ToList();

        var byCategory = receiptsInPeriod
            .SelectMany(r => r.Items)
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Price * i.Quantity));

        return new ExpensePeriod
        {
            StartDate = startDate,
            EndDate = endDate,
            Receipts = receiptsInPeriod,
            Total = receiptsInPeriod.Sum(r => r.Total),
            ByCategory = byCategory
        };
    }

    public ExpensePeriod GetCurrentPeriod()
    {
        var now = DateTime.Now;
        var month = now.Day < 25 ? now.Month - 1 : now.Month;
        var year = month < 1 ? now.Year - 1 : now.Year;
        month = month < 1 ? 12 : month;
        
        return GetExpensesByPeriod(year, month);
    }

    public ExpensePeriod GetExpensesByDateRange(DateTime startDate, DateTime endDate)
    {
        // Normalize to date only (ignore time component)
        startDate = startDate.Date;
        endDate = endDate.Date.AddDays(1).AddTicks(-1); // Include entire end date
        
        var receiptsInPeriod = _receipts
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .OrderBy(r => r.Date)
            .ToList();

        var byCategory = receiptsInPeriod
            .SelectMany(r => r.Items)
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Price * i.Quantity));

        return new ExpensePeriod
        {
            StartDate = startDate,
            EndDate = endDate,
            Receipts = receiptsInPeriod,
            Total = receiptsInPeriod.Sum(r => r.Total),
            ByCategory = byCategory
        };
    }

    public List<ReceiptItem> GetItemsByDateRange(DateTime startDate, DateTime endDate)
    {
        return _receipts
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .SelectMany(r => r.Items.Select(item => new ReceiptItem
            {
                Name = item.Name,
                Price = item.Price,
                Quantity = item.Quantity,
                Category = item.Category
            }))
            .OrderBy(i => i.Name)
            .ToList();
    }

    public void ClearAllData()
    {
        _receipts.Clear();
        _categories.Clear();
        SaveData();
    }

    public void ClearReceiptsOnly()
    {
        _receipts.Clear();
        SaveData();
    }
}
