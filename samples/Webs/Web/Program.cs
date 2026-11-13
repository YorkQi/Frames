using Frame.Core;
using Frame.Core.Application;
using Frame.Core.Modules;
using Frame.Databases;
using Frame.EventBus;
using Frame.Locks;
using Frame.Locks.Enums;
using Infrastructure.DatabaseContexts;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

string[] querys = builder.Configuration.GetRequiredSection("DBConnections:Query").Get<string[]>() ?? throw new Exception("未设置连接字符串");
string[] commands = builder.Configuration.GetRequiredSection("DBConnections:Command").Get<string[]>() ?? throw new Exception("未设置连接字符串");
string[] redisQuerys = builder.Configuration.GetRequiredSection("RedisDBConnections:Query").Get<string[]>() ?? throw new Exception("未设置连接字符串");
string[] redisCommands = builder.Configuration.GetRequiredSection("RedisDBConnections:Command").Get<string[]>() ?? throw new Exception("未设置连接字符串");
ArgumentNullException.ThrowIfNull(querys);
ArgumentNullException.ThrowIfNull(commands);
ArgumentNullException.ThrowIfNull(redisQuerys);
ArgumentNullException.ThrowIfNull(redisCommands);

builder.Services.AddFrameService(option =>
{
    option.UseApplication();
    option.UseModule();
    option.UseDatabaseContext<CommandDatabaseContext>([.. querys]);
    option.UseDatabaseContext<QueryDatabaseContext>([.. commands]);
    //option.UseRedisDatabase<CommandRedisContext>([.. redisCommands]);
    //option.UseRedisDatabase<QueryRedisContext>([.. redisQuerys]);
    option.UseLock(LockType.Local);
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
