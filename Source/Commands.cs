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
				$"Список коротких команд:\n" +
				$"счет - s \n" +
				$"получить-роль rolename - tr rolename \n" +
				$"создать-анкету - cf \n" +
				$"показать-анкету - sf \n" +
				$"найти-человека-по-тегам - fbt \n"
				);
		}
		[Command("счет"),Aliases("s")]
		[RequireGuild]
		public async Task GetScore(CommandContext ctx)
		{
			var guild = Program.GetGuild(ctx.Guild.Id);

			await ctx.RespondAsync($"{ctx.Member.Mention}, your score is {guild.Users[ctx.User.Id].Expirience}");
		}
		[Command("получить-роль"), Aliases("tr")]
		[RequireGuild]
		public async Task TakeRole(CommandContext ctx, params string[] paramsArray)
		{
			DiscordRole rolesThatWasCreatedEarlier = null;

			int requiredNumberOfPoints = 1000;

			var userExp = Program.GetGuild(ctx.Guild.Id).Users[ctx.User.Id].Expirience;

			if (userExp > requiredNumberOfPoints) 
			{ 
				foreach (var role in ctx.Guild.Roles.Values)
				{
					if (role.Name == paramsArray[0]) 
						rolesThatWasCreatedEarlier = role;
				}
				foreach (var role in ctx.Member.Roles)
				{
					if (role.Name == paramsArray[0]) 
						await ctx.RespondAsync("You already have this role."); await Task.CompletedTask;
				}
				
					if (await CollectReactions(ctx))
					{
						if (rolesThatWasCreatedEarlier != null) 
						{
							await ctx.Member.GrantRoleAsync(rolesThatWasCreatedEarlier);
						}
						else
						{
						await ctx.Member.GrantRoleAsync(await ctx.Guild.CreateRoleAsync(paramsArray[0]));
						}
					await ctx.RespondAsync("Action confirmed.");
					await ctx.RespondAsync($"You are {paramsArray[0]} now");
				}
					else await ctx.RespondAsync("Action declined.");
				
			}
			else 
				await ctx.RespondAsync($"You need {requiredNumberOfPoints-userExp} exp more to use this feature");
		}

		[Command("создать-анкету"), Aliases("cf")]
		public async Task RewriteBlank(CommandContext ctx)
		{
			if (!Members.ContainsKey(ctx.User.Id))
			{
				if (ctx.Guild != null)
				{
					Members.Add(ctx.User.Id, ctx.Member);
				}
				else
				{
					await ctx.RespondAsync("Извините, но эту команду необходимо первый раз написать на сервере на котором есть этот бот(");
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

			var directMessageChannel = await member.CreateDmChannelAsync();

			await directMessageChannel.SendMessageAsync("Введите через запятую теги, по которым будет производится поиск, например название игры, род занятий или тема для обсуждения (dota2, c#, политика)");

			var result = await directMessageChannel.GetNextMessageAsync(m  =>
			{
				var tags = m.Content.Split(", ");
				respondent.Tags = tags.ToList();
				return true;
			});
			if (!result.TimedOut)
			{
				await directMessageChannel.SendMessageAsync("Введите содержимое вашей анкеты как дополнение к тегам и прикрепите к этому сообщению картинку (одним сообщением):");
				var data = await directMessageChannel.GetNextMessageAsync();
				if (!data.TimedOut)
				{
					respondent.Form = data.Result.Content;

					if (data.Result.Attachments.Count < 1)
					{
						respondent.AttachmentUrl = data.Result.Author.AvatarUrl;
						await directMessageChannel.SendMessageAsync("Обратите внимание - вы не отправили картинку, поэтому к анкете прикреплена ваша аватарка," +
							" вы можете пересоздать анкету повторным вводом команды");
					}
					else
					{
						respondent.AttachmentUrl = data.Result.Attachments[0].Url;
					}
					
					Program.SaveFormsData(guild);
					await directMessageChannel.SendMessageAsync("Анкета создана и сохранена");
				}
				
			}	
		}
		[Command("показать-мою-анкету"), Aliases("sf")]
		public async Task ShowForm(CommandContext ctx)
		{
			Guild guild = Program.GetGuild(1);
			
			if (guild.Respondents.ContainsKey(ctx.User.Id))
			{
				Respondent respondent = guild.Respondents[ctx.User.Id];
				await ctx.RespondAsync(
					$"Теги: { string.Join(", ", respondent.Tags)} \n" +
					$"Анкета: {respondent.Form}",
					new DiscordEmbedBuilder { ImageUrl = respondent.AttachmentUrl }.Build());
			}
			else
			{
				await ctx.RespondAsync($"У указанного пользователя отсутствует анкета");
			}
		}
		[Command("найти-человека-по-тегам"), Aliases("fbt")]
		public async Task FindByTags(CommandContext ctx)
		{
			Guild guild = Program.GetGuild(1);
			Respondent respondent = guild.Respondents[ctx.User.Id];
			
			if (guild.Respondents[ctx.User.Id].Form == null )
				await ctx.RespondAsync("Для начала необходимо заполнить анкету!");
			else
			{
				List<string> tags = guild.Respondents[ctx.User.Id].Tags;

				Respondent prefferedRespondent = new Respondent();

				string matchedTags = string.Empty;
				int maxPoints = 0;
				int points = 0;
				foreach (var candidate in guild.Respondents.Values)
				{
					points = 0;
					matchedTags = string.Empty;

					foreach (var tag in candidate.Tags)
					{
						if (tags.Contains(tag.ToLower()))
						{
							matchedTags += " " + tag;
							points += 1;
						}
					}
					if ((points > maxPoints) && (candidate.Id != ctx.User.Id )&& (!guild.Respondents[ctx.User.Id].ViewedIds.Contains(candidate.Id)))
					{
						maxPoints = points;
						prefferedRespondent = candidate;
					}
				}
				if (maxPoints > 0)
				{
					await ctx.RespondAsync($"Найден подходящий пользователь \n" +
						$"Совпадающие теги:{matchedTags};\n" +
						$"Полный список его тегов: {string.Join(" ", prefferedRespondent.Tags)};  \n" +
						$"Анкета: \n{prefferedRespondent.Form} \n" +
						$"1  - :heart: \n" +
						$"2  - :broken_heart:", new DiscordEmbedBuilder { ImageUrl = guild.Respondents[prefferedRespondent.Id].AttachmentUrl }.Build());

					var answer = await ctx.Channel.GetNextMessageAsync(ctx.User);
					
					if (!answer.TimedOut)
					{
						if (answer.Result.Content == "1")
						{
							guild.Respondents[ctx.User.Id].ViewedIds.Add(prefferedRespondent.Id);

							Program.SaveFormsData(guild);

							var b = await Members[prefferedRespondent.Id].SendMessageAsync(

								$"Вас оценил пользователь.\n" +
								$"Код добавления: {respondent.DiscordLink}\n" +
								$"Анкета: {respondent.Form}\n"+
								$"Теги:{ string.Join(", ",respondent.Tags)};\n",
								new DiscordEmbedBuilder { ImageUrl = respondent.AttachmentUrl });

							await Task.CompletedTask;
						}
						else if (answer.Result.Content == "2")
						{
							guild.Respondents[ctx.User.Id].ViewedIds.Add(prefferedRespondent.Id);
							Program.SaveFormsData(guild);
						}
						else
						{ 
							await ctx.RespondAsync($"Пожалуйста, отвечайте на анкету ТОЛЬКО ЦИФРАМИ 1 или 2, другие вариантов ответа на данный момент не предусмотрены"); 
						}
					}
				}
				else
				{
					await ctx.RespondAsync($"Увы, никого не нашлось, попробуйте изменить теги");
				}
			}
		}
		public async Task<bool> CollectReactions(CommandContext ctx)
        {
			var message = await ctx.RespondAsync("Реагируйте на это сообщение :thumbsup: или  :thumbsdown: чтобы проголосовать.");

			var reactions = await message.CollectReactionsAsync(TimeSpan.FromSeconds(30));

			var total = 0;
			foreach (var reaction in reactions)
			{
				if (reaction.Emoji.GetDiscordName() == ":thumbsup:") total++;
				if (reaction.Emoji.GetDiscordName() == ":thumbsdown:") total--;
			}
			return total>0;
		}
		public static void AddMember(ulong a, DiscordMember b)
        {
			if (!Members.ContainsKey(a))
				Members.Add(a, b);	
        }
	}
}