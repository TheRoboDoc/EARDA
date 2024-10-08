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
            Console.WriteLine("Checking precense of required binaries...");
            Console.WriteLine();

            if (!File.Exists("ffmpeg.exe") && !File.Exists("ffmpeg"))
            {
                Console.WriteLine("Couldn't find ffmpeg! Downloading...");

                await YoutubeDLSharp.Utils.DownloadFFmpeg();

                Console.WriteLine("ffmpeg downloaded!");
                Console.WriteLine();
            }

            if (!File.Exists("yt-dlp.exe") && !File.Exists("yt-dlp"))
            {
                Console.WriteLine("Couldn't find yt-dlp! Downloading...");

                await YoutubeDLSharp.Utils.DownloadYtDlp();

                Console.WriteLine("yt-dlp downloaded!");
                Console.WriteLine();
            }

            if (!File.Exists("ffprobe.exe") && !File.Exists("ffprobe"))
            {
                Console.WriteLine("Couldn't find ffprobe! Downloading...");

                await YoutubeDLSharp.Utils.DownloadFFprobe();

                Console.WriteLine("ffprobe downloaded!");
                Console.WriteLine();
            }

            Console.WriteLine("Binaries checked!");
            Console.WriteLine();
        }
    }
}
