using Frame.Scheduler;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// 与 Web 共用同一个组合根，再追加调度器；[Scheduled] 标注的任务会被自动发现。
builder.Services.AddTaskBoard(builder.Configuration, options => options.UseScheduler());

var host = builder.Build();
await host.RunAsync();
