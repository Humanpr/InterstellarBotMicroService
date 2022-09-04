using System.Threading.Channels;
using MediaProcessor.Media;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace MediaProcessor.Services;

public class MediaUploadService : BackgroundService
{

    private  ChannelReader<ProcessMessage> _readerChnlProcessMessage { get; }
    private IMediaUploader _mediaUploader;
    private readonly ILogger<MediaUploadService> _logger;
    private readonly string _processedUri;
    private readonly TwitterClient twitterClient;

    
    public MediaUploadService(Channel<ProcessMessage> channel, IMediaUploader mediaUploader,IConfiguration _configuration,ILogger<MediaUploadService> _logger)
    {
        _mediaUploader = mediaUploader;
        this._logger = _logger;
        _readerChnlProcessMessage = channel.Reader;
        _processedUri = _configuration.GetSection("MediaIO").GetValue<string>("ProcessedMediaLocation");
    }
    
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };
        
        await Parallel.ForEachAsync(_readerChnlProcessMessage.ReadAllAsync(stoppingToken),options,async (command,token) =>
        {
            _logger.LogInformation("Media Received for Upload {0}", command.Media_Url);
            var outpath = @$"{_processedUri}{Path.DirectorySeparatorChar}{command.Media_Name}.mp4";
            var mediaBinary = File.ReadAllBytes(outpath);
            await _mediaUploader.UploadMedia(mediaBinary,command);
            _logger.LogInformation($" Tweet with {command.Reply_Tweet_ID} id created by {command.Reply_Tweet_User_Handle} user.. ");
     
        });
    }
}