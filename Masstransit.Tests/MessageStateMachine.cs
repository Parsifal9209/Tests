using MassTransit;
using Microsoft.Extensions.Logging;

namespace Masstransit.Tests;

public class MessageStateMachine : MassTransitStateMachine<MessageSaga>
{
    public MessageStateMachine(ILogger<MessageStateMachine> logger)
    {
        Event(() => MessageRegistration, x => x.CorrelateById(x => x.Message.CorrelationId));

        Event(() => MessageSending, x => x.CorrelateById(x => x.Message.CorrelationId));

        InstanceState(instance => instance.CurrentState);

        Initially(
            HandlerEvent(MessageRegistration)
            .TransitionTo(RegistrationReady));

        During(RegistrationReady,
            HandlerEvent(MessageSending)
            .TransitionTo(SendingReady));
    }

    private EventActivityBinder<MessageSaga, MessageRegistrationEvent>
            HandlerEvent(Event<MessageRegistrationEvent> @event) =>
            When(@event)
                .Then(ctx =>
                {
                    ctx.Instance.CorrelationId = ctx.Data.CorrelationId;
                    ctx.Instance.Address = ctx.Data.Address;
                    ctx.Instance.Body = ctx.Data.Body;
                })
                .PublishAsync(x => x.Init<MessageRegisteredReadyEvent>(x.Data));

    private EventActivityBinder<MessageSaga, MessageSendingEvent>
            HandlerEvent(Event<MessageSendingEvent> @event) =>
            When(@event)
                .Then(ctx =>
                {
                    ctx.Instance.CorrelationId = ctx.Data.CorrelationId;
                })
                .PublishAsync(x => x.Init<MessageSendingReadyEvent>(x.Data));

    public State RegistrationReady { get; private set; }

    public State SendingReady { get; private set; }

    public Event<MessageRegistrationEvent> MessageRegistration { get; private set; }

    public Event<MessageSendingEvent> MessageSending { get; private set; }
}

public class MessageSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; }

    public string Address { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}

public class MessageRegistrationEvent
{
    public Guid CorrelationId { get; set; }

    public string Address { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}

public class MessageRegisteredReadyEvent
{
    public Guid CorrelationId { get; set; }
}

public class MessageSendingEvent
{
    public Guid CorrelationId { get; set; }
}

public class MessageSendingReadyEvent
{
    public Guid CorrelationId { get; set; }
}