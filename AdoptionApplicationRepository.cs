using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Core.Interfaces;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnimalShelterAI.Infrastructure.Repositories
{
    public class AdoptionApplicationRepository : BaseRepository<adoptionapplication>, IAdoptionApplicationRepository
    {
        public AdoptionApplicationRepository(ShelterDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<adoptionapplication>> GetPendingApplicationsAsync()
        {
            return await _context.adoptionapplications
                .Include(a => a.animal)
                .Include(a => a.user)
                .Where(a => a.status == "Pending")
                .OrderByDescending(a => a.applicationdate)
                .ToListAsync();
        }

        public async Task<IEnumerable<adoptionapplication>> GetByUserIdAsync(int userId)
        {
            return await _context.adoptionapplications
                .Include(a => a.animal)
                .ThenInclude(animal => animal.type)
                .Include(a => a.animal)
                .ThenInclude(animal => animal.breed)
                .Where(a => a.userId == userId)
                .OrderByDescending(a => a.applicationdate)
                .ToListAsync();
        }

        public async Task<IEnumerable<adoptionapplication>> GetByAnimalIdAsync(int animalId)
        {
            return await _context.adoptionapplications
                .Include(a => a.user)
                .Where(a => a.animalId == animalId)
                .OrderByDescending(a => a.applicationdate)
                .ToListAsync();
        }

        public override async Task<adoptionapplication?> GetByIdAsync(int id)
        {
            return await _context.adoptionapplications
                .Include(a => a.animal)
                .ThenInclude(animal => animal.type)
                .Include(a => a.animal)
                .ThenInclude(animal => animal.breed)
                .Include(a => a.user)
                .Include(a => a.manager)
                .FirstOrDefaultAsync(a => a.applicationId == id);
        }
    }
}