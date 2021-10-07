using System.Collections.Generic;
using System.Threading.Tasks;
using Lucene.Net.Store;
using Nop.Core.Domain.Catalog;

namespace Nixtus.Plugin.Widgets.Lucene.Services
{
    public interface ILuceneService
    {
        Task AddUpdateLuceneIndex(Product product);
        void ClearAllLuceneIndexes();
        void DeleteLuceneIndexRecord(int productId);
        IEnumerable<Product> Search(string input, string fieldName = "");
        string GetLuceneDirectory();

        Directory LuceneDirectory { get; }

        Task BuildIndex();
    }
}
