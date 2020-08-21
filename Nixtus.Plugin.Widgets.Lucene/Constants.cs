namespace Nixtus.Plugin.Widgets.Lucene
{
    public class Constants
    {
        public const string FIELD_ID = "Id";
        public const string FIELD_NAME = "Name";
        public const string FIELD_SHORT_DESCRIPTION = "ShortDescription";
        public const string FIELD_FULL_DESCRIPTION = "FullDescription";
        public const string FIELD_PRODUCT_TYPE = "ProductType";
        public const string FIELD_META_TITLE = "MetaTitle";
        public const string FIELD_META_DESCRIPTION = "MetaDescription";
        public const string FIELD_META_KEYWORDS = "MetaKeywords";
        public const string FIELD_MANUFACTURER_NAME = "ManufacturerName";
        public const string FIELD_MANUFACTURER_DESCRIPTION = "ManufacturerDescription";
        public const string FIELD_GTIN = "Gtin";
        public const string FIELD_MANUFACTURER_PART_NUMBER = "ManufacturerPartNumber";
        public const string FIELD_SKU = "Sku";
        public const string FIELD_PRICE = "Price";
        public const string FIELD_OLD_PRICE = "OldPrice";
        public const string FIELD_PRODUCT_TAGS = "ProductTags";
        public const string FIELD_CATEGORY_NAME = "ProductCategoryName";
        public const string FIELD_CATEGORY_DESCRIPTION = "ProductCategoryDescription";
        public const string FIELD_DISABLE_BUY_BUTTON = "DisableBuyButton";
        public const string FIELD_DISABLE_WISHLIST_BUTTON = "DisableWishlistButton";
        public const string FIELD_AVAILABLE_FOR_PREORDER = "AvailableForPreOrder";
        public const string FIELD_PREORDER_AVAILABILITY_STARTDATETIME_UTC = "PreOrderAvailabilityStartDateTimeUtc";

        public const string LUCENE_DIRECTORY_NAME = "lucene_index";
        public const int UPDATE_DELAY_MS = 3000;
        public const string SystemName = "Misc.Lucene";

        public const string DbTableName = "LuceneIndexQueueItem";
    }
}
