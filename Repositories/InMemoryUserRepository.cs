using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagementAPI.Models;

namespace UserManagementAPI.Repositories
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new();

        public InMemoryUserRepository()
        {
            // Seed with a sample user
            var sample = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@example.com",
                Phone = "123-456-7890"
            };
            _users[sample.Id] = sample;
        }

        public Task AddAsync(User user)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            return Task.FromResult(_users.TryRemove(id, out _));
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {
            // Return a snapshot to avoid enumeration issues
            var snapshot = _users.Values.ToList().AsReadOnly();
            return Task.FromResult((IEnumerable<User>)snapshot);
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task<bool> UpdateAsync(User user)
        {
            if (!_users.ContainsKey(user.Id)) return Task.FromResult(false);
            _users[user.Id] = user;
            return Task.FromResult(true);
        }
    }
}
