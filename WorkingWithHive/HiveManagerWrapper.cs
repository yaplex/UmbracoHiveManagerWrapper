using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NHibernate;
using Umbraco.Framework;
using Umbraco.Framework.Context;
using Umbraco.Framework.Localization;
using Umbraco.Framework.Localization.Configuration;
using Umbraco.Framework.Persistence.Model.Attribution.MetaData;
using Umbraco.Framework.Persistence.NHibernate;
using Umbraco.Framework.Persistence.NHibernate.Dependencies;
using Umbraco.Framework.Persistence.NHibernate.OrmConfig;
using Umbraco.Framework.Persistence.ProviderSupport._Revised;
using Umbraco.Framework.Persistence.RdbmsModel.Mapping;
using Umbraco.Framework.Tasks;
using Umbraco.Framework.TypeMapping;
using Umbraco.Hive;
using Umbraco.Hive.Configuration;
using Umbraco.Hive.ProviderSupport;
using Configuration = NHibernate.Cfg.Configuration;

namespace WorkingWithHive
{
    using Umbraco.Cms.Web.Context;
    using Umbraco.Cms.Web.Mapping;

    public class HiveManagerWrapper
    {
        public IHiveManager GetHiveManager()
        {
            IFrameworkContext frameworkContext = GetFrameworkContext();

            var providerMetadata = new ProviderMetadata("p__nhibernate", new Uri("content://"), true, false);

            var builder =
                new NHibernateConfigBuilder(
                    ConfigurationManager.ConnectionStrings["DatabaseConnString"].ConnectionString,
                    "unit-tester", SupportedNHDrivers.MsSql2008, "call", false, false);

            NhConfigurationCacheKey cacheKey;
            Configuration config = builder.BuildConfiguration(false, out cacheKey);

            var nhHelper = new NhFactoryHelper(config, null, false, false, frameworkContext);

            ProviderDependencyHelper dependencyHelper = new DependencyHelper(nhHelper, providerMetadata);
            var revisionRepositoryFactory = new RevisionRepositoryFactory(providerMetadata, frameworkContext,
                                                                          dependencyHelper);

            var revisionSchemaSessionFactory = new NullProviderRevisionRepositoryFactory<EntitySchema>(providerMetadata,
                                                                                                       frameworkContext);
            var schemaRepositoryFactory = new SchemaRepositoryFactory(providerMetadata, revisionSchemaSessionFactory,
                                                                      frameworkContext, dependencyHelper);

            var entityRepositoryFactory = new EntityRepositoryFactory(providerMetadata, revisionRepositoryFactory,
                                                          schemaRepositoryFactory, frameworkContext,
                                                          dependencyHelper);

            ProviderSetup singleWriter = GetWriterProvider(providerMetadata, frameworkContext, entityRepositoryFactory);

            ReadonlyProviderSetup singleReader = GetReaderProvider(providerMetadata, revisionRepositoryFactory,
                                                                   schemaRepositoryFactory, frameworkContext,
                                                                   dependencyHelper, config);

            
            Func<AbstractMappingEngine> addTypemapper = () => new ManualMapperv2(new NhLookupHelper(entityRepositoryFactory), providerMetadata);
            frameworkContext.TypeMappers.Add(new Lazy<AbstractMappingEngine, TypeMapperMetadata>(addTypemapper, new TypeMapperMetadata(true)));
            IHiveManager hive = new HiveManager(new[]
                                                    {
                                                        new ProviderMappingGroup(
                                                            "test",
                                                            new WildcardUriMatch("content://"),
                                                            singleReader,
                                                            singleWriter,
                                                            frameworkContext)
                                                    }, frameworkContext);
            return hive;
        }

        private ReadonlyProviderSetup GetReaderProvider(ProviderMetadata providerMetadata,
                                                        RevisionRepositoryFactory revisionRepositoryFactory,
                                                        SchemaRepositoryFactory schemaRepositoryFactory,
                                                        IFrameworkContext frameworkContext,
                                                        ProviderDependencyHelper dependencyHelper, Configuration config)
        {
            AbstractReadonlyEntityRepositoryFactory readonlyEntityRepositoryFactory =
                new EntityRepositoryFactory(providerMetadata, revisionRepositoryFactory, schemaRepositoryFactory,
                                            frameworkContext, dependencyHelper);

            var readonlyUnitFactory = new ReadonlyProviderUnitFactory(readonlyEntityRepositoryFactory);
            AbstractProviderBootstrapper bootstrapper = new ProviderBootstrapper(config, null);
            var singleReader = new ReadonlyProviderSetup(readonlyUnitFactory, providerMetadata, frameworkContext,
                                                         bootstrapper, 0);
            return singleReader;
        }

        private ProviderSetup GetWriterProvider(ProviderMetadata providerMetadata,
                                                IFrameworkContext frameworkContext, EntityRepositoryFactory entityRepositoryFactory)
        {
            var unitFactory = new ProviderUnitFactory(entityRepositoryFactory);
            var singleWriter = new ProviderSetup(unitFactory, providerMetadata, frameworkContext, null, 0);
            return singleWriter;
        }

        private IFrameworkContext GetFrameworkContext()
        {
            TextManager textManager = LocalizationConfig.SetupDefault();
            MappingEngineCollection typeMappers = GetTypeMappers();

            AbstractScopedCache scopedCache = new DictionaryScopedCache();
            AbstractApplicationCache applicationCache = new HttpRuntimeApplicationCache();
            AbstractFinalizer finalizer = new NestedLifetimeFinalizer();
            var taskMgr = new ApplicationTaskManager(Enumerable.Empty<Lazy<AbstractTask, TaskMetadata>>());
            IFrameworkContext frameworkContext = new DefaultFrameworkContext(textManager, typeMappers, scopedCache,
                                                                             applicationCache, finalizer, taskMgr);

            return frameworkContext;
        }

        private MappingEngineCollection GetTypeMappers()
        {
            var webmModelMapper = new RdbmsModelMapper(null, null);
            var binders = new List<Lazy<AbstractMappingEngine, TypeMapperMetadata>>();
            var metadata = new TypeMapperMetadata(true);
            var bind = new Lazy<AbstractMappingEngine, TypeMapperMetadata>(() => webmModelMapper, metadata);
            binders.Add(bind);
/*
            binders.Add(new Lazy<AbstractMappingEngine, TypeMapperMetadata>(() => new ManualMapperv2(new NhLookupHelper(entityRepositoryFactory), providerMetadata),
                                                                            metadata));
*/


/*
            MapResolverContext resolverContext = new MapResolverContext();
            var cmsModelMapper = new RenderTypesModelMapper(resolverContext);
*/


            var typeMappers = new MappingEngineCollection(binders);
            typeMappers.Configure();
            return typeMappers;
        }
    }
}