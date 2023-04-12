using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using System;
using Serilog;
using Serilog.Formatting.Json;
using Xunit;
using Logging = Microsoft.Extensions.Logging;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Masstransit.Tests
{
    public class MessageBrokerUnitTest: IDisposable
    {
        private readonly InMemoryTestHarness _harness;
        private readonly IServiceProvider _services;

        public MessageBrokerUnitTest()
        {
            var services = new ServiceCollection();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(new JsonFormatter(), "log.txt")
                .WriteTo.Console()
                .CreateLogger();

            services.AddLogging(configure => configure.AddSerilog(Log.Logger));

            services.AddMassTransitInMemoryTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<MessageStateMachine, MessageSaga>().InMemoryRepository();
            });

            _services = services.BuildServiceProvider(true);

            _harness = _services.GetRequiredService<InMemoryTestHarness>();

            _harness.Start();
        }

        public void Dispose()
        {
            _harness.Dispose();
        }

        [Fact]
        public async Task Saga_StateMachine_Published()
        {
            var bus = _services.GetService<IBusControl>();
            await bus.StartAsync();
            var monitor = bus.CreateBusActivityMonitor();


            var messageId = Guid.NewGuid();

            await _harness.Bus.Publish(new MessageRegistrationEvent
            {
                CorrelationId = messageId,
                Body = "test",
                Address = "9954494166"
            });

            await _harness.Bus.Publish(new MessageSendingEvent
            {
                CorrelationId = messageId
            });

            await monitor.AwaitBusInactivity();

            Assert.True(await _harness.Published.Any<MessageSendingReadyEvent>());

        }
    }
}