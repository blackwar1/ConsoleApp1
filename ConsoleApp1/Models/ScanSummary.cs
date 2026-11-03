namespace AsyncFileScanner.Models;

// Здесь хранятся общие результаты сканирования.
public sealed record ScanSummary(
    int ScannedFiles,
    int MatchedFiles,
    long TotalMatchedBytes,
    TimeSpan Duration,
    IReadOnlyList<ScanResult> Results,
    IReadOnlyList<string> Errors);