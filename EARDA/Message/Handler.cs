﻿using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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

            string? link = await GetLinkFromMessage(messageArgs.Message.Content);

            if (link == string.Empty)
            {
                return;
            }

            DiscordMessageBuilder builder = new();

            FileManager.Video? downloadResult = await FileManager.DownloadVideo(messageArgs.Message.Id, link);

            if (downloadResult == null)
            {
                return;
            }

            FileManager.Video video = downloadResult.Value;

            if (!FileManager.FileSizeCheck(new FileInfo(video.Path)))
            {
                await FileManager.DeleteVideo(video.Path);

                return;
            }

            string textResponse = $"# [{video.Title}](<{video.Url}>) \n**{video.Uploader}**";

            FileStream fileStream = File.OpenRead(video.Path);

            builder.AddFile(fileStream);

            builder.Content = textResponse;

            try
            {
                await messageArgs.Message.RespondAsync(builder);
            }
            catch (Exception ex)
            {
                Program.WriteLog(LogLevel.Error, ex.Message, new EventId(301, "Message Handler"));

                fileStream.Close();
                fileStream.Dispose();

                return;
            }

            fileStream.Close();
            fileStream.Dispose();

            await messageArgs.Message.ModifyEmbedSuppressionAsync(true);

            await FileManager.DeleteVideo(video.Path);
        }

        public static async Task<string> GetLinkFromMessage(string content)
        {
            return await Task.Run(() =>
            {
                string[] words = content.Split([' ', '\n']);

                foreach (string word in words)
                {
                    if (!Uri.TryCreate(word, UriKind.Absolute, out Uri? uriResult))
                    {
                        continue;
                    }

                    if (!(uriResult.Host == "www.youtube.com" || uriResult.Host == "youtu.be"))
                    {
                        continue;
                    }

                    return word;
                }

                return string.Empty;
            });
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