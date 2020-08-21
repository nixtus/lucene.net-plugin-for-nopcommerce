using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Directory = Lucene.Net.Store.Directory;

namespace Nixtus.Plugin.Widgets.Lucene.Services
{
    public class LuceneService : ILuceneService
    {
        #region Fields
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHelper _webHelper;
        private readonly INopFileProvider _fileProvider;
        private readonly IProductTagService _productTagService;
        private readonly IRepository<ProductProductTagMapping> _productProductTagMappingRepository;
        private readonly IProductService _productService;
        private readonly IStoreContext _storeContext;
        private readonly ICategoryService _categoryService;
        private readonly IRepository<ProductCategory> _productCategoryMappingRepository;
        private readonly IManufacturerService _manufacturerService;
        private readonly IRepository<ProductManufacturer> _productManufacturersMappingRepository;
        private readonly ILogger _logger;
        private readonly IRepository<ProductTag> _productTagRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Manufacturer> _manufacturerRepository;
        #endregion

        #region Ctor
        public LuceneService(IHttpContextAccessor httpContextAccessor, IWebHelper webHelper, INopFileProvider fileProvider, IProductTagService productTagService,
        IRepository<ProductProductTagMapping> productProductTagMappingRepository, IProductService productService, IStoreContext storeContext,
        ICategoryService categoryService, IRepository<ProductCategory> productCategoryMappingRepository, IManufacturerService manufacturerService,
        IRepository<ProductManufacturer> productManufacturersMappingRepository, ILogger logger, IRepository<ProductTag> productTagRepository, IRepository<Category> categoryRepository,
        IRepository<Manufacturer> manufacturerRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _webHelper = webHelper;
            _fileProvider = fileProvider;
            _productTagService = productTagService;
            _productProductTagMappingRepository = productProductTagMappingRepository;
            _productService = productService;
            _storeContext = storeContext;
            _categoryService = categoryService;
            _productCategoryMappingRepository = productCategoryMappingRepository;
            _manufacturerService = manufacturerService;
            _productManufacturersMappingRepository = productManufacturersMappingRepository;
            _logger = logger;
            _productTagRepository = productTagRepository;
            _categoryRepository = categoryRepository;
            _manufacturerRepository = manufacturerRepository;
        }
        #endregion

        #region Private Methods
        private FSDirectory _luceneTempDir;
        public Directory LuceneDirectory
        {
            get
            {
                if (_luceneTempDir == null)
                    _luceneTempDir = FSDirectory.Open(new DirectoryInfo(GetLuceneDirectory()));

                if (IndexWriter.IsLocked(_luceneTempDir))
                    IndexWriter.Unlock(_luceneTempDir);

                var lockFilePath = _fileProvider.Combine(GetLuceneDirectory(), "write.lock");
                _fileProvider.DeleteFile(lockFilePath);

                return _luceneTempDir;
            }
        }

        public string GetLuceneDirectory()
        {
            var path = _fileProvider.MapPath($"~/App_Data/{Constants.LUCENE_DIRECTORY_NAME}");

            _fileProvider.CreateDirectory(path);

            return path;
        }

