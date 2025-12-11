namespace TaskManagerTelegramBot_Кантуганов
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::TaskManagerTelegramBot_Кантуганов.Classes;
    using MySql.Data.MySqlClient;
    using Telegram.Bot;
    using Telegram.Bot.Polling;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types.ReplyMarkups;
    using static System.Net.Mime.MediaTypeNames;

    namespace TaskManagerTelegramBot_Кантуганов.Classes
    {
        public class Worker : BackgroundService
        {
            readonly string ConnectionConfig = "Server=127.0.0.1;Port=3306;DataBase=tg_bot;user=root;password=;";
            readonly string Token = "8575507641:AAFiOOJLTD2_0v0ypJHgx2ygK8ERc1qrQ4I";
            TelegramBotClient TelegramBotClient;
            List<Users> Users = new List<Users>();
            Timer Timer;
            List<string> Message = new List<string>()
        {
            "Здравствуйте! " +
                "\nРады приветствовать вас в Telegram-боте «Напоминатор»" +
                "\nНаш бот создан для того, чтобы напоминать вам о важных событиях и мероприятиях. С ним вы точно не пропустите ничего важного!" +
                "\nНе забудьте добавить бота в список своих контактов и настроить уведомления. Тогда вы всегда будете в курсе событий!",
            "Укажите дату и время напоминания в следующем формате: " +
                "\n<i><b>12:51 26.04.2025</b>" +
                "\nНапомни о том что я хотел сходить в магазин.</i>",

            "Кажется, что-то не получилось." +
                "Укажите дату и время напоминания в следующем формате: " +
                "\n<i><b>12:51 26.04.2025</b>" +
                "\nНапомни о том что я хотел сходить в магазин.</i>",
            "",
            "Задачи пользователя не найдены.",
            "Событие удалено.",
            "Все события удалены.",
            "Для создания повторяющегося напоминания используйте формат:\n" +
                "<i><b>каждую среду и воскресенье в 21:00</b>\n" +
                "Полить цветы.</i>\n\n" +
                "Поддерживаемые дни: понедельник, вторник, среду, четверг, пятницу, субботу, воскресенье"
        };

            private async Task EnsureUserExistsAsync(long chatId)
            {
                using (MySqlConnection connection = Connection())
                {
                    await connection.OpenAsync();

                    string checkQuery = "SELECT COUNT(*) FROM `tg_bot`.`Users` WHERE `ChatId` = @ChatId";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@ChatId", chatId);
                        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                        if (count == 0)
                        {
                            string insertQuery = "INSERT INTO `tg_bot`.`Users` (`ChatId`) VALUES (@ChatId)";
                            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@ChatId", chatId);
                                await insertCmd.ExecuteNonQueryAsync();
                                Console.WriteLine($"Добавлен новый пользователь: {chatId}");
                            }
                        }
                    }
                }
            }

            public bool CheckFormatDateTime(string value, out DateTime time)
            {
                return DateTime.TryParse(value, out time);
            }

            private static ReplyKeyboardMarkup GetButtons()
            {
                List<KeyboardButton> keyboardButtons = new List<KeyboardButton>();
                keyboardButtons.Add(new KeyboardButton("Удалить все задачи"));
                return new ReplyKeyboardMarkup
                {
                    Keyboard = new List<List<KeyboardButton>> {
                    keyboardButtons
                }
                };
            }

            public static InlineKeyboardMarkup DeleteEvent(string Message)
            {
                List<InlineKeyboardButton> inlineKeyboards = new List<InlineKeyboardButton>();
                inlineKeyboards.Add(new InlineKeyboardButton("Удалить", Message));
                return new InlineKeyboardMarkup(inlineKeyboards);
            }

            public async void SendMessage(long chatId, int typeMessage)
            {
                if (typeMessage != 3)
                {
                    await TelegramBotClient.SendMessage(
                        chatId,
                        Message[typeMessage],
                        ParseMode.Html,
                        replyMarkup: GetButtons());
                }
                else if (typeMessage == 3)
                    await TelegramBotClient.SendMessage(
                        chatId,
                        $"Указанное вами время и дата не могут быть установлены, " +
                        $"потому что сейчас уже: {DateTime.Now.ToString("HH:mm dd.MM.yyyy")}");
            }

            public async void Command(long chatId, string command)
            {
                if (command.ToLower() == "/start")
                {
                    await EnsureUserExistsAsync(chatId);
                    SendMessage(chatId, 0);
                }
                else if (command.ToLower() == "/create_task")
                {
                    SendMessage(chatId, 1);
                    await TelegramBotClient.SendMessage(
                        chatId,
                        Message[7],
                        ParseMode.Html
                    );
                }
                else if (command.ToLower() == "/list_tasks")
                {
                    Users User = Users.Find(x => x.IdUser == chatId);
                    if (User == null) SendMessage(chatId, 4);
                    else if (User.Events.Count == 0) SendMessage(chatId, 4);
                    else
                    {
                        foreach (Events Event in User.Events)
                        {
                            string eventType = Event.IsRepeating ? "Повторяющееся" : "Разовое";
                            string scheduleInfo = Event.IsRepeating ?
                                $"\nПовтор: {Event.RepeatSchedule}" : "";

                            await TelegramBotClient.SendMessage(
                                chatId,
                                $"{eventType}\n" +
                                $"Время: {Event.Time:HH:mm dd.MM.yyyy}" +
                                $"{scheduleInfo}\n" +
                                $"Задача: {Event.Message}",
                                replyMarkup: DeleteEvent(Event.Message)
                            );
                        }
                    }
                }
            }

            private async Task GetMessagesAsync(Message message)
            {
                Console.WriteLine($"📩 Получено сообщение: {message.Text} от пользователя: {message.Chat.Id} ({message.Chat.Username})");
                long IdUser = message.Chat.Id;

                await EnsureUserExistsAsync(IdUser);

                if (message.Text.Contains("/"))
                {
                    Command(message.Chat.Id, message.Text);
                }
                else if (message.Text.Equals("Удалить все задачи"))
                {
                    string EventQuaryDelete = "DELETE FROM `tg_bot`.`Events` WHERE `ChatId` = @ChatId";
                    using (MySqlConnection connection = Connection())
                    {
                        using (MySqlCommand cmd = new MySqlCommand(EventQuaryDelete, connection))
                        {
                            cmd.Parameters.AddWithValue("@ChatId", message.Chat.Id);
                            await connection.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    Users User = Users.Find(x => x.IdUser == message.Chat.Id);
                    if (User == null) SendMessage(message.Chat.Id, 4);
                    else if (User.Events.Count == 0) SendMessage(User.IdUser, 4);
                    else
                    {
                        User.Events = new List<Events>();
                        SendMessage(User.IdUser, 6);
                    }
                }
                else
                {
                    Users User = Users.Find(x => x.IdUser == message.Chat.Id);
                    if (User == null)
                    {
                        User = new Users(message.Chat.Id);
                        Users.Add(User);
                        Console.WriteLine($"Добавлен пользователь в список: {message.Chat.Id}");
                    }

                    string[] Info = message.Text.Split('\n');
                    if (Info.Length < 2)
                    {
                        SendMessage(message.Chat.Id, 2);
                        return;
                    }

                    if (Info[0].Contains("каждую") || Info[0].Contains("каждый"))
                    {
                        await ProcessRepeatingTaskAsync(User, message.Text);
                        return;
                    }

                    DateTime Time;
                    if (CheckFormatDateTime(Info[0], out Time) == false)
                    {
                        SendMessage(message.Chat.Id, 2);
                        return;
                    }

                    if (Time < DateTime.Now)
                    {
                        SendMessage(message.Chat.Id, 3);
                        return;
                    }

                    string taskMessage = message.Text.Replace(Time.ToString("HH:mm dd.MM.yyyy") + "\n", "");
                    Events newEvent = new Events(Time, taskMessage);
                    User.Events.Add(newEvent);

                    string EventQuaryInsert = "INSERT INTO `tg_bot`.`Events` (`ChatId`, `Time`, `Message`, `IsRepeating`, `RepeatSchedule`, `RepeatTime`) " +
                                             "VALUES (@ChatId, @Time, @Message, @IsRepeating, @RepeatSchedule, @RepeatTime)";

                    using (MySqlConnection connection = Connection())
                    {
                        await connection.OpenAsync();
                        using (MySqlCommand cmd = new MySqlCommand(EventQuaryInsert, connection))
                        {
                            cmd.Parameters.AddWithValue("@ChatId", message.Chat.Id);
                            cmd.Parameters.AddWithValue("@Time", Time);
                            cmd.Parameters.AddWithValue("@Message", taskMessage);
                            cmd.Parameters.AddWithValue("@IsRepeating", false);
                            cmd.Parameters.AddWithValue("@RepeatSchedule", DBNull.Value);
                            cmd.Parameters.AddWithValue("@RepeatTime", DBNull.Value);

                            await cmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"Добавлено разовое событие для пользователя {message.Chat.Id}");
                        }
                    }

                    await TelegramBotClient.SendMessage(
                        message.Chat.Id,
                        $"Создано напоминание на {Time:dd.MM.yyyy HH:mm}\nЗадача: {taskMessage}"
                    );
                }
            }

            private async Task HandleUpdateAsync(
                ITelegramBotClient client,
                Update update,
                CancellationToken cancellationToken)
            {
                try
                {
                    if (update.Type == UpdateType.Message)
                    {
                        await GetMessagesAsync(update.Message);
                    }
                    else if (update.Type == UpdateType.CallbackQuery)
                    {
                        CallbackQuery query = update.CallbackQuery;
                        Users User = Users.Find(x => x.IdUser == query.Message.Chat.Id);
                        if (User != null)
                        {
                            Events Event = User.Events.Find(x => x.Message == query.Data);
                            if (Event != null)
                            {
                                User.Events.Remove(Event);

                                string deleteQuery = "DELETE FROM `tg_bot`.`Events` WHERE `ChatId` = @ChatId AND `Message` = @Message";
                                using (MySqlConnection connection = Connection())
                                {
                                    await connection.OpenAsync();
                                    using (MySqlCommand cmd = new MySqlCommand(deleteQuery, connection))
                                    {
                                        cmd.Parameters.AddWithValue("@ChatId", User.IdUser);
                                        cmd.Parameters.AddWithValue("@Message", query.Data);
                                        await cmd.ExecuteNonQueryAsync();
                                        Console.WriteLine($"Удалено событие для пользователя {User.IdUser}");
                                    }
                                }

                                SendMessage(query.Message.Chat.Id, 5);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в HandleUpdateAsync: {ex.Message}");
                }
            }

            private async Task HandleErrorAsync(
                ITelegramBotClient client,
                Exception exception,
                HandleErrorSource source,
                CancellationToken token)
            {
                Console.WriteLine($"Ошибка бота: {exception.Message}");
            }

            public async void Tick(object obj)
            {
                string TimeNow = DateTime.Now.ToString("HH:mm dd.MM.yyyy");
                DateTime now = DateTime.Now;

                foreach (Users User in Users)
                {
                    for (int i = User.Events.Count - 1; i >= 0; i--)
                    {
                        var currentEvent = User.Events[i];

                        if (currentEvent.Time.ToString("HH:mm dd.MM.yyyy") == TimeNow)
                        {
                            await TelegramBotClient.SendMessage(
                                User.IdUser,
                                "Напоминание: " + currentEvent.Message
                            );

                            if (currentEvent.IsRepeating)
                            {
                                currentEvent.UpdateForNextOccurrence();
                                Console.WriteLine($"Повторяющееся событие обновлено на: {currentEvent.Time}");
                            }
                            else
                            {
                                User.Events.RemoveAt(i);
                            }
                        }
                    }
                }
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                try
                {
                    Console.WriteLine("Запуск Telegram бота...");
                    TelegramBotClient = new TelegramBotClient(Token);

                    await LoadExistingUsersAsync();

                    TelegramBotClient.StartReceiving(
                        HandleUpdateAsync,
                        HandleErrorAsync,
                        null,
                        new CancellationTokenSource().Token);

                    TimerCallback TimerCallback = new TimerCallback(Tick);
                    Timer = new Timer(TimerCallback, 0, 0, 60 * 1000);

                    Console.WriteLine("Бот запущен успешно!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка при запуске бота: {ex.Message}");
                }
            }

            private async Task LoadExistingUsersAsync()
            {
                try
                {
                    using (MySqlConnection connection = Connection())
                    {
                        await connection.OpenAsync();
                        string query = "SELECT `ChatId` FROM `tg_bot`.`Users`";
                        using (MySqlCommand cmd = new MySqlCommand(query, connection))
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                long chatId = reader.GetInt64("ChatId");
                                if (!Users.Any(u => u.IdUser == chatId))
                                {
                                    Users.Add(new Users(chatId));
                                    Console.WriteLine($"Загружен пользователь из базы: {chatId}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки пользователей: {ex.Message}");
                }
            }

            public MySqlConnection Connection()
            {
                MySqlConnection connection = new MySqlConnection(ConnectionConfig);
                return connection;
            }

            private async Task ProcessRepeatingTaskAsync(Users user, string text)
            {
                try
                {
                    Console.WriteLine($"Обработка повторяющейся задачи: {text}");

                    string[] lines = text.Split('\n');
                    if (lines.Length < 2)
                    {
                        SendMessage(user.IdUser, 2);
                        return;
                    }

                    string scheduleLine = lines[0];
                    string taskMessage = lines[1];

                    var days = new List<string>();
                    if (scheduleLine.Contains("понедельник")) days.Add("Monday");
                    if (scheduleLine.Contains("вторник")) days.Add("Tuesday");
                    if (scheduleLine.Contains("среду")) days.Add("Wednesday");
                    if (scheduleLine.Contains("четверг")) days.Add("Thursday");
                    if (scheduleLine.Contains("пятницу")) days.Add("Friday");
                    if (scheduleLine.Contains("субботу")) days.Add("Saturday");
                    if (scheduleLine.Contains("воскресенье")) days.Add("Sunday");

                    if (days.Count == 0)
                    {
                        SendMessage(user.IdUser, 2);
                        return;
                    }

                    TimeSpan time;
                    if (!TryParseTimeFromText(scheduleLine, out time))
                    {
                        SendMessage(user.IdUser, 2);
                        return;
                    }

                    string schedule = string.Join(",", days);
                    Events repeatingEvent = new Events(taskMessage, schedule, time);

                    user.Events.Add(repeatingEvent);

                    string EventQuaryInsert = "INSERT INTO `tg_bot`.`Events` (`ChatId`, `Time`, `Message`, `IsRepeating`, `RepeatSchedule`, `RepeatTime`) " +
                                             "VALUES (@ChatId, @Time, @Message, @IsRepeating, @RepeatSchedule, @RepeatTime)";

                    using (MySqlConnection connection = Connection())
                    {
                        await connection.OpenAsync();
                        using (MySqlCommand cmd = new MySqlCommand(EventQuaryInsert, connection))
                        {
                            cmd.Parameters.AddWithValue("@ChatId", user.IdUser);
                            cmd.Parameters.AddWithValue("@Time", repeatingEvent.Time);
                            cmd.Parameters.AddWithValue("@Message", taskMessage);
                            cmd.Parameters.AddWithValue("@IsRepeating", true);
                            cmd.Parameters.AddWithValue("@RepeatSchedule", schedule);
                            cmd.Parameters.AddWithValue("@RepeatTime", time);

                            int rowsAffected = await cmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"Добавлено повторяющееся событие. Затронуто строк: {rowsAffected}");
                        }
                    }

                    string daysInRussian = ExtractRussianDays(scheduleLine);

                    await TelegramBotClient.SendMessage(
                        user.IdUser,
                        $"Создано повторяющееся напоминание!\n" +
                        $"Дни: {daysInRussian}\n" +
                        $"Время: {time:hh\\:mm}\n" +
                        $"Задача: {taskMessage}\n" +
                        $"Следующий раз: {repeatingEvent.Time:dd.MM.yyyy HH:mm}"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка при создании повторяющейся задачи: {ex.Message}");
                    Console.WriteLine($"🔍 Внутренняя ошибка: {ex.InnerException?.Message}");
                    SendMessage(user.IdUser, 2);
                }
            }

            private string ExtractRussianDays(string scheduleLine)
            {
                var russianDays = new List<string>();

                if (scheduleLine.Contains("понедельник")) russianDays.Add("понедельник");
                if (scheduleLine.Contains("вторник")) russianDays.Add("вторник");
                if (scheduleLine.Contains("среду")) russianDays.Add("среду");
                if (scheduleLine.Contains("четверг")) russianDays.Add("четверг");
                if (scheduleLine.Contains("пятницу")) russianDays.Add("пятницу");
                if (scheduleLine.Contains("субботу")) russianDays.Add("субботу");
                if (scheduleLine.Contains("воскресенье")) russianDays.Add("воскресенье");

                return string.Join(" и ", russianDays);
            }

            private bool TryParseTimeFromText(string text, out TimeSpan time)
            {
                time = TimeSpan.Zero;

                var timeMatch = System.Text.RegularExpressions.Regex.Match(text, @"(\d{1,2}):(\d{2})");
                if (timeMatch.Success)
                {
                    int hours = int.Parse(timeMatch.Groups[1].Value);
                    int minutes = int.Parse(timeMatch.Groups[2].Value);

                    if (hours == 24) hours = 0;

                    time = new TimeSpan(hours, minutes, 0);
                    return true;
                }

                return false;
            }
        }
    }
}