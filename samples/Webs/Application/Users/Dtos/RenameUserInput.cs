using System.ComponentModel.DataAnnotations;

namespace Application.Users.Dtos
{
    /// <summary>修改用户名称入参。</summary>
    public sealed class RenameUserInput
    {
        /// <summary>新名称</summary>
        [Required(ErrorMessage = "用户名称不能为空。")]
        [StringLength(64, ErrorMessage = "用户名称最长 64 个字符。")]
        public string Name { get; set; } = string.Empty;
    }
}
