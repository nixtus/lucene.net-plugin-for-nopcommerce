using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;

namespace Nixtus.Plugin.Widgets.Lucene
{
    public class LucenePlugin : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        #endregion

        #region Ctor

        public LucenePlugin(ISettingService settingService, ILocalizationService localizationService, IWebHelper webHelper)
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _webHelper = webHelper;
        }
        #endregion

        #region Methods

        public override void Install()
        {
            var settings = new LuceneSettings
            {
                Enabled = true,
                AutoCompleteSearchEnabled = true
            };

            _settingService.SaveSetting(settings);

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.Lucene.Fields.Enabled", "Enabled Lucene search");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.Lucene.Fields.Enabled.Hint", "Turn on the Lucene Full-Text search functionality");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.Lucene.Fields.AutoCompleteSearchEnabled", "Enable auto complete search");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.Lucene.Fields.AutoCompleteSearchEnabled.Hint", "Turn on the Lucene Full-Text search functionality for auto complete");

            base.Install();
        }

        public override void Uninstall()
        {
            _settingService.DeleteSetting<LuceneSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.Lucene.Fields.Enabled");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.Lucene.Fields.Enabled.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.Lucene.Fields.AutoCompleteSearchEnabled");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.Lucene.Fields.AutoCompleteSearchEnabled.Hint");

            base.Uninstall();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/Lucene/Configure";
        }

        #endregion
    }
}
