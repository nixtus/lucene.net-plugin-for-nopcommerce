using Nop.Core.Configuration;

namespace Nixtus.Plugin.Widgets.Lucene
{
    public class LuceneSettings : ISettings
    {
        public bool Enabled { get; set; }

        public bool AutoCompleteSearchEnabled { get; set; }
    }
}
