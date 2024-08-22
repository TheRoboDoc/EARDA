using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace EARDA.Message
{
    public static partial class Handler
    {
        public static async Task MessagePosted(MessageCreatedEventArgs messageArgs)
        {
            if (messageArgs.Author.IsBot)
            {
                return;
            }

            if (!await Analyzer.IsYouTubeLink(messageArgs.Message.Content))
            {
                return;
            }

            DiscordMessageBuilder builder = new();

            FileManager.Video video = await FileManager.DownloadVideo(messageArgs.Message.Id, messageArgs.Message.Content);

            string textResponse = $"**{video.Uploader}**\n# [{video.Title}](<{video.Url}>)";

            FileStream fileStream = File.OpenRead(video.Path);

            builder.AddFile(fileStream);



            builder.Content = textResponse;

            try
            {
                await messageArgs.Message.RespondAsync(builder);
            }
            catch
            {
                Program.WriteLog(LogLevel.Error, "Failed to upload", new EventId(301, "Message Handler"));
            }

            await FileManager.DeleteVideo(video.Path);

            try
            {
                await messageArgs.Message.ModifyEmbedSuppressionAsync(true);
            }
            catch (UnauthorizedException)
            {
                textResponse += $"\n-# {Program.Client?.CurrentUser.Mention} doesn't have **Manage Messages** permission to manage duplicate embeds";
            }
        }

        public static async Task MessageDeleted(DiscordMessage deletedMessage)
        {
            DiscordChannel? channel = deletedMessage.Channel;

            DiscordMessage? message = channel?.GetMessagesAfterAsync(deletedMessage.Id).ToBlockingEnumerable().ToList().First();

            DiscordUser? currentUser = Program.Client?.CurrentUser;
            DiscordUser? author = message?.Author;

            if (currentUser is null || message is null || author is null)
            {
                return;
            }

            if (author == currentUser)
            {
                await message.DeleteAsync();
            }
        }

        private static partial class Analyzer
        {
            public static async Task<bool> IsYouTubeLink(string content)
            {
                return await Task.Run(() =>
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        return false;
                    }

                    return (content.Contains("https://www.youtube.com") && content.Contains("/watch?v=")) ||
                            content.Contains("https://youtu.be/");
                });
            }
        }
    }
}