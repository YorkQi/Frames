using Domain.Users.Events;
using Frame.EventBus;

namespace Web.EventBus
{
    /// <summary>
    /// 领域事件处理器示例：用户创建成功后写一条日志。
    /// UseEventBus() 会扫描并自动注册所有 IEventHandler&lt;TEvent&gt; 实现。
    /// </summary>
    public sealed class UserRegisteredDomainEventHandler(ILogger<UserRegisteredDomainEventHandler> logger)
        : IEventHandler<UserRegisteredDomainEvent>
    {
        /// <inheritdoc />
        public Task ExecuteAsync(UserRegisteredDomainEvent param)
        {
            logger.LogInformation("收到领域事件：新用户“{Name}”已创建", param.Name);
            return Task.CompletedTask;
        }
    }
}
