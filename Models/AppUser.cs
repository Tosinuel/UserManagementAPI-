using System;
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models
{
    public class AppUser
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? Role { get; set; }
    }
}
