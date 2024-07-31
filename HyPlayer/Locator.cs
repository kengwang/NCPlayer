using HyPlayer.HyPlayControl;
using HyPlayer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;


namespace HyPlayer
{
    public class Locator
    {
        public static Locator Instance => _Instance ?? (_Instance = new Locator());
        private static Locator _Instance;

        private IServiceProvider _services;

        public T GetService<T>()
            where T : class
        {
            if (_services.GetService(typeof(T)) is not T service)
            {
                throw new Exception($"{typeof(T)} needs to be regiestered in ConfigureServices.");
            }

            return service;
        }

        public Locator()
        {
            var _servicesCollection = new ServiceCollection();
            _servicesCollection.AddTransient<HistoryManagement>();
            _services = _servicesCollection.BuildServiceProvider();

        }

    }
}
    

