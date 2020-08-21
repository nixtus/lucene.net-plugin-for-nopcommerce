using System.Collections.Generic;
using Lucene.Net.Store;
using Nop.Core.Domain.Catalog;

namespace Nixtus.Plugin.Widgets.Lucene.Services
{
    public interface ILuceneService
    {
        void AddUpdateLuceneIndex(Product product);
        void ClearAllLuceneIndexes();
        void DeleteLuceneIndexRecord(int productId);
        IEnumerable<Product> Search(string input, string fieldName = "");
        string GetLuceneDirectory();

        Directory LuceneDirectory { get; }

        void BuildIndex();
    }
}
