using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Core.Interfaces;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnimalShelterAI.Infrastructure.Repositories
{
    public class AnimalRepository : BaseRepository<animal>, IAnimalRepository
    {
        public AnimalRepository(ShelterDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<animal>> GetAvailableAnimalsAsync()
        {
            return await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .Where(a => a.status == "Available")
                .OrderByDescending(a => a.admissiondate)
                .ToListAsync();
        }

        public async Task<IEnumerable<animal>> GetByTypeAsync(int typeId)
        {
            return await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .Where(a => a.typeid == typeId)
                .ToListAsync();
        }

        public async Task<IEnumerable<animal>> GetByStatusAsync(string status)
        {
            return await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .Where(a => a.status == status)
                .ToListAsync();
        }

        public async Task<IEnumerable<animal>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<animal>();

            return await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .Where(a => (a.name != null && a.name.Contains(searchTerm)) ||
                           (a.description != null && a.description.Contains(searchTerm)) ||
                           (a.color != null && a.color.Contains(searchTerm)))
                .ToListAsync();
        }

        public override async Task<animal?> GetByIdAsync(int id)
        {
            return await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .Include(a => a.medicalrecords)
                .Include(a => a.vaccinations)
                .FirstOrDefaultAsync(a => a.animalid == id);
        }

        public override async Task<IEnumerable<animal>> GetAllAsync()
        {
            return await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .OrderByDescending(a => a.admissiondate)
                .ToListAsync();
        }
    }
}