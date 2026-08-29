namespace Longblack.Application.Common.Exceptions;

public class BatchConflictException(IReadOnlyList<string> conflictingSkus)
    : Exception($"Batch rejected: {conflictingSkus.Count} SKU conflict(s) found.")
{
    public IReadOnlyList<string> ConflictingSkus { get; } = conflictingSkus;
}
