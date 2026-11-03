using System.Diagnostics;
using AsyncFileScanner.Models;

namespace AsyncFileScanner.Services;

/**
 * В этом примере есть несколько важных особенностей.
 * 1. Перечисление файлов выполняется через `Directory.EnumerateFiles`. Это синхронный API.
 *    Он не становится асинхронным оттого, что находится внутри `async` метода.
 * 2. Асинхронная операция здесь — чтение файла через `ReadHeaderAsync`.
 * 3. Ошибки одного файла сохраняются в список. Они не приводят к остановке всего сканирования.
 * 4. Отмена проверяется перед обработкой каждого следующего файла, а также передаётся
 *    непосредственно в `ReadAsync`.
 * Важно: отмена не может гарантированно мгновенно прервать системный вызов перечисления
 * файлов. В данном варианте она проверяется между итерациями.
 */
public sealed class FileScanner
{
    private readonly FileProbe _probe;

    public FileScanner(FileProbe probe)
    {
        _probe = probe;
    }

    public async Task<ScanSummary> ScanAsync(
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        ValidateOptions(options);

        var stopwatch = Stopwatch.StartNew();
        var results = new List<ScanResult>();
        var errors = new List<string>();

        int scannedFiles = 0;
        long totalBytes = 0;

        try
        {
            // Синхронное перечисление, но с проверкой отмены на каждой итерации.
            foreach (string path in Directory.EnumerateFiles(
                         options.RootDirectory,
                         "*",
                         SearchOption.AllDirectories))
            {
                // Проверяем отмену перед обработкой каждого файла.
                cancellationToken.ThrowIfCancellationRequested();

                string ext = Path.GetExtension(path);
                if (!options.Extensions.Contains(ext))
                    continue;

                scannedFiles++;

                try
                {
                    var info = new FileInfo(path);

                    int bytesRead = await _probe.ReadHeaderAsync(
                        path,
                        cancellationToken);

                    long size = info.Length;
                    totalBytes += size;

                    results.Add(new ScanResult(
                        FileName: info.Name,
                        FullPath: path,
                        SizeBytes: size,
                        BytesRead: bytesRead));
                }
                catch (OperationCanceledException)
                {
                    // Отмену не глотаем — пробрасываем наверх.
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    errors.Add($"{path}: нет доступа ({ex.Message})");
                }
                catch (IOException ex)
                {
                    errors.Add($"{path}: ошибка ввода-вывода ({ex.Message})");
                }
                catch (Exception ex)
                {
                    // На случай прочих неожиданных ошибок — не роняем весь скан.
                    errors.Add($"{path}: {ex.GetType().Name} — {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Если отменили — просто фиксируем время и возвращаем то, что успели.
            stopwatch.Stop();
            throw;
        }
        finally
        {
            if (stopwatch.IsRunning)
                stopwatch.Stop();
        }

        return new ScanSummary(
            scannedFiles,
            results.Count,
            totalBytes,
            stopwatch.Elapsed,
            results,
            errors);
    }

    private static void ValidateOptions(ScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.RootDirectory))
            throw new ArgumentException(
                "Не задан путь к директории.", nameof(options));

        if (!Directory.Exists(options.RootDirectory))
            throw new DirectoryNotFoundException(
                $"Директория не найдена: {options.RootDirectory}");

        if (options.Extensions is null || options.Extensions.Count == 0)
            throw new ArgumentException(
                "Не задан ни одного расширения.", nameof(options));
    }
}