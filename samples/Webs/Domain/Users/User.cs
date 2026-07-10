using System.ComponentModel.DataAnnotations.Schema;
using Domain.Users.Enums;
using Frame.Core.Entities;

namespace Domain.Users
{
    [Table("Users")]
    public class User : Entity<int>
    {
        public string Name { get; set; } = string.Empty;

        public UserSex Sex { get; set; }

        public string ProfilePicture { get; set; } = string.Empty;

        public long CreateTime { get; set; }
    }
}
