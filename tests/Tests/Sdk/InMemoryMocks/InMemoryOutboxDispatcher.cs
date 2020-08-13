using System.Collections.Generic;
using System.Threading.Tasks;
using Swisschain.Extensions.Idempotency;

namespace Tests.Sdk.InMemoryMocks
{
    public class InMemoryOutboxDispatcher : IOutboxDispatcher
    {
        private readonly List<object> _sentCommands = new List<object>();
        private readonly List<object> _publishedEvents = new List<object>();

        public IReadOnlyCollection<object> SentCommands => _sentCommands;
        public IReadOnlyCollection<object> PublishedEvents => _publishedEvents;

        public Task Send(object command)
        {
            _sentCommands.Add(command);

            return Task.CompletedTask;
        }

        public Task Publish(object evt)
        {
            _publishedEvents.Add(evt);

            return Task.CompletedTask;
        }

        public void Clear()
        {
            _sentCommands.Clear();
            _publishedEvents.Clear();
        }
    }
}
