﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DsharpBot
{
	class Commands : BaseCommandModule
	{
		public static Dictionary<ulong, DiscordMember> Members = new Dictionary<ulong, DiscordMember> { };

		[Command("команды"), Aliases("cmds")]
		public async Task ShortCommands(CommandContext ctx)
		{

			await ctx.RespondAsync(
				new DiscordEmbedBuilder{
					Color = new DiscordColor("#B388FD"),
					Description = 
						$" счет - s - score \n" +
						$"получить-роль rolename - tr rolename - takerole rolename \n" +
						$"создать-анкету - cf - createform\n" +
						$"показать-анкету - sf - showform \n" +
						$"найти-по-тегам - fbt - findbytags \n"+
						$"найти-случайно - fr - findrandomly \n",
				
					
					Url = "https://github.com/nirasu-git/hazeBot#readme",
					Title = "Readme"
				});
		}
		[Command("счет"),Aliases("s","score")]
		[RequireGuild]
		public async Task GetScore(CommandContext ctx)
			{
			var guild = Program.GetGuild(ctx.Guild.Id);

			await ctx.RespondAsync(
				new DiscordEmbedBuilder {
					Color = new DiscordColor("#B388FD"),
					Description = $"Ваш счет равен {guild.Users[ctx.User.Id].Expirience}." 
				});
		}
		[Command("получить-роль"), Aliases("tr", "takerole")]
		[RequireGuild]
		public async Task TakeRole(CommandContext ctx, params string[] paramsArray)
		{
			DiscordRole rolesThatWasCreatedEarlier = null;

			int requiredNumberOfPoints = 1000;

			var userExp = Program.GetGuild(ctx.Guild.Id).Users[ctx.User.Id].Expirience;

			bool ableToGive = true;

			if (userExp > requiredNumberOfPoints)
			{
				foreach (var role in ctx.Guild.Roles.Values)
				{
					if (role.Name == string.Join(" ", paramsArray))
					{
						ableToGive = false;
						await ctx.RespondAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description = $"Эта роль уже существует на сервере, попросите администратора выдать ее вам"
							});
					}
				}
				foreach (var role in ctx.Member.Roles)
				{
					if (role.Name == string.Join(" ", paramsArray))
					{
						ableToGive = false;
						await ctx.RespondAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description = $"У вас уже есть эта роль"
							});
					}
				}
				if (ableToGive)
				{
					if (await CollectReactions(ctx))
					{
						if (rolesThatWasCreatedEarlier != null)
						{
							await ctx.Member.GrantRoleAsync(rolesThatWasCreatedEarlier);
						}
						else
						{
							await ctx.Member.GrantRoleAsync(await ctx.Guild.CreateRoleAsync(string.Join(" ", paramsArray)));
						}
						await ctx.RespondAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description = $"Теперь вы {string.Join(" ", paramsArray)}"
							}); ;
					}
					else await ctx.RespondAsync(
						new DiscordEmbedBuilder
						{
							Color = new DiscordColor("#B388FD"),
							Description = "Действие отклонено."
						});
				}
			}
			else
				await ctx.RespondAsync(
					new DiscordEmbedBuilder
					{
						Color = new DiscordColor("#B388FD"),
						Description = $"Вам необходимо набрать еще {requiredNumberOfPoints - userExp} опыта чтобы воспользоваться этой функцией, попробуйте пообщаться!"
					});
		}

		[Command("создать-анкету"), Aliases("cf", "createform")]
		public async Task CreateForm(CommandContext ctx)
		{
			if (!Members.ContainsKey(ctx.User.Id))
			{
				if (ctx.Guild != null)
				{
					Members.Add(ctx.User.Id, ctx.Member);
				}
				else
				{
					await ctx.RespondAsync(
						new DiscordEmbedBuilder
						{
							Color = new DiscordColor("#B388FD"),
							Description = "Извините, но эту команду необходимо первый раз написать на сервере на котором есть этот бот"
						});
				}
			}
			Guild guild = Program.GetGuild(1);

			var member = Members[ctx.User.Id];
			
			if (!guild.Respondents.ContainsKey(ctx.User.Id))
			{
				guild.Respondents.Add(ctx.User.Id, new Respondent());
			}
				
			Respondent respondent = guild.Respondents[ctx.User.Id];

			respondent.Id = ctx.User.Id;
			respondent.DiscordLink = member.DisplayName + "#" + member.Discriminator;
		
			var dm = await member.SendMessageAsync(
				new DiscordEmbedBuilder {
					Color = new DiscordColor("#B388FD"),
					Description = "Введите через запятую теги, по которым будет производится поиск, например название игры, род занятий или тема для обсуждения (dota2, c#, политика)" 
				});

			var channel = dm.Channel;

			var result = await channel.GetNextMessageAsync(ctx.User);

			var tags = result.Result.Content.Replace(" ", "");

			var tagsArray = tags.Split(",");
			respondent.Tags = tagsArray.ToList();
			
			if (!result.TimedOut)
			{
				await member.SendMessageAsync(
					new DiscordEmbedBuilder {
						Color = new DiscordColor("#B388FD"),
						Description = "Введите содержимое вашей анкеты как дополнение к тегам и прикрепите к этому сообщению картинку (одним сообщением):" 
					});

				var data = await channel.GetNextMessageAsync(ctx.User);

				if (!data.TimedOut)
				{
					respondent.Form = data.Result.Content;

					if (data.Result.Attachments.Count < 1)
						respondent.AttachmentUrl = data.Result.Author.AvatarUrl;
					
					else
						respondent.AttachmentUrl = data.Result.Attachments[0].Url;
					

					respondent.FormId = new Random().Next(392193, 999999999);
					Program.SaveFormsData(guild);
					await ShowFormPrivate(ctx);
					
				}
			}	
		}
		[Command("показать-мою-анкету"), Aliases("sf","showform")]
		public async Task ShowForm(CommandContext ctx)
		{
			Guild guild = Program.GetGuild(1);
			
			if (guild.Respondents.ContainsKey(ctx.User.Id))
			{
				Respondent respondent = guild.Respondents[ctx.User.Id];
				await ctx.Channel.SendMessageAsync(
					new DiscordEmbedBuilder
					{
						Color = new DiscordColor("#B388FD"),
						Description = $"Ваша анкета: \nТеги: { string.Join(", ", respondent.Tags)} \n" + $"Анкета: {respondent.Form}",
						ImageUrl = respondent.AttachmentUrl });
			}
			else
			{
				await ctx.RespondAsync($"У вас отсутствует анкета");
			}
		}
		public async Task ShowFormPrivate(CommandContext ctx)
		{
			Guild guild = Program.GetGuild(1);

			Respondent respondent = guild.Respondents[ctx.User.Id];
			await Members[ctx.User.Id].SendMessageAsync(
					new DiscordEmbedBuilder
					{
						Color = new DiscordColor("#B388FD"),
						Description = $"Ваша анкета: \nТеги: { string.Join(", ", respondent.Tags)} \n" + $"Анкета: {respondent.Form}",
						ImageUrl = respondent.AttachmentUrl
					});
			await Members[ctx.User.Id].SendMessageAsync(
					new DiscordEmbedBuilder
					{
						Color = new DiscordColor("#B388FD"),
						Description = $"Используйте команду **-fr** чтобы найти случайного человека или **-fbt** чтобы найти человека по тегам. Команды можете вводить как на сервере, так и прямо сюда."
					});
		}

		[Command("найти-по-тегам"), Aliases("fbt", "findbytags")]
		public async Task FindByTags(CommandContext ctx)
		{
			Guild guild = Program.GetGuild(1);
			Respondent respondent = guild.Respondents[ctx.User.Id];

			if (guild.Respondents[ctx.User.Id].Form == null)
				await ctx.RespondAsync(
					new DiscordEmbedBuilder
					{
						Color = new DiscordColor("#B388FD"),
						Description = "Вам необходимо для начала заполнить анкету!"
					});
			else
			{
				List<string> tags = guild.Respondents[ctx.User.Id].Tags;

				Respondent prefferedRespondent = new Respondent();

				string matchedTags = string.Empty;
				int maxPoints = 0;

				foreach (var candidate in guild.Respondents.Values)
				{
					int points = 0;
					matchedTags = string.Empty;

					foreach (var candidateTag in candidate.Tags)
					{
						foreach (var respondentTag in tags)
						{
							if (respondentTag.ToLower() == candidateTag.ToLower())
							{
								matchedTags += " " + respondentTag.ToLower();
								points += 1;
							}
						}
					}
					if ((points > maxPoints) && (candidate.Id != ctx.User.Id) && (!guild.Respondents[ctx.User.Id].ViewedIds.Contains(candidate.FormId)))
					{
						maxPoints = points;
						prefferedRespondent = candidate;
					}
				}
				if (maxPoints > 0)
				{
					var b = await ctx.RespondAsync(
						new DiscordEmbedBuilder
						{
							Color = new DiscordColor("#B388FD"),
							Description =
								$"Найден подходящий пользователь \n" +
								$"Совпадающие теги:{matchedTags};\n" +
								$"Полный список его тегов: {string.Join(", ", prefferedRespondent.Tags)};  \n" +
								$"\n{prefferedRespondent.Form} \n",

							ImageUrl = guild.Respondents[prefferedRespondent.Id].AttachmentUrl
						}.Build());

					await b.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":heart:"));
					await Task.Delay(100);
					await b.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":broken_heart:"));

					var a = await b.WaitForReactionAsync(ctx.User);


					if (a.Result.Emoji.GetDiscordName() == ":heart:")
					{
						guild.Respondents[ctx.User.Id].ViewedIds.Add(prefferedRespondent.FormId);
						await ctx.RespondAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description =
									$" Пользователю отправлено сообщение с вашими данными. \nПоиск нового кандидата..."

							});
						await Members[prefferedRespondent.Id].SendMessageAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description =
									$"Вас оценил пользователь.\n" +
									$"Код добавления: {respondent.DiscordLink}\n" +
									$"Анкета: {respondent.Form}\n" +
									$"Теги: { string.Join(", ", respondent.Tags)};\n",
								ImageUrl = respondent.AttachmentUrl

							});
					}
					else if (a.Result.Emoji.GetDiscordName() == ":broken_heart:")
					{
						guild.Respondents[ctx.User.Id].ViewedIds.Add(prefferedRespondent.FormId);
						await ctx.RespondAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description =
									$"Поиск нового кандидата..."

							});
					}
					else
					{
						await ctx.RespondAsync($"Пожалуйста, используйте реакции предусмотренные системой");
					}

					Program.SaveFormsData(guild);
					await FindByTags(ctx);
				}
				else
				{
					await Members[ctx.User.Id].SendMessageAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description =
									$"По вашим тегам никого не нашлось, скорее всего вы **неправильно их заполнили.** Запускаю случайный поиск"

							});
					await FindRandomly(ctx);
					
				}
			}
		}

		[Command("найти-случайно"), Aliases("fr", "findrandomly")]
		public async Task FindRandomly(CommandContext ctx)
		{
			var guild = Program.GetGuild(1);
			if (guild.Respondents[ctx.User.Id].Form == null)
				await ctx.RespondAsync(
					new DiscordEmbedBuilder
					{
						Color = new DiscordColor("#B388FD"),
						Description = "Вам необходимо для начала заполнить анкету!"
					});
			else
			{
				
				Respondent respondent = guild.Respondents[ctx.User.Id];
				Respondent prefferedRespondent = new Respondent();
				int iterations = 0;
				while (iterations < guild.Respondents.Keys.Count)
				{
					iterations++;
					prefferedRespondent = guild.Respondents.ElementAt(new Random().Next(0, guild.Respondents.Count)).Value;
					if (prefferedRespondent.Id != ctx.User.Id && !respondent.ViewedIds.Contains(prefferedRespondent.FormId))
					{
						break;
					}
				}

				if (prefferedRespondent.Id != ctx.User.Id && !respondent.ViewedIds.Contains(prefferedRespondent.FormId))
				{

					var b = await ctx.RespondAsync(
						new DiscordEmbedBuilder
						{
							Color = new DiscordColor("#B388FD"),
							Description =
								$"Найден подходящий пользователь \n" +
								$"Полный список его тегов: {string.Join(", ", prefferedRespondent.Tags)};  \n" +
								$"\n{prefferedRespondent.Form} \n",

							ImageUrl = guild.Respondents[prefferedRespondent.Id].AttachmentUrl
						}.Build());

					await b.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":heart:"));
					await Task.Delay(100);
					await b.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":broken_heart:"));

					var a = await b.WaitForReactionAsync(ctx.User);

					if (a.Result.Emoji.GetDiscordName() == ":heart:")
					{
						guild.Respondents[ctx.User.Id].ViewedIds.Add(prefferedRespondent.FormId);
						await ctx.RespondAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description =
									$" Пользователю отправлено сообщение с вашими данными, ожидайте. \nПоиск нового кандидата..."

							});
						await Members[prefferedRespondent.Id].SendMessageAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description =
									$"Вас оценил пользователь.\n" +
									$"Код добавления: {respondent.DiscordLink}\n" +
									$"Анкета: {respondent.Form}\n" +
									$"Теги: { string.Join(", ", respondent.Tags)};\n",
								ImageUrl = respondent.AttachmentUrl

							});

					}
					else if (a.Result.Emoji.GetDiscordName() == ":broken_heart:")
					{
						guild.Respondents[ctx.User.Id].ViewedIds.Add(prefferedRespondent.FormId);
						await ctx.RespondAsync(
							new DiscordEmbedBuilder
							{
								Color = new DiscordColor("#B388FD"),
								Description =
									$"Поиск нового кандидата..."

							});

					}

					Program.SaveFormsData(guild);
					await FindRandomly(ctx);
				}
				else
				{
					await ctx.RespondAsync(
						 new DiscordEmbedBuilder
						 {
							 Color = new DiscordColor("#B388FD"),
							 Description =
								 $"Я не смогла никого найти, в системе находится слишком мало анкет.."

						 });
				}
			}
		}

		public async Task<bool> CollectReactions(CommandContext ctx)
        {
			var message = await ctx.RespondAsync(
					 new DiscordEmbedBuilder
					 {
						 Color = new DiscordColor("#B388FD"),
						 Description =
							 $"ГОЛОСОВАНИЕ!"

					 });
			await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
			await Task.Delay(100);
			await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
			await Task.Delay(100);
			var reactions = await message.CollectReactionsAsync(TimeSpan.FromSeconds(30));

			var total = 0;
			foreach (var reaction in reactions)
			{
				if (reaction.Emoji.GetDiscordName() == ":thumbsup:") total++;
				if (reaction.Emoji.GetDiscordName() == ":thumbsdown:") total--;
			}
			return total>1;
		}
		public static void AddMember(ulong a, DiscordMember b)
        {
			if (!Members.ContainsKey(a))
				Members.Add(a, b);	
        }
	}
}