﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace DsharpBot
{
	class Commands : BaseCommandModule
	{
		
		[Command("score")][RequireGuild]
		public async Task GetScore(CommandContext ctx)
		{
			var data = Program.GetGuild(ctx.Guild.Id);

			await ctx.RespondAsync($"{ctx.Member.Mention}, your score is {data.users[ctx.User.Id].expirience}");

		}


		[Command("myRoles")]
		[RequireGuild]
		public async Task GetRoles(CommandContext ctx)
		{
		
			var response = string.Empty;
			
			foreach (var role in ctx.Member.Roles) response += role.Name + "\n";

			if (response == string.Empty) response = "You dont have roles";

			await ctx.RespondAsync($"{response}");
		}


		[Command("allRoles")]
		[RequireGuild]
		public async Task GetAllRoles(CommandContext ctx)
		{

			var response = string.Empty;
			
			foreach (var role in ctx.Guild.Roles) response += role.Value.Name + "\n";

			if (response == string.Empty) response = "Server dont have roles";

			await ctx.RespondAsync($"{response}");
		}
		[Command("GimmeRole")][RequireGuild]
		public async Task GimmeRoles(CommandContext ctx)
		{

			var response = string.Empty;

			foreach (var role in ctx.Guild.Roles) response += role.Value.Name + "\n";

			if (response == string.Empty) response = "Server dont have roles";

			await ctx.RespondAsync($"{response}");
		}


	}
}