using Tweetinvi;
using Tweetinvi.Models;

namespace apigateway.Services;

public class WebhookRegistererService : BackgroundService
{
    private readonly ILogger<WebhookRegistererService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public WebhookRegistererService(ILogger<WebhookRegistererService> logger,IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = _serviceScopeFactory.CreateScope();
        var credentials = scope.ServiceProvider.GetRequiredService<TwitterCredentials>();
        var _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        _logger.LogInformation($"WebhookInitiator Started {DateTime.Now}");
        await Task.Delay(7000); // wait for other services to start
        
        // todo get bearer if not configured
        var userClient = new TwitterClient(credentials);
        _logger.LogInformation($" Acces token {credentials.AccessToken}  Bearer {credentials.BearerToken} ConsumerKey {credentials.ConsumerKey} SonsSecret {credentials.ConsumerSecret} AccessTokewnSeceret {credentials.AccessTokenSecret}");
        
        var webhooks = await userClient.AccountActivity.GetAccountActivityEnvironmentWebhooksAsync("dev");
        
        if (webhooks.Length is not 0)
        {
            _logger.LogInformation($" Already registered webhook deleting old one...");
            await userClient.AccountActivity.DeleteAccountActivityWebhookAsync("dev", webhooks.First().Id);
            _logger.LogInformation($" Old one deleted...");
        }
        
        _logger.LogInformation($"Registering new webhook..");
        
        //todo: check operations for failure
        var railwayUrl = _configuration.GetValue<string>("RAILWAY_STATIC_URL");
        var defaultUrl = _configuration.GetValue<string>("AppUrl");
        
        var webhookEndpointUrl = "https://"+ (railwayUrl ?? defaultUrl) +"/webhooks/twitter";
        _logger.LogInformation($" URL {webhookEndpointUrl}");
        
        var env = _configuration.GetValue<string>("WebhookEnv") ?? "dev";
        await userClient.AccountActivity.CreateAccountActivityWebhookAsync(env, webhookEndpointUrl);
        _logger.LogInformation($"Successfully registered new webhook..");
        // Register user to registered webhook
        _logger.LogInformation($"Subscribing the user..");
        await userClient.AccountActivity.SubscribeToAccountActivityAsync("dev");
        _logger.LogInformation($"Successfully subscribed the user..");
        _logger.LogInformation($"WebhookInitiator Ended {DateTime.Now}");
    }
}