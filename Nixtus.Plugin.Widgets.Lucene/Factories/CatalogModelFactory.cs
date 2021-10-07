using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nixtus.Plugin.Widgets.Lucene.Services;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Topics;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Framework.Events;
using Nop.Web.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nixtus.Plugin.Widgets.Lucene.Factories
{
    public class CatalogModelFactory : Nop.Web.Factories.CatalogModelFactory
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly ICategoryService _categoryService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IProductService _productService;
        private readonly ISearchTermService _searchTermService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        private readonly LuceneSettings _luceneSettings;
        private readonly ILuceneService _luceneService;

        #endregion

        #region Ctor

        public CatalogModelFactory(BlogSettings blogSettings,
            CatalogSettings catalogSettings,
            DisplayDefaultMenuItemSettings displayDefaultMenuItemSettings,
            ForumSettings forumSettings,
            IActionContextAccessor actionContextAccessor,
            ICategoryService categoryService,
            ICategoryTemplateService categoryTemplateService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            IManufacturerTemplateService manufacturerTemplateService,
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            IProductModelFactory productModelFactory,
            IProductService productService,
            IProductTagService productTagService,
            ISearchTermService searchTermService,
            ISpecificationAttributeService specificationAttributeService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            ITopicService topicService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            VendorSettings vendorSettings,
            LuceneSettings luceneSettings,
            ILuceneService luceneService)
            : base(blogSettings,
                 catalogSettings,
                 displayDefaultMenuItemSettings,
                 forumSettings,
                 actionContextAccessor,
                 categoryService,
                 categoryTemplateService,
                 currencyService,
                 customerService,
                 eventPublisher,
                 httpContextAccessor,
                 localizationService,
                 manufacturerService,
                 manufacturerTemplateService,
                 pictureService,
                 priceFormatter,
                 productModelFactory,
                 productService,
                 productTagService,
                 searchTermService,
                 specificationAttributeService,
                 staticCacheManager,
                 storeContext,
                 topicService,
                 urlHelperFactory,
                 urlRecordService,
                 vendorService,
                 webHelper,
                 workContext,
                 mediaSettings,
                 vendorSettings)
        {
            _catalogSettings = catalogSettings;
            _categoryService = categoryService;
            _eventPublisher = eventPublisher;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _productService = productService;
            _searchTermService = searchTermService;
            _storeContext = storeContext;
            _workContext = workContext;

            _luceneSettings = luceneSettings;
            _luceneService = luceneService;
        }

        #endregion

        public override async Task<CatalogProductsModel> PrepareSearchProductsModelAsync(SearchModel searchModel, CatalogProductsCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var model = new CatalogProductsModel
            {
                UseAjaxLoading = _catalogSettings.UseAjaxCatalogProductsLoading
            };

            //sorting
            await PrepareSortingOptionsAsync(model, command);
            //view mode
            await PrepareViewModesAsync(model, command);
            //page size
            await PreparePageSizeOptionsAsync(model, command, _catalogSettings.SearchPageAllowCustomersToSelectPageSize,
                _catalogSettings.SearchPagePageSizeOptions, _catalogSettings.SearchPageProductsPerPage);

            var searchTerms = searchModel.q == null
                ? string.Empty
                : searchModel.q.Trim();

            IPagedList<Product> products = new PagedList<Product>(new List<Product>(), 0, 1);
            // only search if query string search keyword is set (used to aasync Task searching or displaying search term min length error message on /search page load)
            //we don't use "!string.IsNullOrEmpty(searchTerms)" in cases of "ProductSearchTermMinimumLength" set to 0 but searching by other parameters (e.g. category or price filter)
            var isSearchTermSpecified = _httpContextAccessor.HttpContext.Request.Query.ContainsKey("q");
            if (isSearchTermSpecified)
            {
                var currentStore = await _storeContext.GetCurrentStoreAsync();

                if (searchTerms.Length < _catalogSettings.ProductSearchTermMinimumLength)
                {
                    model.WarningMessage =
                        string.Format(await _localizationService.GetResourceAsync("Search.SearchTermMinimumLengthIsNCharacters"),
                            _catalogSettings.ProductSearchTermMinimumLength);
                }
                else
                {
                    var categoryIds = new List<int>();
                    var manufacturerId = 0;
                    var searchInDescriptions = false;
                    var vendorId = 0;
                    if (searchModel.advs)
                    {
                        //advanced search
                        var categoryId = searchModel.cid;
                        if (categoryId > 0)
                        {
                            categoryIds.Add(categoryId);
                            if (searchModel.isc)
                            {
                                //include subcategories
                                categoryIds.AddRange(
                                    await _categoryService.GetChildCategoryIdsAsync(categoryId, currentStore.Id));
                            }
                        }

                        manufacturerId = searchModel.mid;

                        if (searchModel.asv)
                            vendorId = searchModel.vid;

                        searchInDescriptions = searchModel.sid;
                    }

                    //var searchInProductTags = false;
                    var searchInProductTags = searchInDescriptions;
                    var workingLanguage = await _workContext.GetWorkingLanguageAsync();

                    //price range
                    PriceRangeModel selectedPriceRange = null;
                    if (_catalogSettings.EnablePriceRangeFiltering && _catalogSettings.SearchPagePriceRangeFiltering)
                    {
                        selectedPriceRange = await GetConvertedPriceRangeAsync(command);

                        PriceRangeModel availablePriceRange = null;
                        if (!_catalogSettings.SearchPageManuallyPriceRange)
                        {
                            async Task<decimal?> getProductPriceAsync(ProductSortingEnum orderBy)
                            {
                                var products = await _productService.SearchProductsAsync(0, 1,
                                    categoryIds: categoryIds,
                                    manufacturerIds: new List<int> { manufacturerId },
                                    storeId: currentStore.Id,
                                    visibleIndividuallyOnly: true,
                                    keywords: searchTerms,
                                    searchDescriptions: searchInDescriptions,
                                    searchProductTags: searchInProductTags,
                                    languageId: workingLanguage.Id,
                                    vendorId: vendorId,
                                    orderBy: orderBy);

                                return products?.FirstOrDefault()?.Price ?? 0;
                            }

                            availablePriceRange = new PriceRangeModel
                            {
                                From = await getProductPriceAsync(ProductSortingEnum.PriceAsc),
                                To = await getProductPriceAsync(ProductSortingEnum.PriceDesc)
                            };
                        }
                        else
                        {
                            availablePriceRange = new PriceRangeModel
                            {
                                From = _catalogSettings.SearchPagePriceFrom,
                                To = _catalogSettings.SearchPagePriceTo
                            };
                        }

                        model.PriceRangeFilter = await PreparePriceRangeFilterAsync(selectedPriceRange, availablePriceRange);
                    }

                    //products
                    if (_luceneSettings.Enabled)
                    {
                        _luceneService.GetLuceneDirectory();

                        var luceneProducts = _luceneService.Search(searchTerms);

                        products = await luceneProducts.AsQueryable().ToPagedListAsync(command.PageNumber - 1, command.PageSize);
                    }
                    else
                    {
                        products = await _productService.SearchProductsAsync(
                            command.PageNumber - 1,
                            command.PageSize,
                            categoryIds: categoryIds,
                            manufacturerIds: new List<int> { manufacturerId },
                            storeId: currentStore.Id,
                            visibleIndividuallyOnly: true,
                            keywords: searchTerms,
                            priceMin: selectedPriceRange?.From,
                            priceMax: selectedPriceRange?.To,
                            searchDescriptions: searchInDescriptions,
                            searchProductTags: searchInProductTags,
                            languageId: workingLanguage.Id,
                            orderBy: (ProductSortingEnum)command.OrderBy,
                            vendorId: vendorId);
                    }

                    //search term statistics
                    if (!string.IsNullOrEmpty(searchTerms))
                    {
                        var searchTerm =
                            await _searchTermService.GetSearchTermByKeywordAsync(searchTerms, currentStore.Id);
                        if (searchTerm != null)
                        {
                            searchTerm.Count++;
                            await _searchTermService.UpdateSearchTermAsync(searchTerm);
                        }
                        else
                        {
                            searchTerm = new SearchTerm
                            {
                                Keyword = searchTerms,
                                StoreId = currentStore.Id,
                                Count = 1
                            };
                            await _searchTermService.InsertSearchTermAsync(searchTerm);
                        }
                    }

                    //event
                    await _eventPublisher.PublishAsync(new ProductSearchEvent
                    {
                        SearchTerm = searchTerms,
                        SearchInDescriptions = searchInDescriptions,
                        CategoryIds = categoryIds,
                        ManufacturerId = manufacturerId,
                        WorkingLanguageId = workingLanguage.Id,
                        VendorId = vendorId
                    });
                }
            }

            var isFiltering = !string.IsNullOrEmpty(searchTerms);
            await PrepareCatalogProductsAsync(model, products, isFiltering);

            return model;
        }
    }
}
