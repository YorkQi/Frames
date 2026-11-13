using System;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Tasks.Events;
using Frame.Core.Auditing;
using Frame.Core.Entities;

namespace Domain.Tasks
{
    /// <summary>任务实体：隶属于某个项目，完成后登记“任务已完成”事件。</summary>
    [Table("TaskItems")]
    public class TaskItem : Entity<int>, IFullAuditedObject
    {
        /// <summary>所属项目 Id</summary>
        public int ProjectId { get; private set; }

        /// <summary>任务标题</summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>任务状态</summary>
        public TaskItemStatus Status { get; private set; }

        /// <summary>截止时间（UTC）</summary>
        public DateTime? DueDate { get; private set; }

        /// <inheritdoc />
        public DateTime CreationTime { get; set; }

        /// <inheritdoc />
        public string? CreatorId { get; set; }

        /// <inheritdoc />
        public DateTime? LastModificationTime { get; set; }

        /// <inheritdoc />
        public string? LastModifierId { get; set; }

        /// <inheritdoc />
        public bool IsDeleted { get; set; }

        /// <inheritdoc />
        public DateTime? DeletionTime { get; set; }

        /// <inheritdoc />
        public string? DeleterId { get; set; }

        /// <summary>供 EF Core 物化使用。</summary>
        private TaskItem()
        {
        }

        /// <summary>创建任务。</summary>
        public static TaskItem Create(int projectId, string title, DateTime? dueDate)
        {
            if (projectId <= 0)
                throw new DomainException("项目 Id 无效。");

            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("任务标题不能为空。");

            return new TaskItem
            {
                ProjectId = projectId,
                Title = title.Trim(),
                Status = TaskItemStatus.Todo,
                DueDate = dueDate?.ToUniversalTime()
            };
        }

        /// <summary>开始处理任务。</summary>
        public void Start()
        {
            if (Status == TaskItemStatus.Done)
                throw new DomainException("任务已完成，不能重新开始。");

            Status = TaskItemStatus.Doing;
        }

        /// <summary>完成任务并登记事件；重复完成视为领域错误。</summary>
        public void Complete()
        {
            if (Status == TaskItemStatus.Done)
                throw new DomainException("任务已完成，不能重复完成。");

            Status = TaskItemStatus.Done;
            AddDomainEvent(new TaskCompletedDomainEvent(ProjectId, Title));
        }
    }
}
