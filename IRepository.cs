using AnimalShelterAI.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AnimalShelterAI.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<int> CountAsync();
    }

    public interface IAnimalRepository : IRepository<animal>
    {
        Task<IEnumerable<animal>> GetAvailableAnimalsAsync();
        Task<IEnumerable<animal>> GetByTypeAsync(int typeId);
        Task<IEnumerable<animal>> GetByStatusAsync(string status);
        Task<IEnumerable<animal>> SearchAsync(string searchTerm);
        new Task<animal?> GetByIdAsync(int id);
    }

    public interface IUserRepository : IRepository<user>
    {
        Task<user?> GetByUsernameAsync(string username);
        Task<IEnumerable<user>> GetByRoleAsync(string roleName);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<bool> IsEmailTakenAsync(string email);
    }

    public interface IAdoptionApplicationRepository : IRepository<adoptionapplication>
    {
        Task<IEnumerable<adoptionapplication>> GetPendingApplicationsAsync();
        Task<IEnumerable<adoptionapplication>> GetByUserIdAsync(int userId);
        Task<IEnumerable<adoptionapplication>> GetByAnimalIdAsync(int animalId);
        new Task<adoptionapplication?> GetByIdAsync(int id);
    }

    public interface IMedicalRecordRepository : IRepository<medicalrecord>
    {
        Task<IEnumerable<medicalrecord>> GetByAnimalIdAsync(int animalId);
        Task<IEnumerable<medicalrecord>> GetByVetIdAsync(int vetId);
    }

    public interface IVolunteerTaskRepository : IRepository<volunteertask>
    {
        Task<IEnumerable<volunteertask>> GetByVolunteerIdAsync(int volunteerId);
        Task<IEnumerable<volunteertask>> GetPendingTasksAsync();
        Task<IEnumerable<volunteertask>> GetOverdueTasksAsync();
    }
}