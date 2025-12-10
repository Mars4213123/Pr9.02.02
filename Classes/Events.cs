using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagerTelegramBot_Кантуганов.Classes
{
    public class Events
    {
        public DateTime Time { get; set; }
        public string Message { get; set; }
        public bool IsRepeating { get; set; }
        public string RepeatSchedule { get; set; }
        public TimeSpan? RepeatTime { get; set; }
        public Events(DateTime Time, string Message) {
            this.Time = Time;
            this.Message = Message;
            this.IsRepeating = false;
        }
        public Events(string message, string schedule, TimeSpan time)
        {
            this.Message = message;
            this.IsRepeating = true;
            this.RepeatSchedule = schedule;
            this.RepeatTime = time;
            CalculateNextTime();
        }
        private void CalculateNextTime()
        {
            if (!IsRepeating || !RepeatTime.HasValue) return;

            var now = DateTime.Now;
            var days = RepeatSchedule.Split(',');
            var dayNames = days.Select(d => d.Trim()).ToList();

            for (int i = 0; i < 7; i++)
            {
                var checkDate = now.Date.AddDays(i);
                var dayName = checkDate.DayOfWeek.ToString();

                if (dayNames.Contains(dayName))
                {
                    var nextDateTime = checkDate.Add(RepeatTime.Value);

                    if (i == 0 && nextDateTime > now)
                    {
                        Time = nextDateTime;
                        return;
                    }
                    else if (i > 0)
                    {
                        Time = nextDateTime;
                        return;
                    }
                }
            }
            for (int i = 1; i <= 7; i++)
            {
                var checkDate = now.Date.AddDays(i);
                var dayName = checkDate.DayOfWeek.ToString();

                if (dayNames.Contains(dayName))
                {
                    Time = checkDate.Add(RepeatTime.Value);
                    return;
                }
            }
        }

        public void UpdateForNextOccurrence()
        {
            if (IsRepeating && RepeatTime.HasValue)
            {
                CalculateNextTime();
            }
        }
    }
}
