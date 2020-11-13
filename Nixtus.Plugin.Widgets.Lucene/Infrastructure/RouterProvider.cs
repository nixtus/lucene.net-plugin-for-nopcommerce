using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nixtus.Plugin.Widgets.Lucene.Infrastructure
{
    public class RouterProvider : IRouteProvider
    {
        public int Priority => 1;

        public void RegisterRoutes(IRouteBuilder endpointRouteBuilder)
        {
            
        }
    }
}
