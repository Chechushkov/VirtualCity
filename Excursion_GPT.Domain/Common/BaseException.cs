namespace Excursion_GPT.Domain.Common;

public abstract class BaseException : Exception
{
    public int StatusCode { get; }
    public string Object { get; }

    protected BaseException(int statusCode, string @object, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Object = @object;
    }
}

public class NotFoundException : BaseException
{
    public NotFoundException(string @object, string message = "Specified resource is not found")
        : base(404, @object, message) { }
}

public class ForbiddenException : BaseException
{
    public ForbiddenException(string @object, string message = "Access restricted")
        : base(403, @object, message) { }
}

public class UnauthorizedException : BaseException
{
    public UnauthorizedException(string @object = "authentification", string message = "Authentication should be made")
        : base(401, @object, message) { }
}

public class UploadFailedException : BaseException
{
    public UploadFailedException(string @object = "upload", string message = "Could not upload model")
        : base(413, @object, message) { }
}

public class InvalidOperationException : BaseException
{
    public InvalidOperationException(string @object, string message)
        : base(400, @object, message) { }
}