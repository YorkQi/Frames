using System;
using System.Collections.Generic;

namespace Application.Projects.Dtos
{
    /// <summary>项目视图模型；列表接口不返回任务，详情接口返回。</summary>
    public sealed record ProjectDto(
        int Id,
        string Code,
        string Name,
        string? Description,
        string OwnerId,
        string Status,
        DateTime CreationTime,
        DateTime? LastModificationTime,
        IReadOnlyList<TaskItemDto> Tasks);
}
