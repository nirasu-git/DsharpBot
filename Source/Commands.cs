using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace haze.Source
{
    internal class Commands : BaseCommandModule
    {
        private static Dictionary<ulong, DiscordMember> Members = new Dictionary<ulong, DiscordMember>();

        public static Dictionary<ulong, string[]> IdTagsPairs = new Dictionary<ulong, string[]>();

        [Command("команды"), Aliases("cmds")]
        public async Task ShortCommands(CommandContext ctx)
        {
            await ctx.RespondAsync(
                new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#B388FD"),
                    Description =
                        $"создать-анкету - cf - createform\n" +
                        $"показать-анкету - sf - showform \n" +
                        $"найти-по-тегам - fbt - findbytags \n" +
                        $"найти-случайно - fr - findrandomly \n",
                    Url = "https://github.com/nirasu/haze#readme",
                    Title = "Readme"
                });
        }

        [Command("создать-анкету"), Aliases("cf", "createform")]
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
                        ToEmbed(
                                "Извините, но эту команду необходимо " +
                                "первый раз написать на сервере на котором есть этот бот"
                            ));
                }
            }
            var member = Members[ctx.User.Id];

            var respondent = new Respondent
            {
                Id = ctx.User.Id,
                DiscordLink = member.DisplayName + "#" + member.Discriminator
            };

            var discordMessage = await member.SendMessageAsync(
                ToEmbed(
                        "Введите через запятую теги, по которым будет производится поиск, " +
                        "например название игры, род занятий или тема для обсуждения. Регистр не важен. Пример сообщения: \ndota2, c#, политика"
                    ));

            var channel = discordMessage.Channel;
            var result = await channel.GetNextMessageAsync(ctx.User);
            var tags = result.Result.Content.Split(", ");
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
                    ToEmbed(
                            "Введите содержимое вашей анкеты как дополнение к тегам и прикрепите " +
                            "к этому сообщению картинку (одним сообщением): "
                        ));

            var data = await channel.GetNextMessageAsync(ctx.User);

            respondent.Form = data.Result.Content;

            if (data.Result.Attachments.Count < 1)
                respondent.AttachmentUrl = data.Result.Author.AvatarUrl;
            else
                respondent.AttachmentUrl = data.Result.Attachments[0].Url;

            respondent.FormId = new Random().Next(2, 999999999);

            DataBase.SaveForm(respondent);
            await ShowFormPrivate(ctx, respondent);
        }

        [Command("показать-мою-анкету"), Aliases("sf", "showform")]
        public async Task ShowForm(CommandContext ctx)
        {
            if (IdTagsPairs.ContainsKey(ctx.User.Id))
            {
                Respondent respondent = DataBase.LoadForm(ctx.User.Id);
                if (respondent != null)
                {
                    await ctx.Channel.SendMessageAsync(
                        ToEmbed(
                                $"Ваша анкета: \nТеги: { string.Join(", ", respondent.Tags)} \n" + $"Анкета: {respondent.Form}",
                                respondent.AttachmentUrl
                            ));
                }
            }
            else
            {
                await ctx.RespondAsync(
                    ToEmbed(
                            $"У вас отсутствует анкета."
                        ));
            }
        }

        [Command("найти-по-тегам"), Aliases("fbt", "findbytags")]
        public async Task FindByTags(CommandContext ctx)
        {
            Respondent respondent = DataBase.LoadForm(ctx.User.Id);

            if (respondent.Form == null)
                await ctx.RespondAsync(
                    ToEmbed(
                            "Вам необходимо сначала заполнить анкету."
                        ));
            else
            {
                Respondent prefferedRespondent = new Respondent();

                string matchedTags;

                int maxPoints = 0;
                int index = 0;
                for (int i = 0; i < IdTagsPairs.Values.Count; i++)
                {
                    int points = 0;
                    matchedTags = string.Empty;

                    foreach (var candidateTag in IdTagsPairs.ElementAt(i).Value)
                    {
                        foreach (var respondentTag in respondent.Tags)
                        {
                            if (respondentTag.ToLower() == candidateTag.ToLower())
                            {
                                matchedTags += " " + respondentTag.ToLower();
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
                        ToEmbed(
                                $"По вашим тегам никого не нашлось, " +
                                $"скорее всего вы **неправильно их заполнили.** Запускаю случайный поиск"
                            ));
                    await FindRandomly(ctx);
                }
            }
        }

        [Command("найти-случайно"), Aliases("fr", "findrandomly")]
        public async Task FindRandomly(CommandContext ctx)
        {
            Respondent respondent = DataBase.LoadForm(ctx.User.Id);

            if (respondent == null)
                await ctx.RespondAsync(
                    ToEmbed(
                            "Вам необходимо для начала заполнить анкету!"
                        ));
            else
            {
                Respondent prefferedRespondent = new Respondent();
                int iterations = 0;
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
                    await ctx.RespondAsync(
                        ToEmbed(
                                $"Я не смогла никого найти, в системе находится слишком мало анкет.."
                            ));
                }
            }
        }

        public static void AddMember(ulong id, DiscordMember member)
        {
            if (!Members.ContainsKey(id))
                Members.Add(id, member);
        }

        private static async void SendPrefferedRespondentForm(CommandContext ctx, Respondent respondent, Respondent prefferedRespondent)
        {
            var b = await ctx.RespondAsync(
                        ToEmbed(
                                $"Найден подходящий пользователь \n" +
                                $"Полный список его тегов: {string.Join(", ", prefferedRespondent.Tags)};  \n" +
                                $"\n{prefferedRespondent.Form} \n",
                            prefferedRespondent.AttachmentUrl
                            ));

            await b.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":heart:"));
            await Task.Delay(100);
            await b.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":broken_heart:"));

            var a = await b.WaitForReactionAsync(ctx.User);

            if (a.Result.Emoji.GetDiscordName() == ":heart:")
            {
                DataBase.AddVieviedFormIdToRespondent(ctx.User.Id, prefferedRespondent.FormId);
                await ctx.RespondAsync(
                    ToEmbed(
                            $" Пользователю отправлено сообщение с вашими данными, ожидайте. \n" +
                            $"Поиск нового кандидата..."
                        ));
                await Members[prefferedRespondent.Id].SendMessageAsync(
                    ToEmbed(
                            $"Вас оценил пользователь.\n" +
                            $"Код добавления: {respondent.DiscordLink}\n" +
                            $"Анкета: {respondent.Form}\n" +
                            $"Теги: { string.Join(", ", respondent.Tags)};\n",
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

        private static DiscordEmbed ToEmbed(string text, string imageUrl)
        {
            return new DiscordEmbedBuilder
            {
                Color = new DiscordColor("#B388FD"),
                Description = text,
                ImageUrl = imageUrl
            };
        }

        private async Task ShowFormPrivate(CommandContext ctx, Respondent respondent)
        {
            await Members[ctx.User.Id].SendMessageAsync(
                    ToEmbed(
                            $"Ваша анкета: \nТеги: { string.Join(", ", respondent.Tags)} \n" + $"Анкета: {respondent.Form}",
                            respondent.AttachmentUrl
                        ));
            await Members[ctx.User.Id].SendMessageAsync(
                ToEmbed(
                        $"Используйте команду **-fr** чтобы найти случайного человека или **-fbt** " +
                        $"чтобы найти человека по тегам. Команды можете вводить как на сервере, так и прямо сюда."
                    ));
        }

        private static DiscordEmbed ToEmbed(string text)
        {
            return new DiscordEmbedBuilder
            {
                Color = new DiscordColor("#B388FD"),
                Description = text
            };
        }
    }
}