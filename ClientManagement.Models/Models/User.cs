using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class User
    {
        public int Id { get; set; }
        public int FirmId { get; set; }
        public string UserName { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string FullName { get; set; }
        public byte[] ProfileImage { get; set; }
        public LocalDateTime? LastLoginOn { get; set; }
        public bool Active { get; set; }
        public LocalDateTime AddedOn { get; }
        public string Email { get; set; }
        public LocalDateTime? UpdatedOn { get; set; }
    }
}
