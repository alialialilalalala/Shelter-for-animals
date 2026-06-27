using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Core.Interfaces;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnimalShelterAI.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<user>, IUserRepository
    {
        public UserRepository(ShelterDbContext context) : base(context)
        {
        }

        public async Task<user?> GetByUsernameAsync(string username)
        {
            return await _context.users
                .Include(u => u.userroles)
                .ThenInclude(ur => ur.role)
                .FirstOrDefaultAsync(u => u.username == username);
        }

        public async Task<IEnumerable<user>> GetByRoleAsync(string roleName)
        {
            return await _context.users
                .Include(u => u.userroles)
                .ThenInclude(ur => ur.role)
                .Where(u => u.userroles.Any(ur => ur.role != null && ur.role.rolename == roleName))
                .ToListAsync();
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _context.users.AnyAsync(u => u.username == username);
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.users.AnyAsync(u => u.email == email);
        }

        public override async Task<user?> GetByIdAsync(int id)
        {
            return await _context.users
                .Include(u => u.userroles)
                .ThenInclude(ur => ur.role)
                .FirstOrDefaultAsync(u => u.userid == id);
        }
    }
}