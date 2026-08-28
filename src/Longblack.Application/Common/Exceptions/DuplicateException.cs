namespace Longblack.Application.Common.Exceptions;

public class DuplicateException(string entityName, string field, string value)
    : Exception($"A {entityName} with {field} '{value}' already exists.");
