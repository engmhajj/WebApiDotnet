using Microsoft.IdentityModel.Tokens;

namespace webapi.Exceptions;

public class InvalidRefreshTokenException : SecurityTokenException
{
    public InvalidRefreshTokenException(string message)
        : base(message) { }
}
