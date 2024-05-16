using System;

namespace Frame.Repository.Databases
{
    internal class DatabaseContextConfiguration
    {
        internal DatabaseContextConfiguration(Type dataBaseContext, Func<IServiceProvider, object> dataBaseContextProvider)
        {
            DataBaseContextType = dataBaseContext;
            DataBaseContextProvider = dataBaseContextProvider;
        }
        internal Type DataBaseContextType { get; set; }

        internal Func<IServiceProvider, object> DataBaseContextProvider { get; set; }
    }
}
