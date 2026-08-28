namespace Longblack.Application.Common.Exceptions;

public class InvalidReferenceException(string entityName, string field, object value)
    : Exception($"Referenced {entityName} with {field} '{value}' does not exist or is invalid.");
