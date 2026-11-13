using Frame.EventBus;

namespace Domain.Projects.Events
{
    /// <summary>项目已归档。</summary>
    public sealed class ProjectArchivedDomainEvent(string code) : IEvent
    {
        /// <summary>项目编码</summary>
        public string Code { get; } = code;
    }
}
