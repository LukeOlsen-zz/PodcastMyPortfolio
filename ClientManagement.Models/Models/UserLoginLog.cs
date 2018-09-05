using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class UserLoginLog
    {
        private int _userId;
        private LocalDateTime _loginOn;

        public int UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        public LocalDateTime LoginOn
        {
            get { return _loginOn; }
            set { _loginOn = value; }
        }

        public UserLoginLog(int userId, LocalDateTime loginOn)
        {
            _userId = userId;
            _loginOn = loginOn;
        }

    }   
}
