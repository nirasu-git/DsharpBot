using DSharpPlus.Entities;
using System.Collections.Generic;

namespace DsharpBot
{
	public class Guild {
		public Dictionary<ulong, User> users = new Dictionary<ulong, User> { };
		public Dictionary<ulong, Respondent> respondents = new Dictionary<ulong, Respondent> { };
	}

	public class User
	{
		public float expirience { get; set; }
	}
	public class Respondent
	{
		public List<string> tags = new List<string> { };

		public string form { get; set; }

		public List<ulong> declinedIDs = new List<ulong> { };

		public DiscordUser discordLink;

		public string gender = null;

		public string prefferedGender = null;

		public DiscordAttachment attachment { get; set; }
}
}