using MInecraft_Notifier;
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
        private static Dictionary<long, MinecraftHost> ChatIp = new();
        //Чаты с подпиской на заход человека
        private static List<long> _subscribeJoinChats = new();

        private static MinecraftServerMonitorSoket.ServerStatus _lastStatus;
        private static List<string> _lastPlayers;

        public static event Action<Player> PlayerJoined;
        public static event Action<Player> PlayerLeft;


        //todo: Добавить логгирование через Logger
        //todo: Добавить catch ошибок и вывод пользователю(сервер недоступен, и т.д)
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

            if (messageText.StartsWith("/setIp"))
            {
                try
                {
                    var host = ParseSetIpCommand(messageText);
                    var status = await MinecraftServerMonitorSoket.GetMinecraftStatusAsync(host.ip, host.port);
                    if(status == null)
                    {
                        throw new Exception("Сервер недоступен");
                    }

                    if (ChatIp.ContainsKey(chatId))
                    {
                        ChatIp[chatId] =  new MinecraftHost(host.ip, host.port);
                    } else
                    {
                        ChatIp.Add(chatId, new MinecraftHost(host.ip, host.port));
                    }
                    botClient.SendMessage(chatId, "Ip успешно установлен");
                }
                catch (Exception ex)
                {
                    botClient.SendMessage(chatId, $"Не удалось установить ip для сервера.\n{ex.Message}");
                }
            }


            if (messageText == "/online")
            {
                if (ChatIp.ContainsKey(chatId))
                {
                    _lastStatus = await MinecraftServerMonitorSoket.GetMinecraftStatusAsync(ChatIp[chatId].Ip, ChatIp[chatId].Port);

                    var status = _lastStatus;
                    var result = "";
                    if (status != null)
                    {
                        result = $"Игроков онлайн: {status.players.online}/{status.players.max}";

                        if (status.players.sample != null)
                        {
                            result += "\nСписок игроков:";
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
                else
                {
                    botClient.SendMessage(chatId, "Не установлен ip, установите с помощью команды \n \"/setIp  <ip:port>\" ");
                }
            }

            if (messageText == "/status")
            {
                if (ChatIp.ContainsKey(chatId))
                {
                    _lastStatus = await MinecraftServerMonitorSoket.GetMinecraftStatusAsync(ChatIp[chatId].Ip, ChatIp[chatId].Port);

                    var status = _lastStatus;
                    var result = "";
                    if (status != null)
                    {

                        var names = status.players.sample != null
                            ? status.players.sample.Select(x => x.name)
                            : new List<string>();

                        var players = new List<(string Name, Stream HeadStream)>();

                        var headTasks = names.Select(async name =>
                        {
                            var uuid = await MinecraftHeadFetcher.GetUUIDAsync(name);
                            if (uuid == null)
                            {
                                Console.WriteLine($"Не удалось получить голову игрока: {name}");
                                uuid = await MinecraftHeadFetcher.GetUUIDAsync("steve");
                            };

                            var head = await MinecraftHeadFetcher.GetHeadImageAsync(uuid);
                            return (Name: name, Head: head);
                        });

                        var playerHeads = await Task.WhenAll(headTasks);

                        // Пример: перебор и работа с результатами
                        foreach (var player in playerHeads)
                        {
                            Console.WriteLine($"Игрок: {player.Name}, Голова получена: {player.Head != null}");
                        }

                        using var imageStream = await PlayerImageDrawer.DrawPlayerListAsync(playerHeads.ToList(), status.players.max);

                        var image = InputFile.FromStream(imageStream);
                        botClient.SendPhoto(chatId, image);
                    }
                    else
                    {
                        Console.WriteLine("Не удалось получить статус сервера.");
                    }
                }
                else
                {
                    botClient.SendMessage(chatId, "Не установлен ip, установите с помощью команды \n \"/setIp  <ip:port>\" ");
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

                if (messageText == "/ipset")
                {


                }

                /*await botClient.SendMessage(
                chatId,
                "Открыть Mini App",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithWebApp("Запуск", new WebAppInfo("https://v0-telegram-chat-analytics.vercel.app/"))
                )
                );*/
            }
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

        static (string ip, int port) ParseSetIpCommand(string message)
        {
            const string prefix = "/setIp ";
            if (!message.StartsWith(prefix))
                throw new ArgumentException("Сообщение должно начинаться с /setIp");

            string addressPart = message.Substring(prefix.Length).Trim();
            var parts = addressPart.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
                throw new ArgumentException("Неверный формат IP:PORT");

            return (parts[0], port);
        }
    }
}