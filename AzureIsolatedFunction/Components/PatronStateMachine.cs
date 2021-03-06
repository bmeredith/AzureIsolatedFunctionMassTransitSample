using Automatonymous;
using Contracts;
using MassTransit;

namespace AzureIsolatedFunction.Components
{
    public sealed class PatronStateMachine :
        MassTransitStateMachine<PatronState>
    {
        public PatronStateMachine()
        {
            Event(() => Entered, x => x.CorrelateById(m => m.Message.PatronId));
            Event(() => Left, x => x.CorrelateById(m => m.Message.PatronId));

            InstanceState(x => x.CurrentState, Tracking);

            Initially(
                When(Entered)
                    .Then(context => context.Instance.Entered = context.Data.Timestamp)
                    .TransitionTo(Tracking),
                When(Left)
                    .Then(context => context.Instance.Left = context.Data.Timestamp)
                    .TransitionTo(Tracking)
            );

            During(Tracking,
                When(Entered)
                    .Then(context => context.Instance.Entered = context.Data.Timestamp),
                When(Left)
                    .Then(context => context.Instance.Left = context.Data.Timestamp)
            );

            CompositeEvent(() => Visited, x => x.VisitedStatus, CompositeEventOptions.IncludeInitial, Entered, Left);

            DuringAny(
                When(Visited)
                    .PublishAsync(context => context.Init<PatronVisited>(new
                    {
                        PatronId = context.Instance.CorrelationId,
                        context.Instance.Entered,
                        context.Instance.Left
                    }))
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        public State Tracking { get; private set; }
        public Event<PatronEntered> Entered { get; private set; }
        public Event<PatronLeft> Left { get; private set; }
        public Event Visited { get; private set; }
    }
}
