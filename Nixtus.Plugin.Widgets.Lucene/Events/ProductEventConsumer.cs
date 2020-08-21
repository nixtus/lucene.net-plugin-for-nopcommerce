using Nixtus.Plugin.Widgets.Lucene.Services;
using Nop.Core.Domain.Catalog;
using Nop.Core.Events;
using Nop.Services.Events;

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

        public void HandleEvent(EntityInsertedEvent<Product> eventMessage)
        {
            _luceneService.AddUpdateLuceneIndex(eventMessage.Entity);
        }

        public void HandleEvent(EntityDeletedEvent<Product> eventMessage)
        {
            _luceneService.DeleteLuceneIndexRecord(eventMessage.Entity.Id);
        }

        public void HandleEvent(EntityUpdatedEvent<Product> eventMessage)
        {
            _luceneService.AddUpdateLuceneIndex(eventMessage.Entity);
        }
    }
}
