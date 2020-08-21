using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nixtus.Plugin.Widgets.Lucene.Models
{
    public class ConfigurationModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Misc.Lucene.Fields.Enabled")]
        public bool Enabled { get; set; }
        public bool Enabled_OverrideForStore { get; set; }
    }
}
