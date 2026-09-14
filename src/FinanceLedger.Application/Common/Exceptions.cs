namespace FinanceLedger.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, string id) : base($"{entity} '{id}' was not found.") { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class ValidationAppException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Thrown when a call to the external identity provider (Supabase Auth) fails.</summary>
public class IdentityProviderException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public IdentityProviderException(string message, System.Net.HttpStatusCode statusCode, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
