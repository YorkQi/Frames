using Frame.Core;
using Frame.Core.Locks;
using Frame.Databases;
using Frame.Redis;
using Infrastructure.DatabaseContexts;
using Infrastructure.RedisContexts;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var querys = builder.Configuration.GetRequiredSection("DBConnections:Query").Get<string[]>();
var commands = builder.Configuration.GetRequiredSection("DBConnections:Command").Get<string[]>();
var redisQuerys = builder.Configuration.GetRequiredSection("RedisDBConnections:Query").Get<string[]>();
var redisCommands = builder.Configuration.GetRequiredSection("RedisDBConnections:Command").Get<string[]>();
Check.NotNull(querys, nameof(querys));
Check.NotNull(commands, nameof(commands));
Check.NotNull(redisQuerys, nameof(redisQuerys));
Check.NotNull(redisCommands, nameof(redisCommands));

builder.Services.AddFrameService(option =>
{
    option.UseDatabaseContext<CommandDatabaseContext>([.. querys]);
    option.UseDatabaseContext<QueryDatabaseContext>([.. commands]);
    option.UseRedisDatabase<CommandRedisContext>([.. redisCommands]);
    option.UseRedisDatabase<QueryRedisContext>([.. redisQuerys]);
    option.UseLock(LockType.Local);
    option.UseDapperMysql();
    option.UseEventBus();
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();



app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
