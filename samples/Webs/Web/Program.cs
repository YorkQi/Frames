using Application;
using Frame.Repository.DBContexts;
using Repository;
using Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddFrameCore();
builder.Services.AddEventBus<WebModule>();
builder.Services.AddApplication<ApplicationModule>();

//builder.Services.AddRedisLock(new RedisOptions{
//    $"IP:6379,password=密码,connectTimeout=1000,connectRetry=1,syncTimeout=1000"
//});

builder.Services.AddRepository<RepositoryModule>(option =>
{
    option.UseDatabaseContext<CommandDatabaseContext>(new DBConnectionString{
        "Database=数据库名;Data Source=数据库IP;User Id=数据库账号;Password=数据库密码;pooling=true;CharSet=utf8;port=数据库端口;Allow User Variables=True",
    });
}).AddMysql<RepositoryModule>();

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
