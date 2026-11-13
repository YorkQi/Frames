using System.ComponentModel.DataAnnotations;

namespace Application.Projects.Dtos
{
    /// <summary>创建项目入参。</summary>
    public sealed class CreateProjectInput
    {
        /// <summary>项目编码（唯一）</summary>
        [Required(ErrorMessage = "项目编码不能为空。")]
        [StringLength(32, ErrorMessage = "项目编码不能超过 32 个字符。")]
        public string Code { get; set; } = string.Empty;

        /// <summary>项目名称</summary>
        [Required(ErrorMessage = "项目名称不能为空。")]
        [StringLength(128, ErrorMessage = "项目名称不能超过 128 个字符。")]
        public string Name { get; set; } = string.Empty;

        /// <summary>项目描述</summary>
        [StringLength(512, ErrorMessage = "项目描述不能超过 512 个字符。")]
        public string? Description { get; set; }
    }
}
