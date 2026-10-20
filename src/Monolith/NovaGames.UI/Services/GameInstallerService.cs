using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NovaGames.UI.Services
{
    public class GameInstallerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _libraryBasePath = @"C:\NovaGames\Library";

        public GameInstallerService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromHours(2); // Для великих ігор

            if (!Directory.Exists(_libraryBasePath))
            {
                Directory.CreateDirectory(_libraryBasePath);
            }
        }

        // Очищаємо назву від недопустимих символів для папки
        private string GetSafeFolderName(string title)
        {
            return string.Join("_", title.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        }

        // 1. ПЕРЕВІРКА СТАТУСУ ВСТАНОВЛЕННЯ
        public bool IsGameInstalled(string gameTitle)
        {
            string installPath = Path.Combine(_libraryBasePath, GetSafeFolderName(gameTitle));
            if (!Directory.Exists(installPath)) return false;

            // Якщо папка є і в ній є хоча б один .exe (не враховуючи CrashHandler)
            var exeFiles = Directory.GetFiles(installPath, "*.exe", SearchOption.AllDirectories)
                                    .Where(f => !f.EndsWith("UnityCrashHandler64.exe", StringComparison.OrdinalIgnoreCase));

            return exeFiles.Any();
        }

        // 2. ВАЖКЕ ЗАВАНТАЖЕННЯ ТА РОЗПАКУВАННЯ
        public async Task DownloadAndInstallAsync(string zipUrl, string gameTitle, IProgress<double> progress, IProgress<string> statusText)
        {
            if (string.IsNullOrEmpty(zipUrl)) throw new Exception("URL для завантаження порожній. Архів не знайдено на сервері.");

            string folderName = GetSafeFolderName(gameTitle);
            string installPath = Path.Combine(_libraryBasePath, folderName);
            string tempZipPath = Path.Combine(_libraryBasePath, $"{folderName}_temp.zip");

            try
            {
                statusText?.Report("З'єднання з сервером...");

                // Запитуємо тільки заголовки, щоб дізнатися загальний розмір
                using var response = await _httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var stream = await response.Content.ReadAsStreamAsync();

                // Читаємо потік частинами (по 8 КБ), щоб оновлювати прогрес
                using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                statusText?.Report("Завантаження архіву...");

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes != -1)
                    {
                        double percentage = Math.Round((double)totalRead / totalBytes * 100, 1);
                        progress?.Report(percentage);
                    }
                }

                // Закриваємо потік файлу, щоб звільнити його для розпакування
                fileStream.Close();

                statusText?.Report("Розпакування файлів...");
                progress?.Report(100);

                // Очищаємо стару папку, якщо вона була
                if (Directory.Exists(installPath)) Directory.Delete(installPath, true);
                Directory.CreateDirectory(installPath);

                // Розпаковуємо асинхронно через Task.Run, щоб не блокувати UI потік WPF
                await Task.Run(() => ZipFile.ExtractToDirectory(tempZipPath, installPath, overwriteFiles: true));

            }
            finally
            {
                // Прибираємо за собою сміття (тимчасовий .zip)
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
            }
        }

        // 3. ЗАПУСК ГРИ ТА ВІДСТЕЖЕННЯ ПРОЦЕСУ
        public async Task LaunchGameAsync(string gameTitle)
        {
            string installPath = Path.Combine(_libraryBasePath, GetSafeFolderName(gameTitle));

            // Шукаємо головний .exe файл
            var exeFiles = Directory.GetFiles(installPath, "*.exe", SearchOption.AllDirectories)
                                    .Where(f => !f.EndsWith("UnityCrashHandler64.exe", StringComparison.OrdinalIgnoreCase))
                                    .ToList();

            if (!exeFiles.Any()) throw new Exception("Виконуваний файл (.exe) не знайдено в папці з грою.");

            // Беремо перший знайдений .exe (для MVP цього достатньо)
            string targetExe = exeFiles.First();

            var processInfo = new ProcessStartInfo
            {
                FileName = targetExe,
                WorkingDirectory = Path.GetDirectoryName(targetExe),
                UseShellExecute = true
            };

            using var process = Process.Start(processInfo);

            if (process != null)
            {
                // Асинхронно чекаємо, поки користувач не закриє гру
                await process.WaitForExitAsync();
            }
        }

        public void UninstallGame(string gameTitle)
        {
            string installPath = Path.Combine(_libraryBasePath, GetSafeFolderName(gameTitle));
            if (Directory.Exists(installPath))
            {
                Directory.Delete(installPath, true); // true = видалити всі вкладені файли і папки
            }
        }
    }
}