using Frame.Core.Auditing;
using Infrastructure;
using Web.Filters;
using Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.AddHttpContextAccessor();

// 组合根：数据库上下文、仓储、应用服务、事件总线、本地锁。
builder.Services.AddTaskBoard(builder.Configuration);

// 必须放在 AddTaskBoard 之后：框架默认注册 NullCurrentUser，后注册的实现会覆盖它。
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.Run();
