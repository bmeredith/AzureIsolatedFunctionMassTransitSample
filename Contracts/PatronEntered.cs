using System;

namespace Contracts
{
    public interface PatronEntered
    {
        Guid PatronId { get; }
        DateTime Timestamp { get; }
    }
}
