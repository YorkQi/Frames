using Frame.Core;
using Frame.Core.Application;
using Frame.Core.Auditing;
using Frame.Core.Modules;
using Frame.Databases;
using Frame.EventBus;
using Frame.Locks;
using Frame.Locks.Enums;
using Frame.Redis;
using Infrastructure.DatabaseContexts;
using Infrastructure.RedisContexts;
using Web.Filters;
using Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<ExceptionFilter>());
builder.Services.AddHttpContextAccessor();

// 连接串一节缺失或为空时尽早失败，并在消息里指出具体配置节。
string[] commands = ReadConnections(builder.Configuration, "DBConnections:Command");
string[] querys = ReadConnections(builder.Configuration, "DBConnections:Query");
string[] redisCommands = ReadConnections(builder.Configuration, "RedisDBConnections:Command");
string[] redisQuerys = ReadConnections(builder.Configuration, "RedisDBConnections:Query");

builder.Services.AddFrameService(options =>
{
    options.UseModule();
    options.UseApplication();

    // Command 用于写、Query 用于读；示例里两者可以指向同一个库。
    options.UseDatabaseContext<CommandDatabaseContext>(new ConnectionStringCollection(commands));
    options.UseDatabaseContext<QueryDatabaseContext>(new ConnectionStringCollection(querys));

    // Redis 示例：UserService 用 CommandRedisContext 做用户详情缓存。
    options.UseRedisDatabase<CommandRedisContext>(new RedisConnectionStringCollection(redisCommands));
    options.UseRedisDatabase<QueryRedisContext>(new RedisConnectionStringCollection(redisQuerys));

    options.UseLock(LockType.Local);
    options.UseEventBus();
});

// AddFrameService 注册的是 NullCurrentUser；在它之后注册就会覆盖为从 HttpContext 读取。
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.Run();

static string[] ReadConnections(IConfiguration configuration, string sectionPath)
{
    var values = configuration.GetSection(sectionPath).Get<string[]>();
    if (values is null || values.Length == 0)
    {
        throw new InvalidOperationException(
            $"配置节 {sectionPath} 未设置。请参考 appsettings.json 填写连接字符串后再启动。");
    }

    return values;
}
