using System;
using System.Net;

namespace UPA.TrustLayer.Client.Exceptions;

public class TrustLayerException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public string? ErrorCode { get; }

    public TrustLayerException(string message) 
        : base(message) { }

    public TrustLayerException(string message, Exception innerException) 
        : base(message, innerException) { }

    public TrustLayerException(string message, HttpStatusCode statusCode, string? errorCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public class TrustIdempotencyConflictException : TrustLayerException
{
    public TrustIdempotencyConflictException(string message) 
        : base(message, HttpStatusCode.Conflict, "IDEMPOTENCY_CONFLICT") { }
}

public class TrustBundleCollisionException : TrustLayerException
{
    public TrustBundleCollisionException(string message) 
        : base(message, HttpStatusCode.Conflict, "BUNDLE_COLLISION") { }
}

public class TrustNotFoundException : TrustLayerException
{
    public TrustNotFoundException(string message) 
        : base(message, HttpStatusCode.NotFound, "TRUST_NOT_FOUND") { }
}
