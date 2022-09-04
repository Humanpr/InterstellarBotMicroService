namespace apigateway.TwitterAuth;

public class TwitterUserCredentials
{
    public const string LogSection = "TweeterKeys";
    
    public string API_KEY { get; set; }
    public string API_KEY_SECRET { get; set; }
    public string ACCESS_TOKEN { get; set; }
    public string ACCESS_TOKEN_SECRET { get; set; }
    
    public string BEARER { get; set; }

    public override string ToString()
    {
        return
            $" API_KEY {API_KEY} API_KEY_SECRET {API_KEY_SECRET} ACCESS_TOKEN {ACCESS_TOKEN} ACCESS_TOKEN_SECRET {ACCESS_TOKEN_SECRET} BEARER {BEARER}";
    }
}