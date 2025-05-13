using Frame.Core.Lock;
using Frame.Redis;
using Frame.Redis.Locks;
using Frame.Repository;
using Frame.Repository.DBContexts;
using Infrastructure.DatabaseContexts;
using Repository;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var querys = builder.Configuration.GetRequiredSection("DBConnections:Query").Get<string[]>()
    ?? throw new ArgumentNullException("DBConnections:Query���Ӵ�δ�ҵ�");
var commands = builder.Configuration.GetRequiredSection("DBConnections:Command").Get<string[]>()
    ?? throw new ArgumentNullException("DBConnections:Command���Ӵ�δ�ҵ�");
var redisQuerys = builder.Configuration.GetRequiredSection("RedisDBConnections:Query").Get<string[]>()
    ?? throw new ArgumentNullException("RedisDBConnections:Query���Ӵ�δ�ҵ�");
var redisCommands = builder.Configuration.GetRequiredSection("RedisDBConnections:Command").Get<string[]>()
    ?? throw new ArgumentNullException("RedisDBConnections:Command���Ӵ�δ�ҵ�");

builder.Services.AddFrameService(option =>
{
    option.UseDatabaseContext<CommandDatabaseContext>(new DBConnectionString(querys));
    option.UseDatabaseContext<QueryDatabaseContext>(new DBConnectionString(commands));
    option.UseRedisDatabase<CommandRedisContext>(new RedisConnection(redisCommands));
    option.UseRedisDatabase<QueryRedisContext>(new RedisConnection(redisQuerys));
    option.UseLock(LockType.Local);
    option.UseMysql();
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
