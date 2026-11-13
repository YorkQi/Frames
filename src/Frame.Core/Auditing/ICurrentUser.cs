namespace Frame.Core.Auditing
{
    /// <summary>
    /// 当前用户访问器。审计字段中的创建人/修改人/删除人由它提供。
    /// 框架默认注册 <see cref="NullCurrentUser"/>（始终为 <c>null</c>），
    /// 应用可注册自己的实现来接入登录上下文。
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>当前用户唯一标识。未登录时为 <c>null</c>。</summary>
        string? Id { get; }

        /// <summary>当前用户显示名。未登录时为 <c>null</c>。</summary>
        string? Name { get; }
    }

    /// <summary>
    /// 默认的当前用户实现：始终返回 <c>null</c>。
    /// </summary>
    public sealed class NullCurrentUser : ICurrentUser
    {
        /// <inheritdoc />
        public string? Id => null;

        /// <inheritdoc />
        public string? Name => null;
    }
}
