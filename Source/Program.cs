using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;

namespace DsharpBot
{
	public class Program
	{
        private static readonly string DatabasePath = "database.json";

		private static List<DiscordGuild> GuildsToCheck = new List<DiscordGuild>();

		private static Dictionary<ulong, Guild> GuildsData;
		private static DiscordClient Discord;

		public static void Main()
		{
            GuildsData = LoadGuildsData();

            var minutes = 10f;
            var timer = new Timer(AddPointsToUsersInVoiceChannels, 0f, 0, (int)(1000f * 60f * minutes));

            new Program().MainAsync().GetAwaiter().GetResult();
        }

		public async Task MainAsync()
		{
			Discord = new DiscordClient(new DiscordConfiguration()
			{
				AutoReconnect = true,
				Token = Token.token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged
			});

			Discord.GuildAvailable += async (discordClient, guildEventArgs) =>
			{
				var currentGuild = guildEventArgs.Guild;
				GuildsToCheck.Add(currentGuild);

				foreach (var respondent in GuildsData[1].Respondents)
				{
					var guildMember = await currentGuild.GetMemberAsync(respondent.Key);

					Commands.AddMember(guildMember.Id, guildMember);
				}
				if (!GuildsData.ContainsKey(currentGuild.Id))
				{
					GuildsData.Add(currentGuild.Id, new Guild());
				}
				SaveGuildsData();
				await Task.CompletedTask;
			};

			Discord.UseInteractivity(new InteractivityConfiguration()
			{
				PollBehaviour = PollBehaviour.KeepEmojis,
				Timeout = TimeSpan.FromHours(30)
			});

			Discord.MessageCreated += async (discordClient, message) =>
			{
				var currentGuild = message.Guild;

				var messageToPoints = message.Message.Content.Length * message.Message.Content.Length / 30;

				if (!GuildsData.ContainsKey(1))
						GuildsData.Add(1, new Guild());

				if (currentGuild != null)
				{
					if (!GuildsData.ContainsKey(currentGuild.Id))
						GuildsData.Add(currentGuild.Id, new Guild());
					AddPointsToUser(currentGuild.Id, message.Author, messageToPoints);
				}
				
				SaveGuildsData();
				await Task.CompletedTask;
			};
			var commands = Discord.UseCommandsNext(new CommandsNextConfiguration()
			{
				StringPrefixes = new[] { "-" }
			}); commands.RegisterCommands<Commands>();

			await Discord.ConnectAsync();
			await Task.Delay(-1);
		}
		static void AddPointsToUser(ulong currentGuildId, DiscordUser discordUser, float value)
        {
			if (GuildsData[currentGuildId].Users.ContainsKey(discordUser.Id))
			{
				GuildsData[currentGuildId].Users[discordUser.Id].Expirience += value;
			}
			else
			{
				GuildsData[currentGuildId].Users.Add(discordUser.Id, new User()
				{
					Expirience = value
				});
			}
		}
		public static void AddPointsToUsersInVoiceChannels(object state)
		{
			foreach (var currentGuild in GuildsToCheck)
			{
				string data = string.Empty;
				foreach (var channel in currentGuild.Channels)
				{
					if (channel.Value.Type.ToString() == "Voice")
					{
						foreach (var user in channel.Value.Users)
						{
							AddPointsToUser(currentGuild.Id, user, new Random().Next(0,1200));
						}
					}
				}
				
			}
			SaveGuildsData();
		}
		public static Guild GetGuild(ulong currentGuildId) => GuildsData[currentGuildId];

		public static void SaveFormsData(Guild _guild)
        {
			GuildsData[1] = _guild;
			SaveGuildsData();
		}
		public static void SaveGuildsData()
		{
			File.WriteAllText(DatabasePath, JsonConvert.SerializeObject(GuildsData, Formatting.Indented));
		}

		internal static Dictionary<ulong, Guild> LoadGuildsData()
		{
			if (File.Exists(DatabasePath))
			{
				return JsonConvert.DeserializeObject<Dictionary<ulong, Guild>>(File.ReadAllText(DatabasePath));
			}
			else 
			{
				return new Dictionary<ulong, Guild>();
			}
			
		}	
	}
}