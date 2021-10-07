using Microsoft.AspNetCore.Mvc;
using Nixtus.Plugin.Widgets.Lucene.Models;
using Nixtus.Plugin.Widgets.Lucene.Services;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System.Threading.Tasks;

namespace Nixtus.Plugin.Widgets.Lucene.Controllers
{
    public class LuceneController : BasePluginController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ILuceneService _luceneService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Constructors

        public LuceneController(ISettingService settingService,
            ILuceneService luceneService,
            IStoreContext storeContext)
        {
            _settingService = settingService;
            _luceneService = luceneService;
            _storeContext = storeContext;
        }

        #endregion

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<ActionResult> Configure()
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var luceneSettings = await _settingService.LoadSettingAsync<LuceneSettings>(storeScope);

            var model = new ConfigurationModel();
            model.Enabled = luceneSettings.Enabled;
            model.AutoCompleteSearchEnabled = luceneSettings.AutoCompleteSearchEnabled;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.Enabled_OverrideForStore = await _settingService.SettingExistsAsync(luceneSettings, x => x.Enabled, storeScope);
                model.AutoCompleteSearchEnabled_OverrideForStore = await _settingService.SettingExistsAsync(luceneSettings, x => x.AutoCompleteSearchEnabled, storeScope);
            }

            return View("~/Plugins/Nixtus.Misc.Lucene/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<ActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var luceneSettings = await _settingService.LoadSettingAsync<LuceneSettings>(storeScope);

            luceneSettings.Enabled = model.Enabled;
            luceneSettings.AutoCompleteSearchEnabled = model.AutoCompleteSearchEnabled;

            if (model.Enabled_OverrideForStore || storeScope == 0)
                await _settingService.SaveSettingAsync(luceneSettings, x => x.Enabled, storeScope, false);
            else if (storeScope > 0)
                await _settingService.DeleteSettingAsync(luceneSettings, x => x.Enabled, storeScope);

            if (model.AutoCompleteSearchEnabled_OverrideForStore || storeScope == 0)
                await _settingService.SaveSettingAsync(luceneSettings, x => x.AutoCompleteSearchEnabled, storeScope, false);
            else if (storeScope > 0)
                await _settingService.DeleteSettingAsync(luceneSettings, x => x.AutoCompleteSearchEnabled, storeScope);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            return await Configure();
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<ActionResult> RebuildIndex()
        {
            await _luceneService.BuildIndex();

            return Json(new
            {
                success = "0"
            });
        }
    }
}
