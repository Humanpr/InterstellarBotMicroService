using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MediaProcessor.Services;

public class MediaProcessServcie : BackgroundService
{
    private readonly ILogger<MediaProcessServcie> _logger;
    private IConnection _connection;
    private IModel _channel;
    private readonly string _audiopath;
    private readonly string _processedUri;
    private readonly string _ffmpeguri;
    private string _outpath;

    private  ChannelWriter<ProcessMessage> _writerChnlProcessMessage { get; init; }
    
    public MediaProcessServcie(ILogger<MediaProcessServcie> logger, IConfiguration _configuration,Channel<ProcessMessage> internalChannel)
    {
        _logger = logger;
        var connectionFac = new ConnectionFactory() {HostName = "rabbit-service"};
        _connection = connectionFac.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: "MediaProcessExchange", type: ExchangeType.Direct);
        _channel.QueueDeclare("processqueue", false, false);
        _channel.QueueBind("processqueue", "MediaProcessExchange", "process");

        _audiopath = _configuration.GetSection("MediaIO").GetValue<string>("SampleAudio");
        _processedUri = _configuration.GetSection("MediaIO").GetValue<string>("ProcessedMediaLocation");
        _ffmpeguri = _configuration.GetSection("MediaIO").GetValue<string>("FFMPEG");
        _logger.LogInformation("audiopath: {0} processedUri: {1} ffmpeguri: {2} ",_audiopath,_processedUri,_ffmpeguri);
        _writerChnlProcessMessage = internalChannel.Writer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] {0}", message);
            var messageObj = JsonSerializer.Deserialize<ProcessMessage>(message);

            var videoUrl = messageObj.Media_Url;
            _outpath = @$"{_processedUri}{Path.DirectorySeparatorChar}{messageObj.Media_Name}.mp4";

            _logger.LogInformation(
                $"audiolpoc {_audiopath} processedloc {_processedUri} ffmpeg {_ffmpeguri} out {_outpath}");

            var processinfo = new ProcessStartInfo
            {
                FileName = _ffmpeguri,
                WorkingDirectory = _processedUri,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            if (messageObj.Start == -1 || messageObj.End == -1 ||
                messageObj.VideoDuration < messageObj.End - messageObj.Start)
            {
                processinfo.Arguments =
                    $"-y -i {videoUrl} -ss 134 -i {_audiopath} -map 0:v -map 1:a -c:v copy -shortest {_outpath}";
            }
            else
            {
                processinfo.Arguments =
                    $"-y -i {videoUrl}  -i {_audiopath} -filter_complex \"[0:a]volume=0:enable=between(t\\,{messageObj.Start}\\,{messageObj.End})[a0];[1:a]atrim=0:{messageObj.End - messageObj.Start},adelay={messageObj.Start}s:all=1[a1];[a0][a1]amix=normalize=0:duration=first[aout]\" -map 0:v  -map [aout] -c:v copy {_outpath}";
            }

            _logger.LogInformation($"Command : {processinfo.Arguments} ");
            using var process = new Process {StartInfo = processinfo};
            try
            {
                process.Start();

                await process.StandardOutput.ReadToEndAsync(); // waitforexitasync not working
                _logger.LogInformation("Media Processed {0}", messageObj.Media_Url);
                await _writerChnlProcessMessage.WriteAsync(messageObj); // sending message to be uploaded
                _logger.LogInformation("Media Sended for Upload {0}", messageObj.Media_Url);
                
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR PROCESS START.. ");
                throw;
            }
        };

        _channel.BasicConsume(queue: "processqueue",
            autoAck: true,
            consumer: consumer);

        _logger.LogInformation("Waiting for message..");
        return Task.CompletedTask;
    }
}