        private void InternalAddToLuceneIndex(Product product, IEnumerable<string> productTagNames, IEnumerable<Category> productCategories,
            IEnumerable<Manufacturer> productManufacturers, IndexWriter writer)
        {
            var searchQuery = new TermQuery(new Term(Constants.FIELD_ID, product.Id.ToString()));
            writer.DeleteDocuments(searchQuery);

            //add new index
            var doc = new Document();
            doc.Add(new Field(Constants.FIELD_ID, product.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field(Constants.FIELD_NAME, product.Name, Field.Store.YES, Field.Index.ANALYZED));

            if (!string.IsNullOrEmpty(product.MetaTitle))
                doc.Add(new Field(Constants.FIELD_META_TITLE, product.MetaTitle, Field.Store.YES, Field.Index.ANALYZED));

            if (!string.IsNullOrEmpty(product.MetaDescription))
                doc.Add(new Field(Constants.FIELD_META_DESCRIPTION, product.MetaDescription, Field.Store.YES, Field.Index.ANALYZED));

            if (!string.IsNullOrEmpty(product.MetaKeywords))
                doc.Add(new Field(Constants.FIELD_META_KEYWORDS, product.MetaKeywords, Field.Store.YES, Field.Index.ANALYZED));

            if (!string.IsNullOrEmpty(product.FullDescription))
                doc.Add(new Field(Constants.FIELD_FULL_DESCRIPTION, product.FullDescription, Field.Store.YES, Field.Index.ANALYZED));

            if (!string.IsNullOrEmpty(product.ShortDescription))
                doc.Add(new Field(Constants.FIELD_SHORT_DESCRIPTION, product.ShortDescription, Field.Store.YES, Field.Index.ANALYZED));

            if (!string.IsNullOrEmpty(product.Gtin))
                doc.Add(new Field(Constants.FIELD_GTIN, product.Gtin, Field.Store.YES, Field.Index.ANALYZED));

            if (!string.IsNullOrEmpty(product.Sku))
                doc.Add(new Field(Constants.FIELD_SKU, product.Sku, Field.Store.YES, Field.Index.ANALYZED));

            if (product.OldPrice != decimal.Zero)
                doc.Add(new DoubleField(Constants.FIELD_OLD_PRICE, Convert.ToDouble(product.OldPrice), Field.Store.YES));

            if (product.Price != decimal.Zero)
                doc.Add(new DoubleField(Constants.FIELD_PRICE, Convert.ToDouble(product.Price), Field.Store.YES));

            doc.Add(new Field(Constants.FIELD_DISABLE_BUY_BUTTON, product.DisableBuyButton.ToString(), Field.Store.YES, Field.Index.NO));
            doc.Add(new Field(Constants.FIELD_DISABLE_WISHLIST_BUTTON, product.DisableWishlistButton.ToString(), Field.Store.YES, Field.Index.NO));
            doc.Add(new Field(Constants.FIELD_AVAILABLE_FOR_PREORDER, product.AvailableForPreOrder.ToString(), Field.Store.YES, Field.Index.NO));

            if (product.PreOrderAvailabilityStartDateTimeUtc.HasValue)
                doc.Add(new Field(Constants.FIELD_PREORDER_AVAILABILITY_STARTDATETIME_UTC, product.PreOrderAvailabilityStartDateTimeUtc.Value.ToString(), Field.Store.YES, Field.Index.NO));

            if (!string.IsNullOrEmpty(product.ManufacturerPartNumber))
                doc.Add(new Field(Constants.FIELD_MANUFACTURER_PART_NUMBER, product.ManufacturerPartNumber, Field.Store.YES, Field.Index.ANALYZED));


            doc.Add(new Field(Constants.FIELD_PRODUCT_TYPE, product.ProductTypeId.ToString(), Field.Store.YES, Field.Index.NO));

            //add product tags
            foreach (var productTag in productTagNames)
            {
                doc.Add(new Field(Constants.FIELD_PRODUCT_TAGS, productTag, Field.Store.YES, Field.Index.ANALYZED));
            }

            //add product categories
            foreach (var category in productCategories)
            {
                if (!string.IsNullOrEmpty(category.Name))
                    doc.Add(new Field(Constants.FIELD_CATEGORY_NAME, category.Name, Field.Store.YES, Field.Index.ANALYZED));

                if (!string.IsNullOrEmpty(category.Description))
                    doc.Add(new Field(Constants.FIELD_CATEGORY_DESCRIPTION, category.Description, Field.Store.YES, Field.Index.ANALYZED));
            }

            //manufacturers
            foreach (var man in productManufacturers)
            {
                if (!string.IsNullOrEmpty(man.Name))
                    doc.Add(new Field(Constants.FIELD_MANUFACTURER_NAME, man.Name, Field.Store.YES, Field.Index.ANALYZED));

                if (!string.IsNullOrEmpty(man.Description))
                    doc.Add(new Field(Constants.FIELD_MANUFACTURER_DESCRIPTION, man.Description, Field.Store.YES, Field.Index.ANALYZED));
            }

            writer.AddDocument(doc);
        }

        private IEnumerable<Product> InternalSearch(string searchQuery, string searchField = "")
        {
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", "")))
                return new List<Product>();

            using (var reader = DirectoryReader.Open(LuceneDirectory))
            {
                var searcher = new IndexSearcher(reader);

                var hitLimit = 1000;
                var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

                if (!string.IsNullOrEmpty(searchField))
                {
                    var parser = new QueryParser(LuceneVersion.LUCENE_48, searchField, analyzer);
                    var query = ParseQuery(searchQuery, parser);

                    var hits = searcher.Search(query, hitLimit).ScoreDocs;
                    var results = MapLuceneDocumentsToProducts(hits, searcher);

                    return results;
                }
                else
                {
                    var finalQuery = new BooleanQuery();
                    var parser = new MultiFieldQueryParser(LuceneVersion.LUCENE_48, GetSearchableProductFields(), analyzer);

                    var terms = searchQuery.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var term in terms)
                        finalQuery.Add(parser.Parse(term.Replace("~", "") + "~"), Occur.MUST);

                    //var query = ParseQuery(searchQuery, parser);
                    var hits = searcher.Search(finalQuery, null, hitLimit, Sort.RELEVANCE).ScoreDocs;
                    //searcher.Search(query, new HitLimitCollector());
                    var results = MapLuceneDocumentsToProducts(hits, searcher);

                    return results;
                }
            }
        }

