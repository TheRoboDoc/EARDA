﻿using DSharpPlus;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EARDA
{
    internal class Program
    {
        public static DiscordClient? Client { get; private set; }

        private static async Task Main()
        {
            string a = FileManager.Path;

            Console.WriteLine(a);

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
                })
            );

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

            await Task.Delay(-1);
        }

        public static bool DebugStatus()
        {
            bool debugState = false;

            if (Debugger.IsAttached)
            {
                debugState = true;
            }

            return debugState;
        }

        public static void WriteLog(LogLevel level, string message, EventId eventId)
        {
            if (Client is null)
            {
                Client?.Logger.LogWarning(LoggerEvents.Misc, "Client is null");

                return;
            }

            Client.Logger.Log(level, eventId, "{message}", message);
        }

        private static async Task BinaryDownloader()
        {
            Console.WriteLine("Checking precense of binaries...");
            Console.WriteLine();

            if (!File.Exists("ffmpeg.exe"))
            {
                Console.WriteLine("Couldn't find ffmpeg.exe! Downloading...");

                await YoutubeDLSharp.Utils.DownloadFFmpeg();

                Console.WriteLine("ffmpeg.exe downloaded!");
                Console.WriteLine();
            }

            if (!File.Exists("yt-dlp.exe"))
            {
                Console.WriteLine("Couldn't find yt-dlp.exe! Downloading...");

                await YoutubeDLSharp.Utils.DownloadYtDlp();

                Console.WriteLine("yt-dlp.exe downloaded!");
                Console.WriteLine();
            }

            if (!File.Exists("ffprobe.exe"))
            {
                Console.WriteLine("Couldn't find ffprobe.exe! Downloading...");

                await YoutubeDLSharp.Utils.DownloadFFprobe();

                Console.WriteLine("ffprobe.exe downloaded!");
                Console.WriteLine();
            }

            Console.WriteLine("Binaries checked!");
            Console.WriteLine();
        }
    }
}
