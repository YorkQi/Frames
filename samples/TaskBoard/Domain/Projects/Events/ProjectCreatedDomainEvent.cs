using Frame.EventBus;

namespace Domain.Projects.Events
{
    /// <summary>项目已创建（领域事件，实现 IEvent 后才会被事件总线消费）。</summary>
    public sealed class ProjectCreatedDomainEvent(string code, string name) : IEvent
    {
        /// <summary>项目编码</summary>
        public string Code { get; } = code;

        /// <summary>项目名称</summary>
        public string Name { get; } = name;
    }
}
