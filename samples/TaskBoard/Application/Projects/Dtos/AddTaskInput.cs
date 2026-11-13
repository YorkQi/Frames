using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Projects.Dtos
{
    /// <summary>新增任务入参。</summary>
    public sealed class AddTaskInput
    {
        /// <summary>任务标题</summary>
        [Required(ErrorMessage = "任务标题不能为空。")]
        [StringLength(200, ErrorMessage = "任务标题不能超过 200 个字符。")]
        public string Title { get; set; } = string.Empty;

        /// <summary>截止时间（可空，按 UTC 存储）</summary>
        public DateTime? DueDate { get; set; }
    }
}
