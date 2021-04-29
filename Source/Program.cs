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
		private static string databasePath = "G:/DsharpBot/database.json";

		public static List<DiscordGuild> guildsToCheck = new List<DiscordGuild>();

		public static Dictionary<ulong, Guild> guildsData;
		public static string serializedData;
		static DiscordClient discord;

		public static void Main(string[] args)
		{
			guildsData = GetLoadPointsDataBase();

            var minutes = 5f;
			var timer = new Timer(CheckActiveUsersInVoiceChannel, 0f, 0, (int)(1000f * 60f * minutes)); //milis to minutes

			new Program().MainAsync().GetAwaiter().GetResult();
		}

		public async Task MainAsync()
		{
			discord = new DiscordClient(new DiscordConfiguration()
			{
				AutoReconnect = true,
				Token = Token.token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged
			});
			
			discord.GuildAvailable += async (discordClient, guildEventArgs) =>
			{
				var currentGuild = guildEventArgs.Guild;
				guildsToCheck.Add(currentGuild);


				if (guildsData.ContainsKey(currentGuild.Id)) await Task.CompletedTask;
				else
				{
					guildsData.Add(currentGuild.Id, new Guild());
				}
				serializedData = JsonConvert.SerializeObject(guildsData);
			};
			discord.UseInteractivity(new InteractivityConfiguration()
			{
				PollBehaviour = PollBehaviour.KeepEmojis,
				Timeout = TimeSpan.FromSeconds(10)
			});
			discord.MessageCreated += async (discordClient, message) =>
			{
				var currentGuild = message.Guild;
				var messageToPoints = message.Message.Content.Length * message.Message.Content.Length / 30;

				
				AddPointsToUser(currentGuild.Id, message.Author, messageToPoints);


				await Task.CompletedTask;
			};
			var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
			{
				StringPrefixes = new[] { "." }
			}); commands.RegisterCommands<Commands>();

			await discord.ConnectAsync();
			await Task.Delay(-1);
		}
		static void AddPointsToUser(ulong currentGuildId, DiscordUser discordUser, float value)
        {
			var user = new User()
			{
				id = discordUser.Id,
				expirience = value
			};

			if (guildsData[currentGuildId].users == null) guildsData[currentGuildId].users = new Dictionary<ulong, User>();

			if (guildsData[currentGuildId].users.ContainsKey(user.id))
			{
				guildsData[currentGuildId].users[user.id].expirience += value;
			}
			else
				guildsData[currentGuildId].users.Add(user.id, user);

		}
		static void CheckActiveUsersInVoiceChannel(object state)
		{
			var a = AddPointsToActiveUsersInVoiceChannelsAsync();
		}
		public static async Task<int> AddPointsToActiveUsersInVoiceChannelsAsync()
		{
			foreach (var currentGuild in guildsToCheck)
			{
				string data = string.Empty;
				DiscordChannel channelForBot = null;
				foreach (var channel in currentGuild.Channels)
				{
					if (channel.Value.Name.ToString() == "основной" && channelForBot == null)
					{
						channelForBot = channel.Value;
					}
					if (channel.Value.Type.ToString() == "Voice")
					{
						foreach (var user in channel.Value.Users)
						{
							AddPointsToUser(currentGuild.Id, user, new Random().Next(800,1200));

							data += $"{user.DisplayName}, you are rewarded for being in the voice channel, your score is {guildsData[currentGuild.Id].users[user.Id].expirience}\n";
						}
					}
				}
				serializedData = JsonConvert.SerializeObject(guildsData, Formatting.Indented);

				File.WriteAllText(databasePath, serializedData);

				if (data == string.Empty || channelForBot == null) await Task.CompletedTask;
				else await channelForBot.SendMessageAsync(data);
			}
			return 1;
		}
		public static Guild GetGuild(ulong currentGuildId) => guildsData[currentGuildId];

		internal static Dictionary<ulong, Guild> GetLoadPointsDataBase()
		{
			if (File.Exists(databasePath) && File.ReadAllText(databasePath)!= null) return JsonConvert.DeserializeObject<Dictionary<ulong, Guild>>(File.ReadAllText(databasePath));
			else return new Dictionary<ulong, Guild>();
			
		}	
	}
}