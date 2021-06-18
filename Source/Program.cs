using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace haze.Source
{
    public class Program
    {
        private static List<DiscordGuild> GuildsToCheck = new List<DiscordGuild>();

        private static DiscordClient Discord;

        public static void Main()
        {
            Commands.IdTagsPairs = DataBase.Initialize();
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            Discord =
                new DiscordClient(new DiscordConfiguration()
                {
                    AutoReconnect = true,
                    Token = Token.token,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.AllUnprivileged
                });

            Discord
                .UseInteractivity(new InteractivityConfiguration()
                {
                    PollBehaviour = PollBehaviour.KeepEmojis,
                    Timeout = TimeSpan.FromHours(24)
                });
            var commands =
                Discord
                    .UseCommandsNext(new CommandsNextConfiguration()
                    { StringPrefixes = new[] { "-", ".", "/" } });
            commands.RegisterCommands<Commands>();

            Discord.GuildAvailable += async (discordClient, guildEventArgs) =>
            {
                var currentGuild = guildEventArgs.Guild;
                GuildsToCheck.Add(currentGuild);
                if (Commands.IdTagsPairs != null)
                {
                    ulong[] ids = Commands.IdTagsPairs.Keys.ToArray();

                    foreach (var id in ids)
                    {
                        var guildMember = await currentGuild.GetMemberAsync(id);

                        Commands.AddMember(guildMember.Id, guildMember);
                    }
                }
            };
            await Discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}