using DSharpPlus;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using YoutubeDLSharp;

namespace EARDA
{
    internal class Program
    {
        /// <summary>
        ///     Discord Client used by the bot overall
        /// </summary>
        public static DiscordClient? Client { get; private set; }

        /// <summary>
        ///     Main thread
        /// </summary>
        private static async Task Main()
        {
            Console.WriteLine($"Data path: {FileManager.Path}");
            Console.WriteLine();

            await BinaryDownloader();

            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault
            (
                token: tokens.token,
                intents: DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.GuildMessages
            );

            if (DebugStatus())
            {
                builder.SetLogLevel(LogLevel.Debug);
            }
            else
            {
                builder.SetLogLevel(LogLevel.Information);
            }

            builder.ConfigureEventHandlers
            (
                x =>
                x.HandleMessageCreated(async (client, args) =>
                {
                    await Message.Handler.MessagePosted(args);
                }).
                HandleMessageDeleted(async (client, args) =>
                {
                    if (args.Message.Author is null)
                    {
                        return;
                    }

                    if (args.Message.Author.IsBot)
                    {
                        return;
                    }

                    try
                    {
                        await Message.Handler.MessageDeleted(args.Message);
                    }
                    catch (Exception ex)
                    {
                        WriteLog(LogLevel.Error, ex.Message, new EventId(301, "Message Handler"));
                    }
                })
            );

            Console.WriteLine("Setting the bot to start...");
            Console.WriteLine();

            Client = builder.Build();

            WriteLog(LogLevel.Information, "Starting up...", LoggerEvents.Startup);

            if (DebugStatus())
            {
                WriteLog(LogLevel.Information, "Running in Debug Mode", LoggerEvents.Startup);
            }
            else
            {
                WriteLog(LogLevel.Information, "Running in Normal Mode", LoggerEvents.Startup);
            }

            WriteLog(LogLevel.Information, "Connecting to Discord...", LoggerEvents.Startup);

            await Client.ConnectAsync();

            WriteLog(LogLevel.Information, "Connected!", LoggerEvents.Startup);

            WriteLog(LogLevel.Information, "Bot is now operational and running!", LoggerEvents.Startup);

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    RunUpdate();

                    await Task.Delay(TimeSpan.FromDays(1));
                }
            });

            await Task.Delay(-1);
        }

        /// <summary>
        ///     Runs yt-dlp -U to check and update yt-dlp if needed
        /// </summary>
        private static void RunUpdate()
        {
            YoutubeDL ytdlp = new();

            WriteLog(LogLevel.Information, "Runnig yt-dlp updater", new EventId(302, "Updater"));
            WriteLog(LogLevel.Information, ytdlp.RunUpdate().Result, new EventId(302, "Updater"));
        }

        /// <summary>
        ///     Checks if bot is running in debug mode
        /// </summary>
        /// <returns>
        ///     Return <c>true</c> if running in debug mode, return <c>false</c> if running in normal mode
        /// </returns>
        public static bool DebugStatus()
        {
            bool debugState = false;

            if (Debugger.IsAttached)
            {
                debugState = true;
            }

            return debugState;
        }

        /// <summary>
        ///     Helper function to write log messages
        /// </summary>
        /// <param name="level">Logging level of the log message</param>
        /// <param name="message">Contents of the log message</param>
        /// <param name="eventId">Event ID of the event that is sending the log message</param>
        public static void WriteLog(LogLevel level, string message, EventId eventId)
        {
            if (Client is null)
            {
                Client?.Logger.LogWarning(LoggerEvents.Misc, "Client is null");

                return;
            }

            Client.Logger.Log(level, eventId, "{message}", message);
        }

        /// <summary>
        ///     Checks for missing binaries and downloads them if needed
        /// </summary>
        private static async Task BinaryDownloader()
        {
            Console.WriteLine("Checking precense of required binaries...");
            Console.WriteLine();

            if (!File.Exists("ffmpeg.exe") && !File.Exists("ffmpeg"))
            {
                Console.WriteLine("Couldn't find ffmpeg! Downloading...");

                await Utils.DownloadFFmpeg();

                Console.WriteLine("ffmpeg downloaded!");
                Console.WriteLine();
            }

            if (!File.Exists("yt-dlp.exe") && !File.Exists("yt-dlp"))
            {
                Console.WriteLine("Couldn't find yt-dlp! Downloading...");

                await Utils.DownloadYtDlp();

                Console.WriteLine("yt-dlp downloaded!");
                Console.WriteLine();
            }

            if (!File.Exists("ffprobe.exe") && !File.Exists("ffprobe"))
            {
                Console.WriteLine("Couldn't find ffprobe! Downloading...");

                await Utils.DownloadFFprobe();

                Console.WriteLine("ffprobe downloaded!");
                Console.WriteLine();
            }

            Console.WriteLine("Binaries checked!");
            Console.WriteLine();
        }
    }
}
