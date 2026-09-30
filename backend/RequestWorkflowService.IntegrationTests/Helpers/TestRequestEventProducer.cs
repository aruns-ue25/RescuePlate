using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RequestWorkflowService.Kafka;

namespace RequestWorkflowService.IntegrationTests.Helpers;

public class TestRequestEventProducer : IRequestEventProducer
{
    private readonly ConcurrentBag<(string Topic, string Key, object Event)> _publishedDetails = new();
    private readonly ConcurrentBag<object> _publishedEvents = new();
    private readonly bool _shouldFail;

    public TestRequestEventProducer(bool shouldFail = false)
    {
        _shouldFail = shouldFail;
    }

    public List<object> PublishedEvents => _publishedEvents.ToList();

    public List<(string Topic, string Key, object Event)> PublishedDetails => _publishedDetails.ToList();

    public Task<bool> PublishEventAsync<T>(string topic, string key, T @event, CancellationToken cancellationToken = default)
    {
        if (_shouldFail)
        {
            return Task.FromResult(false);
        }

        if (@event != null)
        {
            _publishedEvents.Add(@event);
            _publishedDetails.Add((topic, key, @event));
        }

        return Task.FromResult(true);
    }

    public void Clear()
    {
        _publishedEvents.Clear();
        _publishedDetails.Clear();
    }
}
