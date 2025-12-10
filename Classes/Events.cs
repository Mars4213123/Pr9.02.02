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
        public Events(DateTime Time, string Message) {
            this.Time = Time;
            this.Message = Message;
        }
    }
}
