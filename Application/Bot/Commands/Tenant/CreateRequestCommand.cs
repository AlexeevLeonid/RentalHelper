using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using RentalHelper.Domain;
namespace Application.Bot.Commands.Tenant;

public class CreateRequestCommand : BotCommandBase
{
    public CreateRequestCommand()
    {
    }

    public override bool CanHandle(string command, uState s)
    {
        return command == "create_request" || s == uState.TenantCreatingRequest;
    } // Перехватывает любое сообщение для контекста

    public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
    {
        if (message == null) message = query.Message;

        var chatId = message.Chat.Id;

        // Проверяем, выбрал ли пользователь роль
        if (!context.Users.Any(x => x.TelegramId == message.Chat.Id))
        {
            await botClient.SendMessage(chatId, "Пожалуйста, сначала выберите вашу роль с помощью команды /start.");
            return;
        }
        var user = await context.Users.FirstOrDefaultAsync(x => x.TelegramId == chatId) ??
                    throw new ArgumentException("Пользователь отсутствует в системе");
        // Если пользователь отправляет текст после выбора роли
        if (user.UserState == uState.TenantCreatingRequest)
        {
            context.Requests.Add(new Request()
            {
                Description = message.Text ?? throw new ArgumentException("Пустое сообщение"),
                Status = Status.Новая,
                CreatedById = chatId,
                CreatedAt = DateTime.UtcNow,
            });
            user.UserState = uState.Idle;
            await context.SaveChangesAsync();
            await botClient.SendMessage(
                chatId: chatId,
                text: $"Заявка отправлена: {message.Text}");
            await SendIdleMenu(botClient, message, context);
            foreach (var worker in context.Users.Where(x => x.Role == Role.Сотрудник && x.Requests.Any(x => x.Status == Status.Выполняется)))
            {
                await botClient.SendMessage(worker.TelegramId, $"Появилась новая заявка: {message.Text}");
            }
        }
        else
        {
            user.UserState = uState.TenantCreatingRequest;
            await context.SaveChangesAsync();
            await botClient.SendMessage(chatId, "Введите описание вашей заявки:");
        }
    }
}
