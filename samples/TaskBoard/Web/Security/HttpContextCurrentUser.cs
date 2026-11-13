using System.Security.Claims;
using Frame.Core.Auditing;

namespace Web.Security
{
    /// <summary>从 HttpContext 读取当前用户，供审计字段填充使用。</summary>
    public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        /// <inheritdoc />
        public string? Id => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? accessor.HttpContext?.User.FindFirstValue("sub");

        /// <inheritdoc />
        public string? Name => accessor.HttpContext?.User.Identity?.Name
            ?? accessor.HttpContext?.User.FindFirstValue("name");
    }
}
