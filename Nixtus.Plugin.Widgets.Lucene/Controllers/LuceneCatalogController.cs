using Microsoft.AspNetCore.Mvc;
using Nixtus.Plugin.Widgets.Lucene.Services;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Services.Catalog;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using System.Linq;

namespace Nixtus.Plugin.Widgets.Lucene.Controllers
{
    public class LuceneCatalogController : BasePublicController
    {
        private readonly LuceneSettings _luceneSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly IProductService _productService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IProductModelFactory _productModelFactory;
        private readonly MediaSettings _mediaSettings;
        private readonly ILuceneService _luceneService;

        public LuceneCatalogController(LuceneSettings luceneSettings, CatalogSettings catalogSettings,
        IProductService productService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IProductModelFactory productModelFactory,
        MediaSettings mediaSettings, ILuceneService luceneService)
        {
            _luceneSettings = luceneSettings;
            _catalogSettings = catalogSettings;
            _productService = productService;
            _storeContext = storeContext;
            _workContext = workContext;
            _productModelFactory = productModelFactory;
            _mediaSettings = mediaSettings;
            _luceneService = luceneService;
        }

        public IActionResult SearchTermAutoComplete(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < _catalogSettings.ProductSearchTermMinimumLength)
                return Content("");

            //products
            var productNumber = _catalogSettings.ProductSearchAutoCompleteNumberOfProducts > 0 ?
                _catalogSettings.ProductSearchAutoCompleteNumberOfProducts : 10;

            IPagedList<Product> products = null;

            // use lucene search if enabled
            if (_luceneSettings.AutoCompleteSearchEnabled)
            {
                _luceneService.GetLuceneDirectory();

                var luceneProducts = _luceneService.Search(term);

                products = new PagedList<Product>(luceneProducts.AsQueryable(), 0, productNumber);
            }
            else
            {
                products = _productService.SearchProducts(
                    storeId: _storeContext.CurrentStore.Id,
                    keywords: term,
                    languageId: _workContext.WorkingLanguage.Id,
                    visibleIndividuallyOnly: true,
                    pageSize: productNumber);
            }

            var showLinkToResultSearch = _catalogSettings.ShowLinkToAllResultInSearchAutoComplete && (products.TotalCount > productNumber);

            var models = _productModelFactory.PrepareProductOverviewModels(products, false, _catalogSettings.ShowProductImagesInSearchAutoComplete, _mediaSettings.AutoCompleteSearchThumbPictureSize).ToList();
            var result = (from p in models
                          select new
                          {
                              label = p.Name,
                              producturl = Url.RouteUrl("Product", new { SeName = p.SeName }),
                              productpictureurl = p.DefaultPictureModel.ImageUrl,
                              showlinktoresultsearch = showLinkToResultSearch
                          })
                .ToList();
            return Json(result);
        }
    }
}
