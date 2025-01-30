using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace EARDA.Message
{
    /// <summary>
    /// Discord message handler
    /// </summary>
    public static partial class Handler
    {
        /// <summary>
        ///     What to do when message is posted, will attempt to run bots main functionality of downloading the video and posting it as a video file
        /// </summary>
        /// <param name="messageArgs">User's message that triggered this to be called</param>
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
                Program.WriteLog(
                    LogLevel.Error,
                    ex.Message + string.Join("", ex.Data.Cast<System.Collections.DictionaryEntry>().Select(entry => $"\n{entry.Key}: {entry.Value}")),
                    new EventId(301, "Message Handler")
                );


                fileStream.Close();
                fileStream.Dispose();

                return;
            }

            fileStream.Close();
            fileStream.Dispose();

            await messageArgs.Message.ModifyEmbedSuppressionAsync(true);

            await FileManager.DeleteVideo(video.Path);
        }

        /// <summary>
        ///     Gets a link from message's content
        /// </summary>
        /// <param name="content">Message's content</param>
        /// <returns>A link</returns>
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

                    if (!(uriResult.Host == "www.youtube.com" || uriResult.Host == "youtu.be" || uriResult.Host == "youtube.com"))
                    {
                        continue;
                    }

                    return word;
                }

                return string.Empty;
            });
        }

        /// <summary>
        ///     What to do when a user deletes a message that originally made us reply. Will attempt to delete our reply message
        /// </summary>
        /// <param name="deletedMessage">Message that got deleted</param>
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

                    string[] youtubePatterns =
                    [
                        "https://www.youtube.com/watch?v=",
                        "https://youtube.com/watch?v=",
                        "https://youtu.be/",
                        "https://www.youtube.com/embed/",
                        "https://www.youtube.com/v/",
                        "https://www.youtube.com/shorts/",
                        "https://youtube.com/shorts/"
                    ];

                    foreach (string pattern in youtubePatterns)
                    {
                        if (content.Contains(pattern))
                        {
                            return true;
                        }
                    }

                    return false;
                });
            }
        }
    }
}