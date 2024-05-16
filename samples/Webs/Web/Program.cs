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
//    $"IP:6379,password=√‹¬Î,connectTimeout=1000,connectRetry=1,syncTimeout=1000"
//});

builder.Services.AddRepository<RepositoryModule>(option =>
{
    option.UseDatabaseContext<CommandDatabaseContext>(new DBConnectionStr{
        "Database=Frames;Data Source=mysql.toonline.com.cn;User Id=york;Password=york123;pooling=true;CharSet=utf8;port=5566;Allow User Variables=True",
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
