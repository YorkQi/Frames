using Application;
using Frame.Repository.DBContexts;
using Repository;
using Web;


var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddFrameCore();
builder.Services.AddEventBus<WebModule>();
builder.Services.AddApplication<ApplicationModule>();

//builder.Services.AddRedisLock(new RedisOptions{
//    $"IP:6379,password=√‹¬Î,connectTimeout=1000,connectRetry=1,syncTimeout=1000"
//});

var queryConnectionString = configuration.GetRequiredSection("DBConnectionStrings:Query").Get<string[]>() ?? throw new ArgumentNullException("Query¡¨Ω”¥ÆŒ¥’“µΩ");
builder.Services.AddRepository<RepositoryModule>(option =>
{
    option.UseDatabaseContext<CommandDatabaseContext>(new DBConnectionString(queryConnectionString));
    option.UseDatabaseContext<QueryDatabaseContext>(new DBConnectionString(queryConnectionString));
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
