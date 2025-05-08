using Frame.Redis;
using Frame.Redis.Locks;
using Frame.Repository;
using Frame.Repository.DBContexts;
using Infrastructure.DatabaseContexts;
using Repository;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var querys = builder.Configuration.GetRequiredSection("DBConnectionStrings:Query").Get<string[]>()
    ?? throw new ArgumentNullException("Query连接串未找到");
var commands = builder.Configuration.GetRequiredSection("DBConnectionStrings:Command").Get<string[]>()
    ?? throw new ArgumentNullException("Command连接串未找到");

builder.Services.AddFrameService(option =>
{
    option.UseDatabaseContext<CommandDatabaseContext>(new DBConnectionString(querys));
    option.UseDatabaseContext<QueryDatabaseContext>(new DBConnectionString(commands));
    option.UseRedisDatabase<CommandRedisContext>(new RedisConnection($"IP:6379,password=密码,connectTimeout=1000,connectRetry=1,syncTimeout=1000"));
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
