using System.Diagnostics;
using System.IO.Compression;

namespace MediathekArrDownloader.Utilities;

public static class FfmpegUtils
{
    public static string GetFfmpegArguments(string mp4Path, string subtitlePath, string mkvPath, bool subtitlesAvailable)
    {
        return subtitlesAvailable
            ? $"-y -i \"{mp4Path}\" -i \"{subtitlePath}\" -map 0:v -map 0:a -map 1 -c copy -map_metadata -1 " +
              $"-metadata:s:v:0 language=ger -metadata:s:a:0 language=ger -metadata:s:s:0 language=de \"{mkvPath}\""
            : $"-y -i \"{mp4Path}\" -map 0:v -map 0:a -c copy -map_metadata -1 " +
              $"-metadata:s:v:0 language=ger -metadata:s:a:0 language=ger \"{mkvPath}\"";
    }

    public static async Task<(bool Success, int ExitCode, string ErrorOutput)> StartFfmpegProcessAsync
        (string ffmpegPath, string mp4Path, string subtitlePath, string mkvPath, bool subtitlesAvailable, string title, ILogger logger)
    {
        var ffmpegArgs = GetFfmpegArguments(mp4Path, subtitlePath, mkvPath, subtitlesAvailable);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = ffmpegArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            logger.LogInformation("FFmpeg process started for {Title} with arguments: {Arguments}", title, ffmpegArgs);

            var standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            string ffmpegOutput = await standardErrorTask;

            return (process.ExitCode == 0, process.ExitCode, ffmpegOutput);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the FFmpeg process for {Title}.", title);
            return (false, -1, ex.Message);
        }
    }

    public static async Task EnsureFfmpegExistsAsync(string ffmpegPath, bool isWindows, ILogger logger, HttpClient httpClient)
    {
        if (!File.Exists(ffmpegPath))
        {
            logger.LogInformation("FFmpeg not found at path {FfmpegPath}. Starting download...", ffmpegPath);

            // URLs for downloading FFmpeg based on OS
            string ffmpegDownloadUrl = isWindows
                ? "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
                : "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz";

            var tempFilePath = Path.Combine(Path.GetTempPath(), isWindows ? "ffmpeg.zip" : "ffmpeg.tar.xz");
            var ffmpegDir = Path.Combine(Path.GetDirectoryName(ffmpegPath) ?? string.Empty);

            try
            {
                // Download FFmpeg file
                using (var response = await httpClient.GetAsync(ffmpegDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                    logger.LogInformation("FFmpeg downloaded to temporary path {TempFilePath}", tempFilePath);
                }

                Directory.CreateDirectory(ffmpegDir);
                logger.LogInformation("FFmpeg directory ensured at {FfmpegDir}", ffmpegDir);

                // Extract FFmpeg based on the OS
                if (isWindows)
                {
                    ZipFile.ExtractToDirectory(tempFilePath, ffmpegDir);
                    logger.LogInformation("FFmpeg extracted in Windows environment.");

                    // Move extracted ffmpeg.exe to the expected path
                    var extractedPath = Directory.GetFiles(ffmpegDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (extractedPath != null)
                    {
                        File.Move(extractedPath, ffmpegPath, true);
                        logger.LogInformation("FFmpeg moved to final path {FfmpegPath}", ffmpegPath);
                    }
                }
                else
                {
                    // Linux/macOS extraction
                    var extractionDir = Path.Combine(ffmpegDir, "extracted");
                    Directory.CreateDirectory(extractionDir);

                    logger.LogInformation("Starting extraction of FFmpeg in Linux environment.");

                    var tarProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "tar",
                            Arguments = $"-xf \"{tempFilePath}\" -C \"{extractionDir}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    tarProcess.Start();
                    await tarProcess.WaitForExitAsync();

                    if (tarProcess.ExitCode != 0)
                    {
                        string error = await tarProcess.StandardError.ReadToEndAsync();
                        logger.LogError("Error extracting FFmpeg: {Error}", error);
                        return;
                    }

                    logger.LogInformation("FFmpeg extraction completed.");

                    // Locate the extracted FFmpeg binary
                    var extractedPath = Directory.GetFiles(extractionDir, "ffmpeg", SearchOption.AllDirectories).FirstOrDefault();
                    if (extractedPath != null)
                    {
                        File.Move(extractedPath, ffmpegPath, true);
                        logger.LogInformation("FFmpeg moved to final path {FfmpegPath}", ffmpegPath);

                        // Ensure the binary is executable
                        var chmodProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "chmod",
                                Arguments = $"+x \"{ffmpegPath}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        chmodProcess.Start();
                        await chmodProcess.WaitForExitAsync();
                        logger.LogInformation("Executable permissions set for FFmpeg at {FfmpegPath}", ffmpegPath);
                    }
                    else
                    {
                        logger.LogError("FFmpeg binary not found after extraction.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during FFmpeg download or extraction.");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    logger.LogInformation("Temporary download file deleted at {TempFilePath}", tempFilePath);
                }

                var extractionDir = Path.Combine(ffmpegDir, "extracted");
                if (Directory.Exists(extractionDir))
                {
                    Directory.Delete(extractionDir, true);
                    logger.LogInformation("Temporary extraction directory deleted at {ExtractionDir}", extractionDir);
                }
            }

            logger.LogInformation("FFmpeg download and setup complete.");
        }
        else
        {
            logger.LogInformation("FFmpeg already exists at path {FfmpegPath}. Skipping download.", ffmpegPath);
        }
    }
}
