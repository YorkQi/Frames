using Frame.EventBus;
using Frame.Redis.Locks;
using Frame.Repository.Context;
using Frame.Repository;
using Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();


builder.Services.AddRedisLock(new RedisOptions{
                 $"IP:6379,password=����,connectTimeout=1000,connectRetry=1,syncTimeout=1000"
        });

builder.Services.AddAspNetCore();
builder.Services.AddEvent(EventMode.LocalCache);
//builder.Services.AddModuleAll();

builder.Services.AddEvent();

builder.Services.AddRepository<RepositoryModule>(option =>
{
    option.UseResposityContext<RespositoryContext>(new DBConnectionStr{
          "Database=���ݿ���;Data Source=���ݿ�IP;User Id=���ݿ��˺�;Password=���ݿ�����;pooling=true;CharSet=utf8;port=���ݿ�˿�;Allow User Variables=True",
    });
}).AddMysql();

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