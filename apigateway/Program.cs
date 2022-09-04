using apigateway.Messages;
using apigateway.MessagingClient;
using apigateway.Services;
using apigateway.TwitterAuth;
using Tweetinvi;
using Tweetinvi.AspNet;
using Tweetinvi.Core.DTO;
using Tweetinvi.Models;
using HttpMethod = Tweetinvi.Models.HttpMethod;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMessagingClient, MyRabbitMQClient>();
builder.Services.AddSingleton<TwitterCredentials>((services) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    TwitterUserCredentials twitterCreds = new();
    // Setting twitter credentials
    configuration.GetSection(TwitterUserCredentials.LogSection).Bind(twitterCreds);
    var credentials = new TwitterCredentials(twitterCreds.API_KEY, twitterCreds.API_KEY_SECRET, twitterCreds.ACCESS_TOKEN,
        twitterCreds.ACCESS_TOKEN_SECRET)
    {
        BearerToken = twitterCreds.BEARER
    };
    return credentials;
});

builder.Services.AddHostedService<WebhookRegistererService>();

builder.Services.AddEndpointsApiExplorer();
Plugins.Add<AspNetPlugin>(); 

var app = builder.Build();
app.MapGet("/", () => "Hello World!");

using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;
    
    ILogger<Program> _logger = services.GetRequiredService<ILogger<Program>>();
    var _configuration = services.GetRequiredService<IConfiguration>();
    
    var railwayUrl = _configuration.GetValue<string>("RAILWAY_STATIC_URL");
    var defaultUrl = _configuration.GetValue<string>("AppUrl");
    var webhookEndpointUrl = "https://"+ (railwayUrl ?? defaultUrl) +"/webhooks/twitter";
    _logger.LogInformation($" URL {webhookEndpointUrl}");
    var credentials = services.GetRequiredService<TwitterCredentials>();
    _logger.LogInformation($" Acces token {credentials.AccessToken}  Bearer {credentials.BearerToken} ConsumerKey {credentials.ConsumerKey} SonsSecret {credentials.ConsumerSecret} AccessTokewnSeceret {credentials.AccessTokenSecret}");
    
    var twitterClient = new TwitterClient(credentials);
    var requestHandler = twitterClient.AccountActivity.CreateRequestHandler();
    var config = new WebhookMiddlewareConfiguration(requestHandler);
    
#region account_activity
    
    var serviceFactory = services.GetRequiredService<IServiceScopeFactory>();
    
    var accountActivityStream = requestHandler.GetAccountActivityStream(1030093491559378944, "dev"); // todo user_id
    
    accountActivityStream.TweetCreated += async (sender, tweetCreatedEvent) =>
    {
        using (var serviceScope = serviceFactory.CreateScope())
        {
            ILogger<Program> logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var client = serviceScope.ServiceProvider.GetRequiredService<IMessagingClient>();
            
            try
            {
                // Checking if received tweet contains any bot mention
                if (tweetCreatedEvent.Tweet.Entities.UserMentions.Any(user => user.Id == 1030093491559378944))
                {
                    logger.LogInformation($"A tweet was created by USER_ID or a tweet mentioning USER_ID {tweetCreatedEvent.GetType()} {tweetCreatedEvent.InResultOf.ToString()}");
                    ITweet replyTweet = tweetCreatedEvent.Tweet;
                    var tweetId = replyTweet.InReplyToStatusIdStr ?? replyTweet.IdStr;
                    // getting media tweet
                    var result = await twitterClient.Execute.RequestAsync<TweetDTO>(request =>
                    {
                        request.Url = $"https://api.twitter.com/1.1/statuses/show.json?id={tweetId}&tweet_mode=extended";
                        request.HttpMethod = HttpMethod.GET;
                    });
     
                    ITweet mediaTweet = twitterClient.Factories.CreateTweet(result.Model);
                
                    if (mediaTweet.Entities.Medias.Count is 0)
                    {
                        logger.LogInformation("MEDIA NOT FOUND");
                        return;
                    }
                    if (mediaTweet.Entities.Medias.Where(m => m.MediaType is "video").Count() is 0)
                    {
                        logger.LogInformation("VIDEO NOT FOUND");
                        return;
                    }
                    
                    var message = new ProcessMessage(mediaTweet, replyTweet).ToString();
                    _logger.LogInformation("Message sended.. {0}",message);
                    client.SendToProcess(message);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.StackTrace);
            }
        }
    };
#endregion
    
app.UseTweetinviWebhooks(config);
app.Run();
}