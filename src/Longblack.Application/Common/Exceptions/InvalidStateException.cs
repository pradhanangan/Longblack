namespace Longblack.Application.Common.Exceptions;

// Raised when an operation is not valid for the entity's current lifecycle state,
// e.g. editing a Received goods receipt, or completing one with no lines.
public class InvalidStateException(string message) : Exception(message);
