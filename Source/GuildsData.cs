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

		public string Form = null;
		public int FormId { get; set; }

		public List<int> ViewedIds = new List<int> { };

		public ulong Id;

		public string DiscordLink;
		public string AttachmentUrl { get; set; }
	}
}