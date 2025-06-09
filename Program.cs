using System.Text;
using System.Threading;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static MinecraftServerMonitorSoket;

namespace Misha_s_bot
{
    class Program
    {
        private static string MinecraftServerIP = "ru1.apexnodes.xyz:24344"; //todo: вынести в enviroment

        //Чаты с подпиской на заход человека
        private static List<long> _subscribeJoinChats = new();

        private static MinecraftServerMonitorSoket.ServerStatus _lastStatus;
        private static List<string> _lastPlayers;

        public static event Action<Player> PlayerJoined;
        public static event Action<Player> PlayerLeft;


        //todo: Добавить логгирование через Logger
        // Создаем экземпляр TelegramBotClient, передавая токен бота
        static ITelegramBotClient botClient = new TelegramBotClient(Environment.GetEnvironmentVariable("BOT_TOKEN"));

        //todo: Уйти от статики
        static async Task Main()
        {
            Console.WriteLine("Запуск бота...");

            using var cts = new CancellationTokenSource();

            // Настройка приемника обновлений
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Получаем все типы обновлений
            };

            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            Console.ReadLine();
            await Task.Delay(Timeout.Infinite);
        }

        // Обработка входящих сообщений
        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Проверяем, что обновление содержит сообщение
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            Console.WriteLine($"Чат: {chatId}.Получено сообщение от пользователя: {messageText}");

            _lastStatus = await MinecraftServerMonitorSoket.GetMinecraftStatusAsync("ru1.apexnodes.xyz", 24344);

            if (messageText == "/online")
            {
                var status =_lastStatus;
                var result = "";
                if (status != null)
                {
                    result = $"Игроков онлайн: {status.players.online}/{status.players.max}";

                    if (status.players.sample != null)
                    {
                        result+= "\nСписок игроков:";
                        foreach (var player in status.players.sample)
                            result += $"\n- {player.name}";
                    }
                }
                else
                {
                    result = "Не удалось получить статус сервера.";
                }
                botClient.SendMessage(chatId, result);
            }

            //todo: вынести в метод и сделать отписку
            if (messageText == "/subscribe join")
            {
                if (_subscribeJoinChats.Contains(chatId))
                {
                    botClient.SendMessage(chatId, "Вы уже подписаны на заход человека");
                }
                else
                {
                    _subscribeJoinChats.Add(chatId);
                    botClient.SendMessage(chatId, "Вы успешно подписались на заход человека");
                    PlayerJoined += player => botClient.SendMessage(chatId, $"Игрок ${player} зашел на сервер");
                }
            }

            /*await botClient.SendMessage(
            chatId,
            "Открыть Mini App",
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithWebApp("Запуск", new WebAppInfo("https://v0-telegram-chat-analytics.vercel.app/"))
            )
            );*/
        }

        // Обработка ошибок
        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Ошибка Telegram API:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}