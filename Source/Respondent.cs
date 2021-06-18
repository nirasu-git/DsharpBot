using System.Collections.Generic;

namespace haze.Source
{
    public class Respondent
    {
        public ulong Id;

        public string[] Tags;

        public string Form;

        public int FormId { get; set; }

        public List<int> ViewedFormIds = new List<int>();

        public string DiscordLink;

        public string AttachmentUrl { get; set; }
    }
}