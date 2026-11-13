using Frame.EventBus;

namespace Domain.Tasks.Events
{
    /// <summary>任务已完成。</summary>
    public sealed class TaskCompletedDomainEvent(int projectId, string title) : IEvent
    {
        /// <summary>所属项目 Id</summary>
        public int ProjectId { get; } = projectId;

        /// <summary>任务标题</summary>
        public string Title { get; } = title;
    }
}
