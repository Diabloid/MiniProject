namespace ApiGateway.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ServiceUnavailableException : Exception
{
    public string Title { get; }

    public ServiceUnavailableException(string title, string message) : base(message)
    {
        Title = title;
    }
}

public class GatewayTimeoutException : Exception
{
    public GatewayTimeoutException(string message) : base(message) { }
}
