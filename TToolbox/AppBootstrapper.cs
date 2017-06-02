namespace TToolbox
{
    using Caliburn.Micro;
    using Caliburn.Micro.Logging.NLog;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Windows.Threading;

    public class AppBootstrapper : BootstrapperBase
    {
        private SimpleContainer container;


        public AppBootstrapper()
        {
            Initialize();
            LogManager.GetLog = type => new NLogLogger(type);
        }

        protected override void Configure()
        {
            container = new SimpleContainer();

            container.Singleton<IWindowManager, WindowManager>();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.PerRequest<IShell, ShellViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            var instance = container.GetInstance(service, key);
            if (instance != null)
                return instance;

            throw new InvalidOperationException("Could not locate any instances.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            DisplayRootViewFor<IShell>();
        }

        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //base.OnUnhandledException(sender, e);
            ILog logger = LogManager.GetLog(typeof(ShellViewModel));
            logger.Error(e.Exception);
            e.Handled = true;
        }

        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            return new[] 
            {
                Assembly.GetExecutingAssembly()
            };
        }
    }
}