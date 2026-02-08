namespace IcaReceiptTracker.Models;

public class QueuedFile
{
    public required string FileName { get; set; }
    public required byte[] FileData { get; set; }
}
