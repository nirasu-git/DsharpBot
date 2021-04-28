﻿using System;
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
		
		public static ulong[] adminID = { 766625771673354311 };
		private static string databasePath = "G:/DsharpBot/database.json";


		public static Dictionary<ulong, Dictionary<ulong, float>> currentGuildUsersPointsPairs;
		public static List<DiscordGuild> guilds;

		public static void Main(string[] args)
		{
			currentGuildUsersPointsPairs = LoadPointsDataBase();

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

			discord.GuildAvailable += async (s, e) =>
			{
				var currentGuild = e.Guild;
				guilds.Add(currentGuild);

				string json = JsonConvert.SerializeObject(currentGuildUsersPointsPairs, Formatting.Indented);
				File.WriteAllText(databasePath, json);

				Console.WriteLine("a");
				if (currentGuildUsersPointsPairs.TryGetValue(currentGuild.Id, out var currentGuildKeyPairs)) await Task.CompletedTask;
				else currentGuildUsersPointsPairs.Add(currentGuild.Id, new Dictionary<ulong, float>());
				await Task.CompletedTask;
			};
			discord.MessageCreated += async (s, message) =>
			{
				var currentGuild = message.Guild;
				if (currentGuildUsersPointsPairs[currentGuild.Id].TryGetValue(message.Author.Id, out float authorPoints))
					currentGuildUsersPointsPairs[currentGuild.Id][message.Author.Id] = authorPoints + message.Message.Content.Length * message.Message.Content.Length / 30;
				else currentGuildUsersPointsPairs[currentGuild.Id].Add(message.Author.Id, message.Message.Content.Length * message.Message.Content.Length / 30);

				string json = JsonConvert.SerializeObject(currentGuildUsersPointsPairs, Formatting.Indented);

				File.WriteAllText(databasePath, json);
				await Task.CompletedTask;

			};


			var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
			{
				StringPrefixes = new[] { "." }
			}); commands.RegisterCommands<MyCommands>();

			await discord.ConnectAsync();
			await Task.Delay(-1);
		}

		static void CheckActiveUsersInVoiceChannel(object state)
		{
			var a = AddPointsToActiveUsersInVoiceChannelsAsync();
		}
		public static async Task<int> AddPointsToActiveUsersInVoiceChannelsAsync()
		{
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

		public static Dictionary<ulong, float> GetKeys(DiscordGuild currentGuild)
		{
			return currentGuildUsersPointsPairs[currentGuild.Id];
		}
		internal static Dictionary<ulong, Dictionary<ulong, float>> LoadPointsDataBase()
		{
			try
			{
				string json = File.ReadAllText(databasePath);
				return JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<ulong, float>>>(json);
			}
			catch { return new Dictionary<ulong, Dictionary<ulong, float>>(); }
		}

	}


}