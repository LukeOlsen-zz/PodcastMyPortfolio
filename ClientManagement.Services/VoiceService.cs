using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;

namespace ClientManagement.Services
{
    public interface IVoiceService
    {
        Voice Get(int id);
        IEnumerable<Voice> GetAll();
    }

    public class VoiceService : IVoiceService
    {
        private DataContext _context;

        public VoiceService(DataContext context)
        {
            _context = context;
        }

        public Voice Get(int id)
        {
            return _context.Voices.Find(id);
        }

        public IEnumerable<Voice> GetAll()
        {
            return _context.Voices;
        }
    }
}
