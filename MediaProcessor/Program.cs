using System.Threading.Channels;
using MediaProcessor;
using MediaProcessor.Media;
using MediaProcessor.Services;
using Tweetinvi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TwitterCredentials>((services) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    TwitterUserCredentials twitterCreds = new();
    // Setting twitter credentials
    configuration.GetSection(TwitterUserCredentials.LogSection).Bind(twitterCreds);
    //Console.WriteLine("API_KEY: {0} API_KEY_SECRET: {1} ACCESS_TOKEN: {2} ACCESS_TOKEN_SECRET: {3}",twitterCreds.API_KEY, twitterCreds.API_KEY_SECRET, twitterCreds.ACCESS_TOKEN,twitterCreds.ACCESS_TOKEN_SECRET);
    var credentials = new TwitterCredentials(twitterCreds.API_KEY, twitterCreds.API_KEY_SECRET, twitterCreds.ACCESS_TOKEN,
        twitterCreds.ACCESS_TOKEN_SECRET)
    {
        BearerToken = twitterCreds.BEARER
    };
    return credentials;
});
builder.Services.AddSingleton<IMediaUploader, TwitterMediaUploader>();
builder.Services.AddHostedService<MediaProcessServcie>();
builder.Services.AddHostedService<MediaUploadService>();
builder.Services.AddSingleton(Channel.CreateUnbounded<ProcessMessage>());

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();