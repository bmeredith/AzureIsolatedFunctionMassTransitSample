using System;
using Automatonymous;

namespace AzureIsolatedFunction.Components
{
    public class PatronState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public int CurrentState { get; set; }
        public int VisitedStatus { get; set; }

        public DateTime Entered { get; set; }
        public DateTime Left { get; set; }
    }
}
