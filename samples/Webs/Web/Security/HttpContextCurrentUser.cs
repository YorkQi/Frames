using System.Security.Claims;
using Frame.Core.Auditing;

namespace Web.Security
{
    /// <summary>
    /// 从当前 HTTP 请求的 ClaimsPrincipal 读取操作人。
    /// 示例约定：sub/Jti 作为用户 Id，name 作为显示名；接入真实认证后按需调整。
    /// </summary>
    public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

        /// <inheritdoc />
        public string? Id => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Principal?.FindFirstValue("sub");

        /// <inheritdoc />
        public string? Name => Principal?.Identity?.Name
            ?? Principal?.FindFirstValue("name");
    }
}
