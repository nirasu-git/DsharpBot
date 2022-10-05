using System.Collections.Generic;

namespace haze.Source
{
    public class Respondent
    {
        public List<int> ViewedFormIds = new List<int>(10);
        public string[] Tags;
        public ulong Id;
        public string Form;
        public int FormId;
        public string DiscordLink;
        public string AttachmentUrl;
    }
}