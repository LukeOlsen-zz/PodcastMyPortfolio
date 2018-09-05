using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IClientAccountActivityTypeService
    {
        IEnumerable<ClientAccountActivityType> GetAll();
    }

    public class ClientAccountActivityTypeService : IClientAccountActivityTypeService
    {
        private DataContext _context;
        private IClock _clock;

        public ClientAccountActivityTypeService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public IEnumerable<ClientAccountActivityType> GetAll()
        {
            var q = _context.ClientAccountActivityTypes.ToList();
            return q;
        }
    }
}
