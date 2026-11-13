using Frame.EventBus;

namespace Domain.Users.Events
{
    /// <summary>用户改名事件（提交后分发）。</summary>
    public sealed class UserRenamedDomainEvent(int userId, string name) : IEvent
    {
        /// <summary>用户主键</summary>
        public int UserId { get; } = userId;

        /// <summary>新名称</summary>
        public string Name { get; } = name;
    }
}
