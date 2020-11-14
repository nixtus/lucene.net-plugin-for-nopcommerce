using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Services.Localization;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc.Routing;

namespace Nixtus.Plugin.Widgets.Lucene.Infrastructure
{
    public class RouterProvider : IRouteProvider
    {
        public int Priority => 1;

        public void RegisterRoutes(IRouteBuilder endpointRouteBuilder)
        {
            var pattern = string.Empty;
            if (DataSettingsManager.DatabaseIsInstalled)
            {
                var localizationSettings = endpointRouteBuilder.ServiceProvider.GetRequiredService<LocalizationSettings>();
                if (localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    var langservice = endpointRouteBuilder.ServiceProvider.GetRequiredService<ILanguageService>();
                    var languages = langservice.GetAllLanguages().ToList();
                    pattern = "{language:lang=" + languages.FirstOrDefault().UniqueSeoCode + "}/";
                }
            }

            // override autocomplete search
            endpointRouteBuilder.MapLocalizedRoute(
                "ProductSearchAutoCompleteLuceneOverride", 
                $"{pattern}catalog/searchtermautocomplete",
                new { controller = "LuceneCatalog", action = "SearchTermAutoComplete" });
        }
    }
}
