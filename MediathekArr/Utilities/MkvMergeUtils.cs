using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediathekArr.Utilities;

public static class MkvMergeUtils
{
    private static string GetMkvMergeArguments(string mp4Path, string subtitlePath, string mkvPath, bool subtitlesAvailable)
    {
        return subtitlesAvailable
            ? $"-o \"{mkvPath}\" --language 0:ger --language 1:ger \"{mp4Path}\" --language 0:ger --default-track 0:0 \"{subtitlePath}\""
            : $"-o \"{mkvPath}\" --language 0:ger --language 1:ger \"{mp4Path}\"";
    }

    public static async Task<(bool Success, int ExitCode, string ErrorOutput)> StartMkvmergeProcessAsync
        (string mkvmergePath, string mp4Path, string subtitlePath, string mkvPath, bool subtitlesAvailable, string title, ILogger logger)
    {
        var mkvmergeArgs = GetMkvMergeArguments(mp4Path, subtitlePath, mkvPath, subtitlesAvailable);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = mkvmergePath,
                Arguments = mkvmergeArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            logger.LogDebug("mkvmerge path: {MkvmergePath}", mkvmergePath);
            logger.LogDebug("Arguments: {Arguments}", mkvmergeArgs);
            logger.LogDebug("MP4 Path: {Mp4Path}, Subtitle Path: {SubtitlePath}, MKV Path: {MkvPath}, Subtitles Available: {SubtitlesAvailable}", mp4Path, subtitlePath, mkvPath, subtitlesAvailable);

            process.Start();
            logger.LogInformation("mkvmerge process started for {Title} with arguments: {Arguments}", title, mkvmergeArgs);

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            string mkvmergeOutput = await standardOutputTask;
            string mkvmergeError = await standardErrorTask;

            logger.LogInformation("mkvmerge process completed for {Title}. Exit code: {ExitCode}", title, process.ExitCode);

            if (!string.IsNullOrWhiteSpace(mkvmergeOutput))
            {
                logger.LogDebug("mkvmerge process standard output: {Output}", mkvmergeOutput);
            }

            if (!string.IsNullOrWhiteSpace(mkvmergeError))
            {
                logger.LogError("mkvmerge process error output: {Error}", mkvmergeError);
            }

            if (process.ExitCode != 0)
            {
                logger.LogError("mkvmerge conversion failed for {Title}. Exit code: {ExitCode}.", title, process.ExitCode);
                return (false, process.ExitCode, string.IsNullOrWhiteSpace(mkvmergeError) ? "Unknown error" : mkvmergeError);
            }

            return (true, process.ExitCode, mkvmergeError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the mkvmerge process for {Title}.", title);
            return (false, -1, ex.Message);
        }
    }



    public static async Task EnsureMkvMergeExistsAsync(string mkvmergePath, ILogger logger, HttpClient httpClient)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        if (!isWindows)
        {
            // Check if mkvmerge is available in PATH
            var whichProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "mkvmerge",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            whichProcess.Start();
            string? mkvmergeInPath = await whichProcess.StandardOutput.ReadToEndAsync();
            await whichProcess.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(mkvmergeInPath))
            {
                logger.LogInformation("mkvmerge found in PATH at {MkvmergeInPath}.", mkvmergeInPath.Trim());
            }
            else
            {
                logger.LogError("mkvmerge is not found at the specified path {MkvmergePath} and is not available in PATH.", mkvmergePath);
                logger.LogError("Please ensure mkvmerge is installed and accessible via PATH or located next to MediathekArrDownloader.");
            }

            return;
        }

        if (!File.Exists(mkvmergePath))
        {
            logger.LogInformation("mkvmerge not found at {mkvmergePath}. Attempting to download for Windows environment...", mkvmergePath);

            string mkvmergeDownloadUrl = "https://mkvtoolnix.download/windows/releases/89.0/mkvtoolnix-64-bit-89.0.7z";
            var tempFilePath = Path.Combine(Path.GetTempPath(), "mkvtoolnix.7z");
            var mkvmergeDir = Path.Combine(Path.GetDirectoryName(mkvmergePath) ?? string.Empty);

            try
            {
                // Download mkvmerge file
                using (var response = await httpClient.GetAsync(mkvmergeDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                    logger.LogInformation("mkvmerge downloaded to temporary path {TempFilePath}", tempFilePath);
                }

                Directory.CreateDirectory(mkvmergeDir);
                logger.LogInformation("mkvmerge directory ensured at {MkvmergeDir}", mkvmergeDir);

                // Download and extract using 7zr.exe
                string sevenZipUrl = "https://7-zip.org/a/7zr.exe";
                string sevenZipPath = Path.Combine(Path.GetTempPath(), "7zr.exe");

                using (var response = await httpClient.GetAsync(sevenZipUrl, HttpCompletionOption.ResponseHeadersRead))
                using (var fileStream = new FileStream(sevenZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                    logger.LogInformation("7zr.exe downloaded to {SevenZipPath}", sevenZipPath);
                }

                var sevenZipProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = sevenZipPath,
                        Arguments = $"x \"{tempFilePath}\" -o\"{mkvmergeDir}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                sevenZipProcess.Start();
                await sevenZipProcess.WaitForExitAsync();

                if (sevenZipProcess.ExitCode != 0)
                {
                    string error = await sevenZipProcess.StandardError.ReadToEndAsync();
                    logger.LogError("Error extracting mkvmerge with 7zr.exe: {Error}", error);
                    return;
                }

                logger.LogInformation("mkvmerge extracted in Windows environment.");

                var extractedPath = Directory.GetFiles(mkvmergeDir, "mkvmerge.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (extractedPath != null)
                {
                    File.Move(extractedPath, mkvmergePath, true);
                    logger.LogInformation("mkvmerge moved to final path {MkvmergePath}", mkvmergePath);
                }

                // Clean up
                if (File.Exists(sevenZipPath)) File.Delete(sevenZipPath);
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during mkvmerge download or extraction.");
            }
        }
        else
        {
            logger.LogInformation("mkvmerge already exists at path {MkvmergePath}. Skipping download.", mkvmergePath);
        }
    }

}
