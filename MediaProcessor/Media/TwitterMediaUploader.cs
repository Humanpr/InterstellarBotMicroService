using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace MediaProcessor.Media;

public class TwitterMediaUploader : IMediaUploader 
{
    
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TwitterMediaUploader> _logger;
    private TwitterClient _userClient { get; }

    public TwitterMediaUploader(IServiceScopeFactory serviceScopeFactory, ILogger<TwitterMediaUploader> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        
        using var scope = _serviceScopeFactory.CreateScope();

        var credentials = scope.ServiceProvider.GetRequiredService<TwitterCredentials>();
        
        _userClient = new TwitterClient(credentials);
    }
    
    public async Task UploadMedia(byte[] mediaBinary,ProcessMessage message)
    {
        var uploadedVideo = await _userClient.Upload.UploadTweetVideoAsync(mediaBinary);
        await  _userClient.Upload.WaitForMediaProcessingToGetAllMetadataAsync(uploadedVideo);
        
        var reply = await _userClient.Tweets.PublishTweetAsync(new PublishTweetParameters("@" + message.Reply_Tweet_User_Handle + " here is edited")
        {
            InReplyToTweetId = message.Reply_Tweet_ID,
            MediaIds = {uploadedVideo.UploadedMediaInfo.MediaId}
        });
    }
}