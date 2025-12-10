namespace TaskManagerTelegramBot_Кантуганов.Classes
{
    public class RepeatingTask
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public string Message { get; set; }
        public string Schedule { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime LastRun { get; set; }
        public DateTime NextRun { get; set; }

        public RepeatingTask(long chatId, string message, TimeSpan time, string schedule)
        {
            ChatId = chatId;
            Message = message;
            Time = time;
            Schedule = schedule;
            CalculateNextRun();
        }

        public void CalculateNextRun()
        {
            var now = DateTime.Now;
            var today = now.Date;

            var days = Schedule.Split(',');
            var dayNames = days.Select(d => d.Trim()).ToList();

            for (int i = 0; i < 7; i++)
            {
                var checkDate = today.AddDays(i);
                var dayName = checkDate.DayOfWeek.ToString();

                if (dayNames.Contains(dayName))
                {
                    var nextDateTime = checkDate.Add(Time);
                    if (nextDateTime > now)
                    {
                        NextRun = nextDateTime;
                        return;
                    }
                }
            }

            var firstDay = today.AddDays(7);
            while (true)
            {
                var dayName = firstDay.DayOfWeek.ToString();
                if (dayNames.Contains(dayName))
                {
                    NextRun = firstDay.Add(Time);
                    return;
                }
                firstDay = firstDay.AddDays(1);
            }
        }

        public bool ShouldRunNow()
        {
            return DateTime.Now >= NextRun;
        }

        public void MarkAsRun()
        {
            LastRun = DateTime.Now;
            CalculateNextRun();
        }
    }
}