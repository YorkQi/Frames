using Frame.EventBus;

namespace Domain.Users.Events
{
    /// <summary>
    /// 用户注册成功事件。
    /// 实现 <see cref="IEvent"/> 后，才会被默认的 EventBus 领域事件分发器投递；
    /// 分发时机是工作单元提交成功之后。
    /// </summary>
    public sealed class UserRegisteredDomainEvent(string name) : IEvent
    {
        /// <summary>用户名称（创建时还没有自增主键，因此不带 Id）</summary>
        public string Name { get; } = name;
    }
}
