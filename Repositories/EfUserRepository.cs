using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagementAPI.Data;
using UserManagementAPI.Models;

namespace UserManagementAPI.Repositories
{
    public class EfUserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public EfUserRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _db.Users.FindAsync(id);
            if (entity is null) return false;
            _db.Users.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _db.Users.AsNoTracking().ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> UpdateAsync(User user)
        {
            var exists = await _db.Users.AnyAsync(u => u.Id == user.Id);
            if (!exists) return false;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
