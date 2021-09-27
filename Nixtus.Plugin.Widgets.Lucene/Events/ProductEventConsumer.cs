using Nixtus.Plugin.Widgets.Lucene.Services;
using Nop.Core.Domain.Catalog;
using Nop.Core.Events;
using Nop.Services.Events;
using System.Threading.Tasks;

namespace Nixtus.Plugin.Widgets.Lucene.Events
{
    public class ProductEventConsumer : IConsumer<EntityInsertedEvent<Product>>,
        IConsumer<EntityDeletedEvent<Product>>,
        IConsumer<EntityUpdatedEvent<Product>>
    {
        private readonly ILuceneService _luceneService;

        public ProductEventConsumer(ILuceneService luceneService)
        {
            _luceneService = luceneService;
        }

        public async Task HandleEventAsync(EntityInsertedEvent<Product> eventMessage)
        {
            await _luceneService.AddUpdateLuceneIndex(eventMessage.Entity);
        }

        public async Task HandleEventAsync(EntityDeletedEvent<Product> eventMessage)
        {
            await Task.Run(() => _luceneService.DeleteLuceneIndexRecord(eventMessage.Entity.Id));
        }

        public async Task HandleEventAsync(EntityUpdatedEvent<Product> eventMessage)
        {
            await _luceneService.AddUpdateLuceneIndex(eventMessage.Entity);
        }
    }
}
