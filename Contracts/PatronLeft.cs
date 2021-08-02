using System;

namespace Contracts
{
    public interface PatronLeft
    {
        Guid PatronId { get; }
        DateTime Timestamp { get; }
    }
}
