
using CSharpFunctionalExtensions;

namespace ServiceDefault;

public record RabbitMqUri
{
    public static Result<RabbitMqUri, string> From(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return Result.Failure<RabbitMqUri, string>("URI is empty.");
        }

        try
        {
            var uriObj = new Uri(uri);
            if (uriObj is null)
                return Result.Failure<RabbitMqUri, string>("URI is not valid.");

            string scheme = uriObj.Scheme;
            string userInfo = uriObj.UserInfo;
            string host = uriObj.Host;
            int port = uriObj.Port;

            // Split userInfo to get username and password
            var userInfoParts = userInfo.Split(':');
            string username = userInfoParts[0];
            string password = userInfoParts.Length > 1 ? userInfoParts[1] : string.Empty;

            if (string.IsNullOrEmpty(userInfo))
                return Result.Failure<RabbitMqUri, string>("URI does not contain user info.");

            if (string.IsNullOrEmpty(password))
                return Result.Failure<RabbitMqUri, string>("URI does not contain password.");


            return new RabbitMqUri
            {
                Uri = uriObj,
                Username = username,
                Password = password
            };
        }
        catch (Exception ex)
        {
            return Result.Failure<RabbitMqUri, string>(ex.Message);
        }
    }

    public required Uri Uri { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }


}
