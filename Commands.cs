
﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace DsharpBot
{
	class Commands : BaseCommandModule
	{
		


		[Command("score")]
		public async Task GetScore(CommandContext ctx)
		{
			var data = Program.GetKeys(ctx.Guild);

			await ctx.RespondAsync($"{ctx.Member.Mention}, your score is {data[ctx.User.Id]}");
		}


		[Command("myRoles")]
		public async Task GetRoles(CommandContext ctx)
		{
			var roles = ctx.Member.Roles;
			var response = string.Empty;
			foreach (var role in roles) response += role.Name + "\n";

			foreach (var role in ctx.Member.Roles) response += role.Name + "\n";

			if (response == string.Empty) response = "You dont have roles";

			await ctx.RespondAsync($"{response}");
		}


		[Command("allRoles")]
		public async Task GetAllRoles(CommandContext ctx)
		{
			var roles = ctx.Guild.Roles;
			var response = string.Empty;
			foreach (var role in roles) response += role.Value.Name + "\n";
			if (response == string.Empty) response = "You dont have roles";

			foreach (var role in ctx.Guild.Roles) response += role.Value.Name + "\n";

			if (response == string.Empty) response = "Server dont have roles";

			await ctx.RespondAsync($"{response}");
		}


	}
}