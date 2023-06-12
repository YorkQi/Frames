# Frams（DDD手脚架）
本框架采用DDD领域模型来构建手脚架。框架拥有AspCore增强、EventBus事件总线、分布式缓存（目前采用的是redis，后续会尝试Dragonfly）、仓储（目前使用的Dapper和EFCore）、定时执行任务

### AspCore增强

AspCore增强加入异常操作Filter（ExceptionFilter）和参数验证Filter（ModelValidattionFilter）

##### 使用方式（.net 7.0）：

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAspNetCore();
```

### EventBus事件总线

EventBus事件总线用于非阻塞数据处理，目前设计思路是使用了三种模式（LocalCache 本地缓存，Redis缓存，RabbitMQ队列）

##### 使用方式，参数不传也行默认LocalCache（.net 7.0）：

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEvent(EventMode.LocalCache);
```

##### 项目使用

写入事件总线

```c#
		private readonly IEventFactory factory;

        public HomeController(IEventFactory factory)
        {
            this.factory = factory;
        }

        public IActionResult Index()
        {
            factory.Push(new TestEvent<TestEventHandler>
            {
                Name = "Test"
            });
            return View();
        }
```



TestEvent.cs 事件总线参数类

```c#
public class TestEvent<TEventHandler> : IEvent
        where TEventHandler : IEventHandler
    {
        public string Name { get; set; } = string.Empty;
    }
```

TestEventHandler.cs 事件总线处理类

```c#
public class TestEventHandler : IEventHandler
    {

        public Task ExecuteAsync(IEvent param)
        {
            return Task.CompletedTask;
        }
    }
```

### 分布式缓存（包含分布式缓存锁）

分布式缓存用于分布式系统架构中解决读的慢的问题，懂的都懂 目前使用的是redis,内置使用的redlock分布式缓存锁

##### 缓存锁使用方式（.net 7.0）

```c#
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisLock(new RedisOptions{
    $"IP:6379,password=密码,connectTimeout=1000,connectRetry=1,syncTimeout=1000"
});

```

##### 项目使用

```c#
        private readonly IRedisLockFactory lockFactory;

        public HomeController(IRedisLockFactory lockFactory)
        {
            this.lockFactory = lockFactory;
        }

        public IActionResult Index()
        {
            using (var redisLock = lockFactory.CreateLock("锁key"))
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
builder.Services.AddRepository<RepositoryModule>(option =>
{
    option.UseResposityContext<RespositoryContext>(new DBConnectionStr{
        "Database=数据库名;Data Source=数据库IP;User Id=数据库账号;Password=数据库密码;pooling=true;CharSet=utf8;port=数据库端口;Allow User Variables=True",
    });
}).AddMysql();
```

###### RepositoryModule：如果仓储是其他程序集则需要注入模块

```c#
public class RepositoryModule : IModule
    {

    }
```



##### 仓储接口和类

此方式由框架提供基础的增、删、查、改及批量增、删、查、改，并能新增自己对此仓储的操作

##### 接口

```c#
public interface IAccountRepository :
        IRepository<int, Account>,
        IScopedInstance
    {

    }
```

##### 类

```c#
public class AccountRepository :
        Repository<int, Account>,
        IAccountRepository
    {

    }
```

待续。。。。。。

##### 获取仓储

能获取框架提供的基础增删查改及项目中新增对仓储的操作

```c#
        private readonly RespositoryContext respository;

        public HomeController(RespositoryContext respository)
        {
            this.respository = respository;
        }

        public IActionResult Index()
        {
            var repo2 = respository.Get<IAccountRepository>();
            var account2 = repo2.GetAsync(1);
            
            var repo = respository.Get<int, account>();
            var account = repo.GetAsync(1);

            return View();
        }
```

待续。。。

### 定时执行任务

系统中的有需要定时执行的任务可以使用此框架、目前集成的是Quarzt,后续可能使用FluentScheduler或者自己手写

##### 使用方式（.net 7.0）

```c#
//注入调度计划
builder.Services.AddScheduler();

var app = builder.Build();


#region 第一种方式 获取所有继承IScheduler的公共类，PS:此类必须标记SchedulerCronAttribute特性
app.UseScheduler();
#endregion


#region  第二种方式 传参的方式
List<SchedulerOption> options = new()
{
    new SchedulerOption
    {
        SchedulerName = "Test",
        SchedulerAssmbly = "Job.Tasks.TestScheduler",
        Cron = "0/5 * * * * ?"
    }
};
app.UseScheduler(options);
#endregion

```

##### 第一种方式

```c#
[SchedulerCron("0/5 * * * * ?")]
    public class Test3Scheduler : IScheduler
    {
        public Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
```

##### 第二种方式

```c#
public class TestScheduler : IScheduler
    {
        public Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
```

