using System.ComponentModel.DataAnnotations;

namespace Application.Projects.Dtos
{
    /// <summary>项目改名入参。</summary>
    public sealed class RenameProjectInput
    {
        /// <summary>新名称</summary>
        [Required(ErrorMessage = "项目名称不能为空。")]
        [StringLength(128, ErrorMessage = "项目名称不能超过 128 个字符。")]
        public string Name { get; set; } = string.Empty;
    }
}
