using AsyncFileScanner.Models;
using AsyncFileScanner.Services;

Console.WriteLine("=== FILE SCANNER ===");

Console.Write("Введите путь к директории: ");
string root = Console.ReadLine() ?? "";

Console.Write("Введите расширения через запятую: ");
string[] extensions = (Console.ReadLine() ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
    .Select(x => x.StartsWith('.') ? x : "." + x)
    .ToArray();

var options = new ScanOptions(
    root,
    new HashSet<string>(
        extensions,
        StringComparer.OrdinalIgnoreCase));

var scanner = new FileScanner(new FileProbe());

using var cts = new CancellationTokenSource();

Task<ScanSummary> scanTask = scanner.ScanAsync(options, cts.Token);

Console.WriteLine();
Console.WriteLine("Сканирование...");
Console.WriteLine("Нажмите Enter для отмены.");

// Фоновая задача для реакции на Enter — не блокирует основной поток.
_ = Task.Run(() =>
{
    Console.ReadLine();
    if (!cts.IsCancellationRequested)
        cts.Cancel();
});

try
{
    ScanSummary summary = await scanTask;

    Console.WriteLine();
    Console.WriteLine("Найденные файлы:");
    Console.WriteLine();

    for (int i = 0; i < summary.Results.Count; i++)
    {
        ScanResult r = summary.Results[i];
        Console.WriteLine(
            $"[{i + 1}] {r.FileName,-20} {r.SizeBytes,10} байт " +
            $"(прочитано {r.BytesRead})");
    }

    Console.WriteLine();
    Console.WriteLine("Сканирование завершено.");
    Console.WriteLine();
    Console.WriteLine($"Всего найдено файлов: {summary.MatchedFiles}");
    Console.WriteLine($"Общий размер: {summary.TotalMatchedBytes} байт");
    Console.WriteLine($"Время: {summary.Duration}");

    if (summary.Errors.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"Ошибки ({summary.Errors.Count}):");
        foreach (string error in summary.Errors)
            Console.WriteLine($"  - {error}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("Сканирование отменено пользователем.");
    // Как и в сценарии — можно показать, сколько успели обработать.
    // Для этого нужен доступ к summary; но он не возвращается при отмене.
    // Вариант: считать счётчик в отдельном объекте-прогрессе.
}