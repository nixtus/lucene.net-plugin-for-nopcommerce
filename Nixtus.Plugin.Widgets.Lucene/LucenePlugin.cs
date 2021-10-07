﻿using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public override Task InstallAsync()
        {
            var settings = new LuceneSettings
            {
                Enabled = true,
                AutoCompleteSearchEnabled = true
            };

            _settingService.SaveSettingAsync(settings);

            //locales
            _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                { "Plugins.Misc.Lucene.Fields.Enabled", "Enable Lucene search" },
                { "Plugins.Misc.Lucene.Fields.Enabled.Hint", "Turn on the Lucene Full-Text search funtionality" },
                { "Plugins.Misc.Lucene.Fields.AutoCompleteSearchEnabled", "Enable auto complete search" },
                { "Plugins.Misc.Lucene.Fields.AutoCompleteSearchEnabled.Hint", "Turn on the Lucene Full-Text search funtionality for auto complete" }
            });

            return base.InstallAsync();
        }

        public override Task UninstallAsync()
        {
            _settingService.DeleteSettingAsync<LuceneSettings>();

            //locales
            _localizationService.DeleteLocaleResourcesAsync(new List<string>
            {
                "Plugins.Misc.Lucene.Fields.Enabled",
                "Plugins.Misc.Lucene.Fields.Enabled.Hint",
                "Plugins.Misc.Lucene.Fields.AutoCompleteSearchEnabled",
                "Plugins.Misc.Lucene.Fields.AutoCompleteSearchEnabled.Hint"
            });

            return base.UninstallAsync();
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
