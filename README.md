# Frams（DDD手脚架）
本框架采用DDD领域模型来构建手脚架。框架拥有AspCore增强、EventBus事件总线、分布式缓存（目前采用的是redis，后续会尝试Dragonfly）、仓储（目前使用的Dapper和EFCore）、定时执行任务

### AspCore增强

AspCore增强加入异常操作Filter（ExceptionFilter）和参数验证Filter（ModelValidattionFilter）

##### 使用方式（.net 7.0）：

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFrameService();
```

### EventBus事件总线

EventBus事件总线用于非阻塞数据处理，目前设计思路是使用了三种模式（LocalCache 本地缓存，Redis缓存，RabbitMQ队列）

##### 使用方式，参数不传也行默认LocalCache（.net 7.0）：

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFrameService(option =>
{
    option.UseEventBus();
});
```

##### 项目使用

写入事件总线

```c#
private readonly IEventBus eventBus;

public HomeController(IEventBus eventBus)
{
    this.eventBus = eventBus;
}

public IActionResult Index()
{
    await eventBus.Push(new TestEvent
    {
         Name = "Test"
    });
    return View();
}
```



TestEvent.cs 事件总线参数类

```c#
public class TestEvent : IEvent
{
    public string Name { get; set; } = string.Empty;
}
```

TestEventHandler.cs 事件总线处理类

```c#
public class TestEventHandler : IEventHandler<TestEvent>
{
    public Task ExecuteAsync(TestEvent param)
    {
        Debug.WriteLine(param.Name);
        Console.WriteLine(param.Name);
        return Task.CompletedTask;
    }
}
```

### 分布式缓存（包含分布式缓存锁）

分布式缓存用于分布式系统架构中解决读的慢的问题，懂的都懂 目前使用的是redis,内置使用的redlock分布式缓存锁

##### 缓存锁使用方式（.net 7.0）

```c#
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFrameService(option =>
{
    option.UseRedisDatabase<CommandRedisContext>(new RedisConnection($"IP:6379,password=密码,connectTimeout=1000,connectRetry=1,syncTimeout=1000"));
});

```

##### 项目使用

```c#
private readonly CommandRedisContext redisContext;

public HomeController(CommandRedisContext redisContext)
{
    this.redisContext = redisContext;
}

public async Task<IActionResult> Index()
{
    var locks = redisContext.GetLock();
    using (IRedisLock redisLock = locks.CreateLock("锁key"))
    {
        if (redisLock.IsAcquired)
        {

        }
    }
    return View();
}
```

待续。。。。

### 仓储

仓储是DDD中一个比较薄弱的一层，目前采用了两种方式来连接数据库操作数据（仓储接口和类、获取仓储）

##### 使用方式（.net 7.0）

```c#
builder.Services.AddFrameService(option =>
{
    option.UseDatabaseContext<CommandDatabaseContext>(new DBConnectionString(["Database=数据库名;Data Source=数据库IP;User Id=数据库账号;Password=数据库密码;pooling=true;CharSet=utf8;port=数据库端口;Allow User Variables=True"]));
    option.UseDatabaseContext<QueryDatabaseContext>(new DBConnectionString(["Database=数据库名;Data Source=数据库IP;User Id=数据库账号;Password=数据库密码;pooling=true;CharSet=utf8;port=数据库端口;Allow User Variables=True"]));
    option.UseMysql();
});
```

##### 仓储接口和类

此方式由框架提供基础的增、删、查、改及批量增、删、查、改，并能新增自己对此仓储的操作

##### 接口

```c#
public interface IUserRepository :
    IRepository<int, User>
{
    Task<IEnumerable<User>> QueryAsync();
}
```

##### 类

```c#
public class UserRepository :
    Repository<int, User>,
    IUserRepository
{

    public Task<IEnumerable<User>> QueryAsync()
    {
        var sql = "SELECT * FROM `User` WHERE Id > 1 ; ";
        return DBContext.QueryAsync<User>(sql);
    }
}
```



##### 获取仓储

能获取框架提供的基础增删查改及项目中新增对仓储的操作

```c#
private readonly CommandDatabaseContext command;

public HomeController(CommandDatabaseContext command)
{
    this.command = command;
}

public async Task<IActionResult> Index()
{
    var repo2 = command.GetRepository<IUserRepository>();
    var user = await repo2.GetAsync(1);
    var users = await repo2.QueryAsync(new List<int> { 1 });
    user.Sex = UserSex.Man;

    var i1 = await repo2.InsertAsync(new User { Name = "York", Sex = UserSex.Man, ProfilePicture = "http://baidu.com", CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() });

    List<User> addUsers = new();
    for (int i = 0; i < 10000; i++)
    {
        addUsers.Add(new User { Name = "York", Sex = UserSex.Man, ProfilePicture = "http://baidu.com", CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() });
    }
    var i2 = await repo2.InsertBatchAsync(addUsers);

    var j1 = await repo2.UpdateAsync(user);
    var j2 = await repo2.UpdateBatchAsync(new List<User> { user });



    var ii1 = await repo2.DeleteAsync(7);

    var user2 = await repo2.QueryAsync();

    var ii2 = await repo2.DeleteBatchAsync(user2.Select(t => t.Id));

    var repo = command.GetRepository<int, User>();
    var user3 = await repo.GetAsync(1);
    return View();
}
```



### 定时执行任务

系统中的有需要定时执行的任务可以使用此框架、目前集成的是Quarzt,后续可能使用FluentScheduler或者自己手写

##### 使用方式（.net 7.0）

```c#
var builder = WebApplication.CreateBuilder(args);

//注入调度计划
#region 第一种方式 获取所有继承IScheduler的公共类，PS:此类必须标记SchedulerCronAttribute特性
builder.Services.AddFrameService(options =>
{
    options.UserScheduler();
});
#endregion

#region  第二种方式 传参的方式
List<SchedulerJobParam> schedulerOptions = new()
{
    new SchedulerJobParam
    {
        JobName = "Test",
        JobClassName = "Job.Tasks.TestScheduler",
        Cron = "0/5 * * * * ?"
    },
    new SchedulerJobParam
    {
        JobName = "Test2",
        JobClassName = "Job.Tasks.Test2Scheduler",
        Cron = "0/5 * * * * ?"
    }
};
builder.Services.AddFrameService(options =>
{
    options.UserScheduler(schedulerOptions);
});
#endregion

```

##### 第一种方式

```c#
[SchedulerCron("0/5 * * * * ?")]
public class Test3Scheduler : ISchedulerJob
{
    public Task ExecuteAsync()
    {
        Console.WriteLine("TestScheduler3");
        return Task.CompletedTask;
    }
}
```

##### 第二种方式

```c#
public class TestScheduler : ISchedulerJob
{
    public Task ExecuteAsync()
    {
        Console.WriteLine("TestScheduler");
        return Task.CompletedTask;
    }
}
```

