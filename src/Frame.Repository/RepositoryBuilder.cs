using Frame.Repository.Context;
using System;
using System.Collections.Generic;

namespace Frame.Repository
{
    public class RepositoryBuilder
    {
        private List<RepositoryConfiguration> Repositorys = new List<RepositoryConfiguration>();

        public void Add<Type>(Func<IServiceProvider, object> context)
        {
            Repositorys.Add(new RepositoryConfiguration { TypeKey = typeof(Type), TypeValue = context });
        }

        internal List<RepositoryConfiguration> Get()
        {
            return Repositorys;
        }

        public void UseResposityContext<TResposity>(ConnectionStr str) where TResposity : RespositoryContext, new()
        {
            Repositorys.Add(new RepositoryConfiguration
            {
                TypeKey = typeof(TResposity),
                TypeValue = (IServiceProvider provider) =>
                {
                    var resposity = new TResposity();
                    resposity.Initialize(provider, str);
                    return resposity;
                }
            });
        }
    }
}
