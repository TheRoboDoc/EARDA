using DSharpPlus;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EARDA
{
    internal class Program
    {
        public static DiscordClient? Client { get; private set; }

        private static async Task Main()
        {
            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault
            (
                token: tokens.token,
                intents: DiscordIntents.GuildMessages | DiscordIntents.MessageContents
            );

            if (DebugStatus())
            {
                builder.SetLogLevel(LogLevel.Debug);
            }
            else
            {
                builder.SetLogLevel(LogLevel.Information);
            }

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
    }
}
