using Autofac;
using Nixtus.Plugin.Widgets.Lucene.Services;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Web.Factories;

namespace Nixtus.Plugin.Widgets.Lucene
{
    public class DependencyRegister : IDependencyRegistrar
    {
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<LuceneService>().As<ILuceneService>().InstancePerLifetimeScope();

            builder.RegisterType<Nixtus.Plugin.Widgets.Lucene.Factories.CatalogModelFactory>()
                .As<ICatalogModelFactory>().InstancePerLifetimeScope();
        }

        public int Order
        {
            get { return 500; }
        }
    }
}
