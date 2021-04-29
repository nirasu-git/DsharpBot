using System.Collections.Generic;

namespace DsharpBot
{
	public class Guild {
		public Dictionary<ulong,User> users { get; set; }
	}

	public class User
	{
		public ulong id { get; set; }

		public float expirience { get; set; }
		public float level { get; set; }
	}
}