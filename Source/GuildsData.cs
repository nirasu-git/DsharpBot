using DSharpPlus.Entities;
using System.Collections.Generic;

namespace DsharpBot
{
	public class Guild {
		public Dictionary<ulong, User> Users = new Dictionary<ulong, User> { };
		public Dictionary<ulong, Respondent> Respondents = new Dictionary<ulong, Respondent> { };
	}

	public class User
	{
		public float Expirience { get; set; }
	}
	public class Respondent
	{
		public List<string> Tags = new List<string> { };

		public string Form { get; set; }

		public List<ulong> DeclinedIds = new List<ulong> { };

		public DiscordUser DiscordLink;

		public string Gender = null;

		public string PrefferedGender = null;

		public string AttachmentUrl { get; set; }
}
}