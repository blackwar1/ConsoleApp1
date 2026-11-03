namespace AsyncFileScanner.Models;

// Модель результата обработки одного файла.
public sealed record ScanResult(
    string FileName,
    string FullPath,
    long SizeBytes,
    int BytesRead);