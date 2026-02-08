using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using IcaReceiptTracker.Models;

namespace IcaReceiptTracker.Services;

public class ReceiptParserService
{
    private readonly DataService _dataService;
    private readonly string _logPath = "/Users/em_rudholm/icareceipttracker/pdf_debug.log";

    public ReceiptParserService(DataService dataService)
    {
        _dataService = dataService;
    }

    private void LogDebug(string message)
    {
        try
        {
            File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
        catch
        {
            // Silently fail if logging doesn't work
        }
    }

    public async Task<Receipt> ParsePdfReceiptAsync(Stream pdfStream)
    {
        // Clear log file at start
        try
        {
            File.WriteAllText(_logPath, "");
            LogDebug("=== NEW PDF PARSING SESSION ===");
        }
        catch
        {
            // Continue even if we can't create log
        }

        // Copy to memory stream to avoid synchronous read issues
        using var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        
        LogDebug("PDF stream copied to memory");
        
        using var pdfReader = new PdfReader(memoryStream);
        using var pdfDocument = new PdfDocument(pdfReader);
        
        var pageCount = pdfDocument.GetNumberOfPages();
        LogDebug($"PDF has {pageCount} page(s)");
        
        var text = "";
        for (int i = 1; i <= pageCount; i++)
        {
            var page = pdfDocument.GetPage(i);
            var strategy = new LocationTextExtractionStrategy();
            var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
            text += pageText;
            LogDebug($"Extracted {pageText.Length} characters from page {i}");
        }

        LogDebug("\n=== EXTRACTED PDF TEXT ===");
        LogDebug(text);
        LogDebug("=== END PDF TEXT ===\n");

        return ParseIcaReceiptText(text);
    }

    private Receipt ParseIcaReceiptText(string text)
    {
        var receipt = new Receipt();
        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        LogDebug($"=== PARSED LINES ({lines.Count} total) ===");
        for (int i = 0; i < Math.Min(100, lines.Count); i++)
        {
            LogDebug($"Line {i}: {lines[i]}");
        }
        LogDebug("=== END LINES ===\n");

        // Parse store name
        receipt.Store = ParseStoreName(lines);
        LogDebug($"Store: {receipt.Store}");

        // Parse date and receipt number
        var (date, receiptNumber) = ParseReceiptDateAndNumber(lines);
        receipt.Date = date;
        receipt.ReceiptNumber = receiptNumber;
        LogDebug($"Date: {receipt.Date}");
        LogDebug($"Receipt Number: {receipt.ReceiptNumber}");

        // Parse items
        receipt.Items = ParseReceiptItems(lines);
        LogDebug($"Parsed {receipt.Items.Count} items total");

        // Calculate total
        receipt.Total = receipt.Items.Sum(i => i.Price * i.Quantity);
        LogDebug($"Receipt total: {receipt.Total:F2} kr\n");

        return receipt;
    }

    private string ParseStoreName(List<string> lines)
    {
        // Look for store name after "Kvitto"
        var storeIndex = lines.FindIndex(l => l.Contains("Kvitto"));
        if (storeIndex >= 0 && storeIndex + 1 < lines.Count)
        {
            return lines[storeIndex + 1];
        }
        return "ICA";
    }

    private (DateTime date, string? receiptNumber) ParseReceiptDateAndNumber(List<string> lines)
    {
        DateTime date = DateTime.Now;
        string? receiptNumber = null;
        
        // Try multiple patterns for date parsing
        
        // Pattern 1: "Datum" on same line as date (e.g., "Allégatan 21 Datum 2026-02-08")
        var datumLineIndex = lines.FindIndex(l => l.Contains("Datum", StringComparison.OrdinalIgnoreCase));
        if (datumLineIndex >= 0)
        {
            var datumLine = lines[datumLineIndex];
            LogDebug($"Found 'Datum' at line {datumLineIndex}: '{datumLine}'");
            
            // Extract date from the same line using regex
            var dateMatch = Regex.Match(datumLine, @"\d{4}-\d{2}-\d{2}");
            if (dateMatch.Success)
            {
                if (DateTime.TryParse(dateMatch.Value, out var parsedDate))
                {
                    date = parsedDate;
                    LogDebug($"Successfully parsed date from same line: {date}");
                    
                    // Try to find time on next line
                    if (datumLineIndex + 1 < lines.Count)
                    {
                        var nextLine = lines[datumLineIndex + 1];
                        var timeMatch = Regex.Match(nextLine, @"\d{2}:\d{2}");
                        if (timeMatch.Success && TimeSpan.TryParse(timeMatch.Value, out var time))
                        {
                            date = date.Add(time);
                            LogDebug($"Added time from next line: {date}");
                        }
                    }
                }
            }
            else
            {
                // Pattern 2: Date might be 6 lines after "Datum" label (old format)
                if (datumLineIndex + 6 < lines.Count)
                {
                    var dateLine = lines[datumLineIndex + 6];
                    LogDebug($"Trying to parse date from line {datumLineIndex + 6}: '{dateLine}'");
                    
                    if (DateTime.TryParse(dateLine, out var parsedDate))
                    {
                        date = parsedDate;
                        
                        // Try to add time component
                        if (datumLineIndex + 7 < lines.Count && TimeSpan.TryParse(lines[datumLineIndex + 7], out var time))
                        {
                            date = date.Add(time);
                            LogDebug($"Successfully parsed date with time: {date}");
                        }
                        else
                        {
                            LogDebug($"Successfully parsed date without time: {date}");
                        }
                    }
                }
            }
        }
        
        // Try multiple patterns for receipt number parsing
        
        // Pattern 1: "Kvitto nr" followed by number on same line (e.g., "Kvitto nr 4142")
        var kvittoLine = lines.FirstOrDefault(l => l.Contains("Kvitto nr", StringComparison.OrdinalIgnoreCase));
        if (kvittoLine != null)
        {
            LogDebug($"Found 'Kvitto nr' line: '{kvittoLine}'");
            
            // Extract number after "Kvitto nr" using regex
            var kvittoMatch = Regex.Match(kvittoLine, @"Kvitto\s+nr\s+(\d+)", RegexOptions.IgnoreCase);
            if (kvittoMatch.Success)
            {
                receiptNumber = kvittoMatch.Groups[1].Value;
                LogDebug($"Found receipt number on same line: {receiptNumber}");
            }
            else
            {
                // Pattern 2: Try finding index and looking 3 lines later (old format)
                var kvittoIndex = lines.FindIndex(l => l.Contains("Kvitto nr", StringComparison.OrdinalIgnoreCase));
                if (kvittoIndex >= 0 && kvittoIndex + 3 < lines.Count)
                {
                    var potentialNumber = lines[kvittoIndex + 3];
                    // Only accept if it's a number
                    if (Regex.IsMatch(potentialNumber, @"^\d+$"))
                    {
                        receiptNumber = potentialNumber;
                        LogDebug($"Found receipt number 3 lines later: {receiptNumber}");
                    }
                }
            }
        }
        
        if (receiptNumber == null)
        {
            LogDebug("Could not find receipt number");
        }
        
        return (date, receiptNumber);
    }

    private List<ReceiptItem> ParseReceiptItems(List<string> lines)
    {
        var items = new List<ReceiptItem>();
        
        // Find where "Beskrivning" appears - this is the header line
        var headerIndex = lines.FindIndex(l => l.Contains("Beskrivning"));
        if (headerIndex < 0)
        {
            LogDebug("ERROR: Could not find 'Beskrivning' header");
            return items;
        }

        LogDebug($"\nFound header at line {headerIndex}: {lines[headerIndex]}");
        LogDebug("=== PARSING ITEMS ===");

        var processedLines = new HashSet<int>(); // Track which lines we've already processed

        // Now look through the rest of the text for lines matching the item pattern
        for (int i = headerIndex + 1; i < lines.Count; i++)
        {
            // Skip if we already processed this line (e.g., as a discount)
            if (processedLines.Contains(i))
                continue;

            var line = lines[i];
            
            // Stop at certain keywords that indicate end of items
            if (line.Contains("Betalat") || 
                line.Contains("Moms %") || 
                line.Contains("Erhållen rabatt") ||
                line.Contains("Betalningsinformation"))
            {
                LogDebug($"Stopping at line {i}: {line}");
                break;
            }

            // Try to parse as an item FIRST (items have 13-digit article numbers)
            var item = ParseItemLine(line, i);
            if (item != null)
            {
                items.Add(item);
                LogDebug($"✓ Item {items.Count}: {item.Name}");
                LogDebug($"  Price: {item.Price:F2} kr × {item.Quantity} = {(item.Price * item.Quantity):F2} kr");
            }
            // If not an item, check if this line contains a discount
            // Skip summary lines like "Erhållen rabatt" and general store discounts
            else if (!line.Contains("Erhållen rabatt") && 
                     !line.Contains("Storköpsrabatt") && 
                     !line.Contains("rabatt") && 
                     Regex.IsMatch(line, @"-[\d,]+"))
            {
                // This is an item-specific discount (like campaign offers)
                // Extract the discount amount from anywhere in the line
                var discountMatch = Regex.Match(line, @"-[\d,]+");
                if (discountMatch.Success && items.Count > 0)
                {
                    var discountStr = discountMatch.Value.Substring(1).Replace(",", ".");
                    if (decimal.TryParse(discountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var discount))
                    {
                        var lastItem = items[items.Count - 1];
                        var originalTotal = lastItem.Price * lastItem.Quantity;
                        var newTotal = originalTotal - discount;
                        
                        // Only apply if the discount doesn't make the item negative
                        if (newTotal >= 0)
                        {
                            lastItem.Price = newTotal / lastItem.Quantity;
                            
                            LogDebug($"  → Found item discount: '{line}'");
                            LogDebug($"  → Applied discount of -{discount:F2} kr to previous item ({lastItem.Name})");
                            LogDebug($"  → Original: {originalTotal:F2} kr, After discount: {newTotal:F2} kr");
                            LogDebug($"  → New price per unit: {lastItem.Price:F2} kr");
                            
                            processedLines.Add(i);
                        }
                        else
                        {
                            LogDebug($"  → Skipping discount '{line}' - would make item negative");
                        }
                    }
                }
            }
            else if (Regex.IsMatch(line, @"-[\d,]+") && !line.Contains("Erhållen rabatt"))
            {
                // This is a store-wide discount - add it as a separate line item
                var discountMatch = Regex.Match(line, @"-[\d,]+");
                if (discountMatch.Success)
                {
                    var discountStr = discountMatch.Value.Substring(1).Replace(",", ".");
                    if (decimal.TryParse(discountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var discount))
                    {
                        var discountName = line.Replace(discountMatch.Value, "").Trim();
                        
                        var discountItem = new ReceiptItem
                        {
                            Name = discountName,
                            Price = -discount,
                            Quantity = 1,
                            Category = "Discount"
                        };
                        
                        items.Add(discountItem);
                        LogDebug($"✓ Store Discount: {discountName}");
                        LogDebug($"  Amount: -{discount:F2} kr");
                        processedLines.Add(i);
                    }
                }
            }
            else if (line.Length > 10 && Regex.IsMatch(line, @"\d{13}"))
            {
                // This line has an article number but didn't parse - log it
                LogDebug($"✗ Failed to parse line {i}: {line}");
            }
        }

        LogDebug($"\n=== TOTAL ITEMS PARSED: {items.Count} ===\n");
        return items;
    }

    private ReceiptItem? ParseItemLine(string line, int lineNumber)
    {
        // More flexible pattern: split by finding the 13-digit article number first
        // Pattern matches: optional *, anything, 13 digits, anything, quantity, unit, total
        var pattern = @"^(\*?)(.+?)\s+(\d{13})\s+([\d,]+)\s+([\d,]+)\s+(st|kg)\s+([\d,]+)\s*$";
        var match = Regex.Match(line, pattern);

        if (!match.Success)
        {
            // Try without trailing whitespace requirement
            pattern = @"^(\*?)(.+?)\s+(\d{13})\s+([\d,]+)\s+([\d,]+)\s+(st|kg)\s+([\d,]+)";
            match = Regex.Match(line, pattern);
        }

        if (match.Success)
        {
            var hasAsterisk = match.Groups[1].Value == "*";
            var name = match.Groups[2].Value.Trim();
            var articleNumber = match.Groups[3].Value;
            var unitPriceStr = match.Groups[4].Value.Replace(",", ".");
            var quantityStr = match.Groups[5].Value.Replace(",", ".");
            var unit = match.Groups[6].Value;
            var totalStr = match.Groups[7].Value.Replace(",", ".");

            LogDebug($"  Regex matched: name='{name}', article={articleNumber}, qty={quantityStr}, unit={unit}, total={totalStr}");

            if (decimal.TryParse(totalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var total) && 
                decimal.TryParse(quantityStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity) &&
                quantity > 0)
            {
                var category = _dataService.GetCategory(name) ?? "";
                
                // For weight-based items (kg), show weight in name and set quantity to 1
                if (unit == "kg")
                {
                    var weightDisplay = quantityStr.Replace(".", ","); // Keep Swedish format
                    return new ReceiptItem
                    {
                        Name = $"{name} ({weightDisplay} kg)",
                        Price = total,
                        Quantity = 1,
                        Category = category
                    };
                }
                else // count-based items (st)
                {
                    return new ReceiptItem
                    {
                        Name = name,
                        Price = total / quantity,
                        Quantity = (int)Math.Round(quantity),
                        Category = category
                    };
                }
            }
            else
            {
                LogDebug($"  Failed to parse numbers: total={totalStr}, qty={quantityStr}");
            }
        }
        else
        {
            LogDebug($"  Regex did not match at all");
        }

        return null;
    }

    public Receipt ParseKivraReceipt(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var receipt = new Receipt
        {
            Date = ParseDate(doc, ns),
            Store = ParseStore(doc, ns),
            Items = ParseItems(doc, ns)
        };

        receipt.Total = receipt.Items.Sum(i => i.Price * i.Quantity);

        return receipt;
    }

    private DateTime ParseDate(XDocument doc, XNamespace ns)
    {
        var dateElement = doc.Descendants(ns + "Date").FirstOrDefault()
            ?? doc.Descendants(ns + "date").FirstOrDefault()
            ?? doc.Descendants("Date").FirstOrDefault()
            ?? doc.Descendants("date").FirstOrDefault();

        if (dateElement != null && DateTime.TryParse(dateElement.Value, out var date))
        {
            return date;
        }

        return DateTime.Now;
    }

    private string ParseStore(XDocument doc, XNamespace ns)
    {
        var storeElement = doc.Descendants(ns + "Store").FirstOrDefault()
            ?? doc.Descendants(ns + "store").FirstOrDefault()
            ?? doc.Descendants("Store").FirstOrDefault()
            ?? doc.Descendants("store").FirstOrDefault()
            ?? doc.Descendants(ns + "Seller").FirstOrDefault()
            ?? doc.Descendants("Seller").FirstOrDefault();

        return storeElement?.Value ?? "ICA";
    }

    private List<ReceiptItem> ParseItems(XDocument doc, XNamespace ns)
    {
        var items = new List<ReceiptItem>();

        var itemElements = doc.Descendants(ns + "Item")
            .Concat(doc.Descendants(ns + "item"))
            .Concat(doc.Descendants("Item"))
            .Concat(doc.Descendants("item"))
            .Concat(doc.Descendants(ns + "Line"))
            .Concat(doc.Descendants("Line"));

        foreach (var element in itemElements)
        {
            var name = element.Element(ns + "Name")?.Value
                ?? element.Element(ns + "name")?.Value
                ?? element.Element("Name")?.Value
                ?? element.Element("name")?.Value
                ?? element.Element(ns + "Description")?.Value
                ?? element.Element("Description")?.Value
                ?? "";

            if (string.IsNullOrWhiteSpace(name)) continue;

            var priceStr = element.Element(ns + "Price")?.Value
                ?? element.Element(ns + "price")?.Value
                ?? element.Element("Price")?.Value
                ?? element.Element("price")?.Value
                ?? element.Element(ns + "Amount")?.Value
                ?? element.Element("Amount")?.Value
                ?? "0";

            var quantityStr = element.Element(ns + "Quantity")?.Value
                ?? element.Element(ns + "quantity")?.Value
                ?? element.Element("Quantity")?.Value
                ?? element.Element("quantity")?.Value
                ?? "1";

            decimal.TryParse(priceStr.Replace(",", "."), out var price);
            int.TryParse(quantityStr, out var quantity);
            if (quantity == 0) quantity = 1;

            var category = _dataService.GetCategory(name) ?? "";

            items.Add(new ReceiptItem
            {
                Name = name,
                Price = price,
                Quantity = quantity,
                Category = category
            });
        }

        return items;
    }
}
