using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IClientMessageTypeService
    {
        IEnumerable<ClientMessageType> GetAll();
    }

    public class ClientMessageTypeService : IClientMessageTypeService
    {
        private DataContext _context;
        private IClock _clock;

        public ClientMessageTypeService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public IEnumerable<ClientMessageType> GetAll()
        {
            var q = _context.ClientMessageTypes.ToList();
            return q;
        }
    }
}
