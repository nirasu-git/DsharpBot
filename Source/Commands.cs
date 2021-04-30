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
		string[] genders = {"м", "ж"};
		[Command("счет"),Aliases("s")]
		[RequireGuild]
		public async Task GetScore(CommandContext ctx)
		{
			var guild= Program.GetGuild(ctx.Guild.Id);

			await ctx.RespondAsync($"{ctx.Member.Mention}, your score is {guild.users[ctx.User.Id].expirience}");
		}
		[Command("получить-роль"), Aliases("tr")]
		[RequireGuild]
		public async Task TakeRole(CommandContext ctx, params string[] paramsArray)
		{
			DiscordRole rolesThatWasCreatedEarlier = null;

			var userExp = Program.GetGuild(ctx.Guild.Id).users[ctx.User.Id].expirience;

            if (userExp > 1000) 
			{ 
				foreach (var role in ctx.Guild.Roles.Values)
				{
					if (role.Name == paramsArray[0]) rolesThatWasCreatedEarlier = role;
				}
				foreach (var role in ctx.Member.Roles)
				{
					if (role.Name == paramsArray[0]) await ctx.RespondAsync("You already have this role."); await Task.CompletedTask;
				}
				if (rolesThatWasCreatedEarlier != null)  await ctx.Member.GrantRoleAsync(rolesThatWasCreatedEarlier);
				else
				{
					if (await CollectReactions(ctx))
					{	
						await ctx.Member.GrantRoleAsync(await ctx.Guild.CreateRoleAsync(paramsArray[0], null, new DiscordColor(paramsArray[1])));
						await ctx.RespondAsync("Action confirmed.");	
						await ctx.RespondAsync($"You are {paramsArray[0]} now");
					}
					else await ctx.RespondAsync("Action declined.");
				}
			}
			else 
				await ctx.RespondAsync($"You need {1000-userExp} exp more to use this feature");
		}
		[Command("добавить-теги"), Aliases("addtgs")]
		public async Task SetTags(CommandContext ctx, params string[] newTags)
		{
			var guild = Program.GetGuild(1);

			if (guild.respondents.ContainsKey(ctx.User.Id))
				foreach (var tag in newTags)
                    if (!guild.respondents[ctx.User.Id].tags.Contains(tag))
                        guild.respondents[ctx.User.Id].tags.Add(tag.ToLower());
					
			else 
				guild.respondents.Add(ctx.User.Id, new Respondent(){ tags = newTags.ToList()});
			
			Program.SaveFormsData(guild);
			await ctx.RespondAsync($"Your tags now - {string.Join(", ", guild.respondents[ctx.User.Id].tags) }");
		}
		[Command("показать-теги-пользователя"), Aliases("shwtgs")]
		public async Task ShowTags(CommandContext ctx, DiscordMember user)
		{
			var guild = Program.GetGuild(1);

			if (guild.respondents[user.Id].tags != null || guild.respondents[user.Id].tags.Count!=0) 
				await ctx.RespondAsync($"У пользователя следующие теги - {string.Join(", ", guild.respondents[user.Id].tags)}");

			else await ctx.RespondAsync($"У пользователя нет тегов");
		}
		[Command("удалить-теги"), Aliases("rmtgs")]
		public async Task RemoveTags(CommandContext ctx, params string[] tagsToRemove)
		{
			var guild = Program.GetGuild(1);

			if (guild.respondents.ContainsKey(ctx.User.Id))
				foreach (var tag in tagsToRemove)
					if (guild.respondents[ctx.User.Id].tags.Contains(tag))
						guild.respondents[ctx.User.Id].tags.Remove(tag);
				
			else
				await ctx.RespondAsync($"У вас нет тегов");
			
			Program.SaveFormsData(guild);
			await ctx.RespondAsync($"Указанные теги были удалены");
		}
		[Command("удалить-все-теги"), Aliases("rmatgs")]
		public async Task RemoveAllTags(CommandContext ctx)
		{
			var guild = Program.GetGuild(1);

			if (guild.respondents.ContainsKey(ctx.User.Id))
			{
				guild.respondents[ctx.User.Id].tags.Clear();
			}
			Program.SaveFormsData(guild);
			await ctx.RespondAsync($"Все теги удалены");
		}
		[Command("создать-анкету"), Aliases("crtfrm")]
		public async Task RewriteBlank(CommandContext ctx)
		{
			var guild = Program.GetGuild(1);

			if (!guild.respondents.ContainsKey(ctx.User.Id)) 
				guild.respondents.Add(ctx.User.Id, new Respondent());

			await ctx.RespondAsync("Ваш пол: *м* или *ж*");

			var result = await ctx.Message.GetNextMessageAsync(m =>
			{
				if (genders.Contains(m.Content.ToLower()))
				{
					guild.respondents[ctx.User.Id].gender = m.Content;
					return true;
				}
				else return false;
			});
			if (!result.TimedOut)
			{
				await ctx.RespondAsync("Предпочтительный пол: *м* или *ж*");
				result = await ctx.Message.GetNextMessageAsync(m =>
				{
					if (genders.Contains(m.Content.ToLower()))
					{
						guild.respondents[ctx.User.Id].prefferedGender = m.Content;
						return true;
					}
					else return false;
				});
				if (!result.TimedOut)
				{
					await ctx.RespondAsync("Введите содержимое вашей анкеты и отправьте картинку:");
					result = await ctx.Message.GetNextMessageAsync(m =>
					{
						guild.respondents[ctx.User.Id].form = m.Content;
						guild.respondents[ctx.User.Id].attachment = m.Attachments[0];
						guild.respondents[ctx.User.Id].discordLink = ctx.User;
						ctx.RespondAsync("Анкета создана");
						return true;
					});
					

				}
				else
					await ctx.RespondAsync("Предпочитаемый пол не указан либо указан неверно.");
			}
			else
				await ctx.RespondAsync("Пол не указан либо указан неверно.");

			Program.SaveFormsData(guild);
		}
		[Command("показать-анкету"), Aliases("shwfrm")]
		public async Task ShowForm(CommandContext ctx, DiscordMember user)
		{
			var guild = Program.GetGuild(1);
			if (guild.respondents.ContainsKey(user.Id) & guild.respondents[user.Id].form != null) 

				await ctx.RespondAsync($"{guild.respondents[user.Id].form}", new DiscordEmbedBuilder{ ImageUrl = guild.respondents[user.Id].attachment.Url }.Build());

			else await ctx.RespondAsync($"У указанного пользователя отсутствует анкета");
		}
		[Command("код-пользователя"), Aliases("uc")]
		public async Task ShowUser(CommandContext ctx, DiscordMember user)
		{
			await ctx.RespondAsync($"{user.Username}#{user.Discriminator}");
			await ctx.RespondAsync($"{user.Id}");
		}
	
		[Command("найти-человека-по-тегам"), Aliases("findbytags")]
		public async Task FindByTags(CommandContext ctx)
		{
			var guild = Program.GetGuild(1);
			
			List<string> tags = guild.respondents[ctx.User.Id].tags;

			Respondent prefferedRespondent = new Respondent();

			string matchedTags = string.Empty;
			
			int maxPoints = -1;
			foreach (var respondent in guild.respondents.Values)
			{
				int points = 0;

				foreach (var tag in respondent.tags)
                {
					if (tags.Contains(tag))
						matchedTags += " " + tag;
						points += 1;
                }
				if (points > maxPoints)
				{
					maxPoints = points;
					prefferedRespondent = respondent;
				}
			
			}
				
			await ctx.RespondAsync($"С данным пользователем совпали следующие теги:{matchedTags};\n" +
				$"{prefferedRespondent.discordLink.Username}#{prefferedRespondent.discordLink.Discriminator} \n" +
				$"{prefferedRespondent.form}", new DiscordEmbedBuilder{ ImageUrl = guild.respondents[prefferedRespondent.discordLink.Id].attachment.Url }.Build());
		}


		public async Task<bool> CollectReactions(CommandContext ctx)
        {
			var message = await ctx.RespondAsync("Реагируйте на это сообщение :thumbsup: или  :thumbsdown: чтобы проголосовать.");

			var reactions = await message.CollectReactionsAsync();

			var total = 0;
			foreach (var reaction in reactions)
			{
				if (reaction.Emoji.GetDiscordName() == ":thumbsup:") total++;
				if (reaction.Emoji.GetDiscordName() == ":thumbsdown:") total--;
			}
			return total>0;
		}
	}
}