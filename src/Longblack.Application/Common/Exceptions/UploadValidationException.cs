namespace Longblack.Application.Common.Exceptions;

public record UploadRowError(int Row, string Message);

// Raised when an uploaded file is rejected as a whole; Row 0 denotes a file-level problem (e.g. missing header).
public class UploadValidationException(IReadOnlyList<UploadRowError> errors)
    : Exception($"Upload rejected: {errors.Count} error(s) found.")
{
    public IReadOnlyList<UploadRowError> Errors { get; } = errors;
}