        private string[] GetSearchableProductFields()
        {
            return new[]
            {
                Constants.FIELD_ID,
                Constants.FIELD_NAME,
                Constants.FIELD_SHORT_DESCRIPTION,
                Constants.FIELD_FULL_DESCRIPTION,
                Constants.FIELD_PRODUCT_TYPE,
                Constants.FIELD_META_TITLE,
                Constants.FIELD_META_DESCRIPTION ,
                Constants.FIELD_META_KEYWORDS,
                Constants.FIELD_MANUFACTURER_NAME,
                Constants.FIELD_MANUFACTURER_DESCRIPTION,
                Constants.FIELD_GTIN,
                Constants.FIELD_MANUFACTURER_PART_NUMBER,
                Constants.FIELD_SKU,
                Constants.FIELD_PRICE,
                Constants.FIELD_OLD_PRICE,
                Constants.FIELD_PRODUCT_TAGS ,
                Constants.FIELD_CATEGORY_NAME,
                Constants.FIELD_CATEGORY_DESCRIPTION,
                Constants.FIELD_DISABLE_BUY_BUTTON,
                Constants.FIELD_DISABLE_WISHLIST_BUTTON,
                Constants.FIELD_AVAILABLE_FOR_PREORDER,
                Constants.FIELD_PREORDER_AVAILABILITY_STARTDATETIME_UTC
            };
        }

        private Product MapLuceneToProduct(Document document)
        {
            return new Product
            {
                Id = Convert.ToInt32(document.Get(Constants.FIELD_ID)),
                Name = document.Get(Constants.FIELD_NAME),
                ShortDescription = document.Get(Constants.FIELD_SHORT_DESCRIPTION),
                FullDescription = document.Get(Constants.FIELD_FULL_DESCRIPTION),
                MetaTitle = document.Get(Constants.FIELD_META_TITLE),
                MetaDescription = document.Get(Constants.FIELD_META_DESCRIPTION),
                MetaKeywords = document.Get(Constants.FIELD_META_KEYWORDS),
                Gtin = document.Get(Constants.FIELD_GTIN),
                Sku = document.Get(Constants.FIELD_SKU),
                ProductType = (ProductType)Convert.ToInt32(document.Get(Constants.FIELD_PRODUCT_TYPE)),
                OldPrice = string.IsNullOrEmpty(document.Get(Constants.FIELD_OLD_PRICE)) ? decimal.Zero : decimal.Parse(document.Get(Constants.FIELD_OLD_PRICE)),
                Price = string.IsNullOrEmpty(document.Get(Constants.FIELD_PRICE)) ? decimal.Zero : decimal.Parse(document.Get(Constants.FIELD_PRICE)),
                DisableBuyButton = Convert.ToBoolean(document.Get(Constants.FIELD_DISABLE_BUY_BUTTON)),
                DisableWishlistButton = Convert.ToBoolean(document.Get(Constants.FIELD_DISABLE_WISHLIST_BUTTON)),
                AvailableForPreOrder = Convert.ToBoolean(document.Get(Constants.FIELD_AVAILABLE_FOR_PREORDER)),
                PreOrderAvailabilityStartDateTimeUtc = string.IsNullOrEmpty(document.Get(Constants.FIELD_PREORDER_AVAILABILITY_STARTDATETIME_UTC)) ? (DateTime?)null : DateTime.Parse(document.Get(Constants.FIELD_PREORDER_AVAILABILITY_STARTDATETIME_UTC)),
                ManufacturerPartNumber = document.Get(Constants.FIELD_MANUFACTURER_PART_NUMBER)
            };
        }

        private IEnumerable<Product> MapLuceneDocumentsToProducts(IEnumerable<Document> documents)
        {
            return documents.Select(x => MapLuceneToProduct(x));
        }

        private IEnumerable<Product> MapLuceneDocumentsToProducts(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            return hits.Select(hit => MapLuceneToProduct(searcher.Doc(hit.Doc))).ToList();
        }

