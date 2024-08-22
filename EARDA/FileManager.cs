using Microsoft.Extensions.Logging;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace EARDA
{
    public static class FileManager
    {
        public static readonly string Path = $"{AppDomain.CurrentDomain.BaseDirectory}Data";

        public struct Video
        {
            public string Title;
            public string Uploader;
            public string Url;
            public string Path;

            public ulong id;
        }

        public static async Task<Video> DownloadVideo(ulong id, string url)
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

                    Program.WriteLog(LogLevel.Information, $"Downloading {title}... Progress: {downloaedProgress:P1}", new EventId(201, "File Manager"));
                }
            });

            RunResult<string> downloadResult = await ytdlp.RunVideoDownload(url, progress: progress);

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

        public static async Task DeleteVideo(string path)
        {
            await Task.Run(() =>
            {
                FileInfo fileInfo = new(path);

                try
                {
                    fileInfo.Delete();
                }
                catch
                {
                    Program.WriteLog(LogLevel.Error, $"Failed to delete video '{path}'", new EventId(201, "File Manager"));
                }
            });
        }
    }
}
