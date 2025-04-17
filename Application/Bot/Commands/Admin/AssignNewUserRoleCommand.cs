using Application.Services;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Bot.Commands.AdminCommands
{
    public class AssignNewUserRoleCommand : BotCommandBase
    {
        private readonly RequestService requestService;

        public AssignNewUserRoleCommand(UserService userService, RequestService requestService)
            : base(userService)
        {
            this.requestService = requestService;
        }

        public override bool CanHandle(string command, uState state, Role role)
        {
            return role == Role.Менеджер && (command == "assign_role" || state == uState.AdminAssignRole);
        }

        public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var chatId = message.Chat.Id;

            var admin = await userService.GetAdminByIdAsync(chatId);
            if (admin.UserState == uState.Idle)
            {
                foreach (var newUser in await userService.GetNewUsersAsync())
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Пользователь: {newUser.Name}",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("Сотрудник", $"assignrole:{newUser.TelegramId}{Role.Сотрудник.ToString()}") },
                            new[] { InlineKeyboardButton.WithCallbackData("Арендатор", $"assignrole:{newUser.TelegramId}:{Role.Арендатор.ToString()}") }
                        }));
                await userService.SetUserState(admin, uState.AdminAssignRole);
            }
            else
            {
                var data = query.Data.Split(":");
                var role = Enum.Parse<Role>(data[2]);
                var oldUser = await userService.GetUserByIdAsync(long.Parse(data[1])) as NewUser;
                var user = await userService.AssignRoleAsync(oldUser, role);
                await userService.SetUserState(admin, uState.AdminAssignRole);
                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Пользователю {user.Name} успешно присвоена роль {role.ToString()}");
                await SendIdleMenu(botClient, chatId);
                await botClient.SendMessage(
                    chatId: user.TelegramId,
                    text: $"Вам присвоена роль {role.ToString()}");
                await SendIdleMenu(botClient, user.TelegramId);
            }
        }
    }
}
