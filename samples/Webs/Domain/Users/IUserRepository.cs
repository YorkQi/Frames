﻿using Domain.Users;
using Frame.Core;
using Frame.Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public interface IUserRepository :
        IRepository<int, User>,
        IScopedInstance
    {
        Task<IEnumerable<User>> QueryAsync();
    }
}