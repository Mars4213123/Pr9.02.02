using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagerTelegramBot_Кантуганов.Classes
{
    public class Users
    {
        public long IdUser { get; set; }
        public long ChatId { get; set; }
        public List<Events> Events { get; set; }
        public Users(long IdUser) {
            this.IdUser = IdUser;
            this.Events = new List<Events>();
        }
    }
}
