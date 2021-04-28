using System;﻿
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Newtonsoft.Json;


namespace DsharpBot
{
	public class Program
	{
		private static string databasePath;

		public static Dictionary<ulong, Dictionary<ulong, float>> currentGuildUsersPointsPairs;
		public static List<DiscordGuild> guilds = new List<DiscordGuild>();

		public static void Main(string[] args)
		{
			databasePath = Environment.CurrentDirectory;
			
			currentGuildUsersPointsPairs = GetLoadPointsDataBase();

			var timer = new Timer(CheckActiveUsersInVoiceChannel, 0, 0, 1000 * 60 * 5); //5 minutes

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

			discord.GuildAvailable += async (discordClient, guildEventArgs) =>
			{
				var currentGuild = guildEventArgs.Guild;
				guilds.Add(currentGuild);

				if (currentGuildUsersPointsPairs.TryGetValue(currentGuild.Id, out var currentGuildKeyPairs)) await Task.CompletedTask;
				else currentGuildUsersPointsPairs.Add(currentGuild.Id, new Dictionary<ulong, float>()); await Task.CompletedTask;
			};
			discord.MessageCreated += async (discordClient, message) =>
			{
				var currentGuild = message.Guild;

				var messageToPoints = message.Message.Content.Length * message.Message.Content.Length / 30;

				if (currentGuildUsersPointsPairs[currentGuild.Id].TryGetValue(message.Author.Id, out float authorPoints))
					currentGuildUsersPointsPairs[currentGuild.Id][message.Author.Id] = authorPoints + messageToPoints;

				else currentGuildUsersPointsPairs[currentGuild.Id].Add(message.Author.Id, messageToPoints);

				string json = JsonConvert.SerializeObject(currentGuildUsersPointsPairs, Formatting.Indented);

				File.WriteAllText(databasePath, json);

				await Task.CompletedTask;
			};
			
			var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
			{
				StringPrefixes = new[] { "." }
			}); commands.RegisterCommands<Commands>();

			await discord.ConnectAsync();
			await Task.Delay(-1);
		}

		static void CheckActiveUsersInVoiceChannel(object state)
		{
			var a = AddPointsToActiveUsersInVoiceChannelsAsync();
		}
		public static async Task<int> AddPointsToActiveUsersInVoiceChannelsAsync()
		{
			await Task.Delay(10000);
			foreach (var currentGuild in guilds)
			{
				string data = string.Empty;
				DiscordChannel channelForBot = null;
				foreach (var channel in currentGuild.Channels)
				{
					if (channel.Value.Type.ToString() == "Voice")
					{
						if (channel.Value.Type == 0)
						{
							channelForBot = channel.Value;
						}
						foreach (var user in channel.Value.Users)
						{

							if (currentGuildUsersPointsPairs[currentGuild.Id].TryGetValue(user.Id, out float authorPoints))
							{
								currentGuildUsersPointsPairs[currentGuild.Id][user.Id] = authorPoints + 1000;
							}
							else
							{
								currentGuildUsersPointsPairs[currentGuild.Id].Add(user.Id, 1000);
							}
							data += $"{user.Mention}, you are rewarded for being in the voice channel, your score is {currentGuildUsersPointsPairs[currentGuild.Id][user.Id]}\n";
						}
					}
				}
				string json = JsonConvert.SerializeObject(currentGuildUsersPointsPairs, Formatting.Indented);
				File.WriteAllText(databasePath, json);
				if (data == string.Empty && channelForBot != null) await Task.CompletedTask;
				else await channelForBot.SendMessageAsync(data);
			}
			return 1;
		}
		public static Dictionary<ulong, float> GetKeys(DiscordGuild currentGuild) => currentGuildUsersPointsPairs[currentGuild.Id];

		internal static Dictionary<ulong, Dictionary<ulong, float>> GetLoadPointsDataBase()
		{
			
			if (File.Exists(databasePath)) return JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<ulong, float>>>(File.ReadAllText(databasePath));
			else return new Dictionary<ulong, Dictionary<ulong, float>>(); 
		}
	}
}