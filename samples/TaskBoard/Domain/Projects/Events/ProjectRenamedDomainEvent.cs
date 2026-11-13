using Frame.EventBus;

namespace Domain.Projects.Events
{
    /// <summary>项目已改名。</summary>
    public sealed class ProjectRenamedDomainEvent(string code, string name) : IEvent
    {
        /// <summary>项目编码</summary>
        public string Code { get; } = code;

        /// <summary>新名称</summary>
        public string Name { get; } = name;
    }
}
