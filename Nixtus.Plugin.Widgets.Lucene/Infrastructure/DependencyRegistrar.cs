using Microsoft.Extensions.DependencyInjection;
using Nixtus.Plugin.Widgets.Lucene.Services;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Web.Factories;

namespace Nixtus.Plugin.Widgets.Lucene.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, AppSettings appSettings)
        {
            services.AddScoped<ILuceneService, LuceneService>();
            services.AddScoped<ICatalogModelFactory, Factories.CatalogModelFactory>();
        }

        public int Order
        {
            get { return 500; }
        }
    }
}
