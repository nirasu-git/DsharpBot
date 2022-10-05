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

namespace haze
{
    public class Program
    {
        public static void Main()
        {
            Commands.IdTagsPairs = DataBase.Initialize();
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                AutoReconnect = true,
                Token = Token.token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });

            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromHours(24)
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration() { StringPrefixes = new[] { "-", ".", "/" } });
            commands.RegisterCommands<Commands>();

            discord.GuildAvailable += async (discordClient, guildEventArgs) =>
            {
                if (Commands.IdTagsPairs == null) return;

                ulong[] ids = Commands.IdTagsPairs.Keys.ToArray();

                var idsLength = ids.Length;

                for (var i = 0; i < idsLength; i++)
                {
                    var guildMember = await guildEventArgs.Guild.GetMemberAsync(ids[i]);

                    Commands.AddMember(guildMember.Id, guildMember);
                }
            };

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}