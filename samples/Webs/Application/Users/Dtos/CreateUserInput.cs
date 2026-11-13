using System.ComponentModel.DataAnnotations;
using Domain.Users.Enums;

namespace Application.Users.Dtos
{
    /// <summary>创建用户入参。</summary>
    public sealed class CreateUserInput
    {
        /// <summary>用户名称（必填，最长 64 个字符）</summary>
        [Required(ErrorMessage = "用户名称不能为空。")]
        [StringLength(64, ErrorMessage = "用户名称最长 64 个字符。")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public UserSex Sex { get; set; }

        /// <summary>头像地址（可选）</summary>
        public string? ProfilePicture { get; set; }
    }
}
