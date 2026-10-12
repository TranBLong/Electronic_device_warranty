namespace Warranty.Exceptions;

public sealed class ConflictException(string message) : Exception(message);
