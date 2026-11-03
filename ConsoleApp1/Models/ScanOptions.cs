namespace AsyncFileScanner.Models;

// Модель содержит настройки сканирования.
public sealed record ScanOptions(
    string RootDirectory,
    IReadOnlySet<string> Extensions);