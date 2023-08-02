using Frame.Redis.Locks;
using Frame.Repository;
using Frame.Repository.Context;
using Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddFrameCore();
builder.Services.AddRedisLock(new RedisOptions{
    $"IP:6379,password=密码,connectTimeout=1000,connectRetry=1,syncTimeout=1000"
});
builder.Services.AddEvent();
builder.Services.AddMysql().AddRepository<RepositoryModule>(option =>
{
    option.UseResposityContext<RespositoryContext>(new ConnectionStr{
        "Database=数据库名;Data Source=数据库IP;User Id=数据库账号;Password=数据库密码;pooling=true;CharSet=utf8;port=数据库端口;Allow User Variables=True",
    });
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