        private Query ParseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParserBase.Escape(searchQuery.Trim()));
            }

            return query;
        }

        private void AddUpdateLuceneIndex(IEnumerable<Product> products, IEnumerable<ProductTag> productTags,
            IEnumerable<ProductProductTagMapping> productProductTagMappings, IEnumerable<Category> categories,
            IEnumerable<ProductCategory> productCategories, IEnumerable<Manufacturer> manufacturers,
            IEnumerable<ProductManufacturer> productManufacturers)
        {
            var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var iwc = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };

            using (var writer = new IndexWriter(LuceneDirectory, iwc))
            {
                int i = 1;
                foreach (var product in products)
                {
                    var productTagNames = from pt in productTags
                                          join ptt in productProductTagMappings on pt.Id equals ptt.ProductTagId
                                          where ptt.ProductId == product.Id
                                          select pt.Name;

                    var filteredProductCategories = from c in categories
                                                    join pc in productCategories on c.Id equals pc.CategoryId
                                                    where pc.ProductId == product.Id
                                                    select c;

                    var filteredProductManufacturers = from m in manufacturers
                                                       join pm in productManufacturers on m.Id equals pm.ManufacturerId
                                                       where pm.ProductId == product.Id
                                                       select m;

                    InternalAddToLuceneIndex(product, productTagNames, filteredProductCategories, filteredProductManufacturers, writer);
                    i++;

                    if (i == 2000)
                    {
                        writer.Flush(true, true);
                        i = 1;
                    }
                }
                
                writer.Dispose();
            }
        }
        #endregion

        #region Public Methods
        public void AddUpdateLuceneIndex(Product product)
        {
            var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var iwc = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
            iwc.OpenMode = OpenMode.CREATE_OR_APPEND;

            var productTagNames = _productTagService.GetAllProductTagsByProductId(product.Id).Select(x => x.Name);

            var productCategories = _categoryService.GetProductCategoriesByProductId(product.Id);
            var categories = _categoryService.GetCategoriesByIds(productCategories.Select(x => x.CategoryId).ToArray());

            var productManufacturers = _manufacturerService.GetProductManufacturersByProductId(product.Id);
            var manufacturers = from m in _manufacturerService.GetAllManufacturers()
                                join pm in productManufacturers on m.Id equals pm.ManufacturerId
                                select m;

            using (var writer = new IndexWriter(LuceneDirectory, iwc))
            {
                InternalAddToLuceneIndex(product, productTagNames, categories, manufacturers, writer);

                writer.Dispose();
            }
        }

        public void DeleteLuceneIndexRecord(int productId)
        {
            var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var iwc = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };

            using (var writer = new IndexWriter(LuceneDirectory, iwc))
            {
                var searchQuery = new TermQuery(new Term("Id", productId.ToString()));

                writer.DeleteDocuments(searchQuery);

                writer.Dispose();
            }
        }

        public void ClearAllLuceneIndexes()
        {
            try
            {
                var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
                var iwc = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
                {
                    OpenMode = OpenMode.CREATE_OR_APPEND
                };

                using (var writer = new IndexWriter(LuceneDirectory, iwc))
                {
                    writer.DeleteAll();

                    writer.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error occurred while clearing lucene indexes", ex);
            }
        }

        public void BuildIndex()
        {
            var pageIndex = 0;
            var pageSize = 5000;

            // get all product categories, tags and manufacturers so that we aren't
            // requesting them for every product
            var productTags = _productTagService.GetAllProductTags().ToList();
            var productProductTags = (from ptt in _productProductTagMappingRepository.Table
                                      join pt in _productTagRepository.Table on ptt.ProductTagId equals pt.Id
                                      select ptt).ToList();


            var categories = _categoryService.GetAllCategories().ToList();
            var productCategories = (from pc in _productCategoryMappingRepository.Table
                                     join c in _categoryRepository.Table on pc.CategoryId equals c.Id
                                     select pc).ToList();

            var manufacturers = _manufacturerService.GetAllManufacturers().ToList();
            var productManufacturers = (from pm in _productManufacturersMappingRepository.Table
                                        join m in _manufacturerRepository.Table on pm.ManufacturerId equals m.Id
                                        select pm).ToList();

            var products = _productService.SearchProducts(
                pageIndex: pageIndex, pageSize: pageSize,
                storeId: _storeContext.CurrentStore.Id);

            while (products.Count != 0)
            {
                var productIds = products.Select(x => x.Id);
                var filteredProductProductTags = productProductTags.Where(x => productIds.Contains(x.ProductId));
                var filteredProductCategories = productCategories.Where(x => productIds.Contains(x.ProductId));
                var filteredProductManufacturers = productManufacturers.Where(x => productIds.Contains(x.ProductId));

                AddUpdateLuceneIndex(products.ToList(), productTags, filteredProductProductTags,
                    categories, filteredProductCategories, manufacturers, filteredProductManufacturers);

                pageIndex += 1;

                products = _productService.SearchProducts(
                    pageIndex: pageIndex, pageSize: pageSize,
                    storeId: _storeContext.CurrentStore.Id,
                    visibleIndividuallyOnly: true);
            }
        }

        public IEnumerable<Product> Search(string input, string fieldName = "")
        {
            if (string.IsNullOrEmpty(input))
                return new List<Product>();

            return InternalSearch(input, fieldName);
        }
        #endregion
    }
}
