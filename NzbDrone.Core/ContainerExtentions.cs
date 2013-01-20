﻿using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
using NLog;
using NzbDrone.Common;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Instrumentation;
using NzbDrone.Core.Providers.ExternalNotification;
using NzbDrone.Core.Providers.Indexer;
using NzbDrone.Core.Providers.Metadata;
using NzbDrone.Core.Providers.Search;
using PetaPoco;

namespace NzbDrone.Core
{
    public static class ContainerExtentions
    {

        private static readonly Logger logger = LogManager.GetLogger("ServiceRegistration");

        public static void RegisterCoreServices(this ContainerBuilder containerBuilder)
        {
            var core = Assembly.Load("NzbDrone.Core");
            var common = Assembly.Load("NzbDrone.Common");

            containerBuilder.RegisterAssembly(core);
            containerBuilder.RegisterAssembly(common);

            containerBuilder.InitDatabase();

            containerBuilder.RegisterModule<LogInjectionModule>();
        }


        private static void RegisterAssembly(this ContainerBuilder container, Assembly assembly)
        {
            logger.Info("Registering Services from {0}", assembly.FullName);

            container.RegisterAssemblyTypes(assembly)
                     .AsSelf()
                     .SingleInstance();

            container.RegisterAssemblyTypes(assembly)
                     .AsImplementedInterfaces()
                     .SingleInstance();

            container.RegisterAssemblyTypes(assembly)
                     .Where(t => t.IsSubclassOf(typeof(IndexerBase)))
                     .As<IndexerBase>().SingleInstance();

            container.RegisterAssemblyTypes(assembly)
                      .Where(t => t.IsSubclassOf(typeof(SearchBase)))
                     .As<SearchBase>().SingleInstance();

            container.RegisterAssemblyTypes(assembly)
                      .Where(t => t.IsSubclassOf(typeof(ExternalNotificationBase)))
                     .As<ExternalNotificationBase>().SingleInstance();

            container.RegisterAssemblyTypes(assembly)
                     .Where(t => t.IsSubclassOf(typeof(MetadataBase)))
                     .As<MetadataBase>().SingleInstance();
        }

        private static void InitDatabase(this ContainerBuilder container)
        {
            logger.Info("Registering Database...");

            var appDataPath = new EnvironmentProvider().GetAppDataPath();
            if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);

            container.Register(c => c.Resolve<ConnectionFactory>().GetMainPetaPocoDb())
                     .As<IDatabase>();

            container.Register(c => c.Resolve<ConnectionFactory>().GetLogPetaPocoDb(false))
                     .SingleInstance()
                     .Named<IDatabase>("DatabaseTarget");

            container.Register(c => c.Resolve<ConnectionFactory>().GetLogPetaPocoDb())
                     .Named<IDatabase>("LogProvider");

            container.RegisterType<DatabaseTarget>().WithParameter(ResolvedParameter.ForNamed<IDatabase>("DatabaseTarget"));
            container.RegisterType<LogProvider>().WithParameter(ResolvedParameter.ForNamed<IDatabase>("LogProvider"));
        }
    }
}