﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DsharpBot
{
	class Commands : BaseCommandModule
	{
		
		[Command("Score")]
		[RequireGuild]
		public async Task GetScore(CommandContext ctx)
		{
			var data = Program.GetGuild(ctx.Guild.Id);

			await ctx.RespondAsync($"{ctx.Member.Mention}, your score is {data.users[ctx.User.Id].expirience}");
		}

		[Command("MyRoles")]
		[RequireGuild]
		public async Task GetRoles(CommandContext ctx)
		{
		
			var response = string.Empty;
			
			foreach (var role in ctx.Member.Roles) response += role.Name + "\n";

			if (response == string.Empty) response = "You dont have roles";

			await ctx.RespondAsync($"{response}");
		}
		
		[Command("AllRoles")]
		[RequireGuild]
		public async Task GetAllRoles(CommandContext ctx)
		{

			var response = string.Empty;
			
			foreach (var role in ctx.Guild.Roles) response += role.Value.Name + "\n";

			if (response == string.Empty) response = "Server dont have roles";

			await ctx.RespondAsync($"{response}");
		}
		
		[Command("TakeRole")]
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
					var message = await ctx.RespondAsync("React to this with :thumbsup: or :thumbsdown: to vote.");

					var reactions = await message.CollectReactionsAsync();

					var total = 0;
					foreach (var reaction in reactions)
					{
						if (reaction.Emoji.GetDiscordName() == ":thumbsup:") total++;
						if (reaction.Emoji.GetDiscordName() == ":thumbsdown:") total--;
					}
					if (total > 0)
					{
						await ctx.RespondAsync("Action confirmed.");
						await ctx.Member.GrantRoleAsync(await ctx.Guild.CreateRoleAsync(paramsArray[0], null, new DiscordColor(paramsArray[1])));

						await ctx.RespondAsync($"You are {paramsArray[0]} now");

					}
					else await ctx.RespondAsync("Action declined.");
					
				}
			}else await ctx.RespondAsync($"You need {1000-userExp} exp more to use this feature");
		}



	}
}