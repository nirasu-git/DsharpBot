using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace haze
{
    internal class Commands : BaseCommandModule
    {

        private const string CommaSplitter = ", ";

        private static readonly DiscordColor defaultColor = new DiscordColor("#B388FD");

        private static readonly Dictionary<ulong, DiscordMember> Members = new Dictionary<ulong, DiscordMember>();

        public static Dictionary<ulong, string[]> IdTagsPairs = new Dictionary<ulong, string[]>();

        [Command("команды"), Aliases("help")]
        public async Task ShortCommands(CommandContext ctx)
        {
            await ctx.RespondAsync(
                ToEmbed(
                    new StringBuilder()
                         .Append("создать-анкету - cf \n")
                         .Append("показать-анкету - sf \n")
                         .Append("найти-по-тегам - fbt \n")
                         .Append("найти-случайно - fr \n")
                         .ToString()
                ));
        }

        [Command("создать-анкету"), Aliases("cf")]
        public async Task CreateForm(CommandContext ctx)
        {
            if (!Members.ContainsKey(ctx.User.Id))
            {
                if (ctx.Guild != null)
                {
                    Members.Add(ctx.User.Id, ctx.Member);
                }
                else
                {
                    await ctx.RespondAsync(
                        ToEmbed("Извините, но эту команду необходимо первый раз написать на сервере на котором есть этот бот")
                        );
                }
            }
            var member = Members[ctx.User.Id];

            var respondent = new Respondent
            {
                Id = ctx.User.Id,
                DiscordLink = new StringBuilder()
                .Append(member.DisplayName)
                .Append("#")
                .Append(member.Discriminator)
                .ToString()
            };

            var discordMessage = await member.SendMessageAsync(
                ToEmbed(
                    new StringBuilder()
                    .Append("Введите через запятую теги, по которым будет производится поиск, ")
                    .Append("например название игры, род занятий или тема для обсуждения. Регистр не важен. Пример сообщения: \ndota2, c#, политика")
                    .ToString()
                    ));

            var channel = discordMessage.Channel;
            var result = await channel.GetNextMessageAsync(ctx.User);
            var tags = result.Result.Content.Split(CommaSplitter);
            respondent.Tags = tags;

            if (!IdTagsPairs.ContainsKey(ctx.User.Id))
            {
                IdTagsPairs.Add(ctx.User.Id, tags);
            }
            else
            {
                IdTagsPairs[ctx.User.Id] = tags;
            }

            await member.SendMessageAsync(
                    ToEmbed("Введите содержимое вашей анкеты как дополнение к тегам и прикрепите к этому сообщению картинку (одним сообщением): ")
                    );

            var data = await channel.GetNextMessageAsync(ctx.User);

            respondent.Form = data.Result.Content;

            if (data.Result.Attachments.Count < 1)
                respondent.AttachmentUrl = data.Result.Author.AvatarUrl;
            else
                respondent.AttachmentUrl = data.Result.Attachments[0].Url;

            respondent.FormId = DataBase.LastFormId + 1;

            DataBase.SaveForm(respondent);
            await ShowFormPrivate(ctx, respondent);
        }

        [Command("показать-мою-анкету"), Aliases("sf")]
        public async Task ShowForm(CommandContext ctx)
        {
            if (IdTagsPairs.ContainsKey(ctx.User.Id))
            {
                var respondent = DataBase.LoadForm(ctx.User.Id);
                if (respondent != null)
                {
                    await ctx.Channel.SendMessageAsync(
                        ToEmbed(
                                string.Format("Ваша анкета: \nТеги: {0} \n Анкета: {1}", string.Join(CommaSplitter, respondent.Tags), respondent.Form),
                                respondent.AttachmentUrl
                            ));
                }
            }
            else
            {
                await ctx.RespondAsync(ToEmbed($"У вас отсутствует анкета."));
            }
        }

        [Command("найти-по-тегам"), Aliases("fbt")]
        public async Task FindByTags(CommandContext ctx)
        {
            var respondent = DataBase.LoadForm(ctx.User.Id);

            if (respondent.Form == null)
                await ctx.RespondAsync(ToEmbed("Вам необходимо сначала заполнить анкету."));
            else
            {
                var prefferedRespondent = new Respondent();

                string matchedTags;

                var maxPoints = 0;
                var index = 0;
                for (var i = 0; i < IdTagsPairs.Values.Count; i++)
                {
                    var points = 0;
                    matchedTags = string.Empty;

                    foreach (var candidateTag in IdTagsPairs.ElementAt(i).Value)
                    {
                        foreach (var respondentTag in respondent.Tags)
                        {
                            if (respondentTag.ToLower() == candidateTag.ToLower())
                            {
                                matchedTags += respondentTag.ToLower();
                                points += 1;
                                index = i;
                            }
                        }
                    }
                    if (points > maxPoints && IdTagsPairs.ElementAt(index).Key != ctx.User.Id && !respondent.ViewedFormIds.Contains(DataBase.LoadFormId(IdTagsPairs.ElementAt(index).Key)))
                    {
                        maxPoints = points;
                        prefferedRespondent = DataBase.LoadForm(IdTagsPairs.ElementAt(index).Key);
                    }
                }
                if (maxPoints > 0)
                {
                    SendPrefferedRespondentForm(ctx, respondent, prefferedRespondent);
                }
                else
                {
                    await Members[ctx.User.Id].SendMessageAsync(
                        ToEmbed($"По вашим тегам никого не нашлось, скорее всего вы **неправильно их заполнили.** Запускаю случайный поиск"));
                    await FindRandomly(ctx);
                }
            }
        }

        [Command("найти-случайно"), Aliases("fr")]
        public async Task FindRandomly(CommandContext ctx)
        {
            var respondent = DataBase.LoadForm(ctx.User.Id);

            if (respondent == null)
                await ctx.RespondAsync(
                    ToEmbed("Вам необходимо для начала заполнить анкету!"));
            else
            {
                var prefferedRespondent = new Respondent();
                var iterations = 0;
                while (iterations < IdTagsPairs.Keys.Count)
                {
                    iterations++;
                    prefferedRespondent = DataBase.LoadForm(IdTagsPairs.ElementAt(new Random().Next(0, IdTagsPairs.Count)).Key);
                    if (prefferedRespondent.Id != ctx.User.Id && !respondent.ViewedFormIds.Contains(prefferedRespondent.FormId))
                    {
                        break;
                    }
                }

                if (prefferedRespondent.Id != ctx.User.Id && !respondent.ViewedFormIds.Contains(prefferedRespondent.FormId))
                {
                    SendPrefferedRespondentForm(ctx, respondent, prefferedRespondent);

                    await FindRandomly(ctx);
                }
                else
                {
                    await ctx.RespondAsync(ToEmbed($"В системе находится слишком мало анкет."));
                }
            }
        }

        private static async void SendPrefferedRespondentForm(CommandContext ctx, Respondent respondent, Respondent prefferedRespondent)
        {
            var b = await ctx.RespondAsync(
                        ToEmbed(
                            string.Format(
                                new StringBuilder()
                                    .Append("Найден подходящий пользователь \n")
                                    .Append("Полный список его тегов: {0}; \n Форма: {1} \n")
                                .ToString(),
                                string.Join(CommaSplitter, prefferedRespondent.Tags),
                                prefferedRespondent.Form),
                            prefferedRespondent.AttachmentUrl
                            ));

            await b.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":heart:"));

            // без этого 2 реакции подряд создаваться не хотят
            await Task.Delay(100);

            await b.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":broken_heart:"));

            var a = await b.WaitForReactionAsync(ctx.User);

            if (a.Result.Emoji.GetDiscordName() == ":heart:")
            {
                DataBase.AddVieviedFormIdToRespondent(ctx.User.Id, prefferedRespondent.FormId);

                await ctx.RespondAsync(ToEmbed($" Пользователю отправлено сообщение с вашими данными, ожидайте. \n Поиск нового кандидата..."));

                await Members[prefferedRespondent.Id].SendMessageAsync(
                    ToEmbed(string.Format(new StringBuilder()
                                    .Append("Вас оценил пользователь.\n")
                                    .Append("Код добавления: {0}; \n Анкета: {1} \n Теги: {2}")
                                .ToString(),
                                respondent.DiscordLink,
                            respondent.Form,
                            string.Join(CommaSplitter, respondent.Tags)),
                        respondent.AttachmentUrl
                        ));
            }
            else if (a.Result.Emoji.GetDiscordName() == ":broken_heart:")
            {
                DataBase.AddVieviedFormIdToRespondent(ctx.User.Id, prefferedRespondent.FormId);
                await ctx.RespondAsync(
                    ToEmbed(
                            $"Поиск нового кандидата..."
                        ));
            }
        }

        private async Task ShowFormPrivate(CommandContext ctx, Respondent respondent)
        {
            await Members[ctx.User.Id].SendMessageAsync(
                    ToEmbed(
                            string.Format("Ваша анкета: \nТеги: {0} \n Анкета: {1}", string.Join(CommaSplitter, respondent.Tags), respondent.Form),
                            respondent.AttachmentUrl
                        ));
            await Members[ctx.User.Id].SendMessageAsync(
                ToEmbed(
                    new StringBuilder()
                    .Append("Используйте команду **-fr** чтобы найти случайного человека или **-fbt** ")
                    .Append("чтобы найти человека по тегам. Команды можете вводить как на сервере, так и прямо сюда.")
                    .ToString()
                    ));
        }

        public static void AddMember(ulong id, DiscordMember member)
        {
            if (!Members.ContainsKey(id))
                Members.Add(id, member);
        }

        private static DiscordEmbed ToEmbed(string text)
        {
            return new DiscordEmbedBuilder
            {
                Color = defaultColor,
                Description = text
            };
        }
        private static DiscordEmbed ToEmbed(string text, string imageUrl)
        {
            return new DiscordEmbedBuilder
            {
                Color = defaultColor,
                Description = text,
                ImageUrl = imageUrl
            };
        }
    }
}