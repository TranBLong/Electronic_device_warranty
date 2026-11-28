namespace Warranty.Services;

public sealed class FileValidationException(string message) : Exception(message);
