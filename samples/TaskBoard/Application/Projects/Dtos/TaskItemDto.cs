using System;

namespace Application.Projects.Dtos
{
    /// <summary>任务视图模型。</summary>
    public sealed record TaskItemDto(
        int Id,
        int ProjectId,
        string Title,
        string Status,
        DateTime? DueDate);
}
