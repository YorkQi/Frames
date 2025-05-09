﻿using Domain.Users;
using Domain.Users.Enums;
using Frame.EventBus;
using Frame.Redis.Locks;
using Infrastructure.DatabaseContexts;
using Microsoft.AspNetCore.Mvc;
using Repository;
using System.Diagnostics;
using Web.EventBus;
using Web.Models;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly CommandDatabaseContext command;
        private readonly IUserService service;
        private readonly IEventBus eventBus;
        private readonly CommandRedisContext redisContext;
        private readonly ILockFactory lockFactory;

        public HomeController(
            IEventBus eventBus,
            CommandDatabaseContext command,
            IUserService service,
            CommandRedisContext redisContext,
            ILockFactory lockFactory)
        {
            this.eventBus = eventBus;
            this.command = command;
            this.service = service;
            this.redisContext = redisContext;
            this.lockFactory = lockFactory;
        }

        public async Task<IActionResult> Index()
        {
            var sss = await service.LoginAsync();
            using (ILock redisLock = lockFactory.CreateLock("锁key"))
            {
                if (redisLock.IsAcquired)
                {

                }
            }
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


            await eventBus.Push(new TestEvent
            {
                Name = "Test"
            });

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    [Flags]
    public enum ServiceCode
    {
        /// <summary>
        /// 教师
        /// </summary>
        Teach = (int)PlatformType.Education << 0x20 | ServiceType.Teach << 0x10,
    }

    public enum PlatformType
    {
        /// <summary>
        /// 管理
        /// </summary>
        Manage = 0x01,

        /// <summary>
        /// 学生
        /// </summary>
        Consume = 0x02,

        /// <summary>
        /// 教学
        /// </summary>
        Education = 0x03
    }

    public enum ServiceType
    {
        /// <summary>
        /// 系统
        /// </summary>
        System = 0x01,

        /// <summary>
        /// 购买
        /// </summary>
        Consume = 0x02,

        /// <summary>
        /// 教师
        /// </summary>
        Teach = 0x03
    }
}