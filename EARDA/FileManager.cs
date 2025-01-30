using Microsoft.Extensions.Logging;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace EARDA
{
    /// <summary>
    ///     Handles file manipulation
    /// </summary>
    public static class FileManager
    {
        /// <summary>
        ///     Path where all downloaded data is stored
        /// </summary>
        public static readonly string Path = $"{AppDomain.CurrentDomain.BaseDirectory}Data";

        /// <summary>
        ///     Structure for handling the video
        /// </summary>
        public struct Video
        {
            public string Title;
            public string Uploader;
            public string Url;
            public string Path;

            public ulong id;
        }

        /// <summary>
        ///     Downloads a video from the given URL
        /// </summary>
        /// <param name="id">ID of the message</param>
        /// <param name="url">URL of the video to download</param>
        /// <returns>
        ///     Video <c>struct</c> or null if download failed
        /// </returns>
        public static async Task<Video?> DownloadVideo(ulong id, string url)
        {
            YoutubeDL ytdlp = new()
            {
                OutputFolder = Path,
                OutputFileTemplate = $"{id}.%(ext)s",
            };

            RunResult<VideoData> result = await ytdlp.RunVideoDataFetch(url);

            VideoData video = result.Data;

            string title = video.Title;
            string uploader = video.Uploader;

            double downloaedProgress = 0.0f;
            double lastLoggedProgress = 0.0f;

            Progress<DownloadProgress> progress = new(p =>
            {
                downloaedProgress = p.Progress;

                if (Math.Floor(downloaedProgress * 10) / 10 >= lastLoggedProgress + 0.1)
                {
                    lastLoggedProgress = Math.Floor(downloaedProgress * 10) / 10;

                    Program.WriteLog(LogLevel.Information, $"Downloading '{title}'... Progress: {downloaedProgress:P1}", new EventId(201, "File Manager"));
                }
            });

            OptionSet options = new()
            {
                Format = "bestvideo[width>=360][ext=webm][filesize_approx<7MiB]+bestaudio[ext=webm][filesize_approx<3MiB]/bestvideo[width>=360][ext=mp4][filesize_approx<7MiB]+bestaudio[ext=m4a][filesize_approx<3MiB]/best[width>=360][ext=mp4][filesize<10MiB]",
                NoContinue = true,
            };

            RunResult<string> downloadResult;

            try
            {
                downloadResult = await ytdlp.RunVideoDownload(url, progress: progress, overrideOptions: options);
            }
            catch (Exception ex)
            {
                Program.WriteLog(LogLevel.Warning, ex.Message, new EventId(201, "File Manager"));

                return null;
            }

            string path = downloadResult.Data;

            return new Video
            {
                Title = title,
                Uploader = uploader,
                Url = url,
                Path = path,
                id = id
            };
        }

        /// <summary>
        ///     Deletes a video
        /// </summary>
        /// <param name="path">Path to the video</param>
        public static async Task DeleteVideo(string path)
        {
            await Task.Run(() =>
            {
                FileInfo fileInfo = new(path);

                try
                {
                    fileInfo.Delete();
                }
                catch (Exception e)
                {
                    Program.WriteLog(LogLevel.Error, $"{e.Message}", new EventId(201, "File Manager"));
                }
            });
        }

        /// <summary>
        ///     Checks if file's size is within Discord file size limits
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns>
        ///     <c>true</c> if file is under 10 MiB, <c>false</c> if file larger than 10 MiB
        /// </returns>
        public static bool FileSizeCheck(FileInfo fileInfo)
        {
            long fileSizeInBytes = fileInfo.Length;
            long fileSizeInMegabytes = fileSizeInBytes / (1024 * 1024);

            if (fileSizeInMegabytes < 10) // 10 MB in bytes
            {
                //The file is under 10 MB.
                return true;
            }
            else
            {
                //The file is 10 MB or larger.
                return false;
            }
        }
    }
}
