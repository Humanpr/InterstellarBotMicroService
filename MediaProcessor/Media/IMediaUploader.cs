namespace MediaProcessor.Media;

public interface IMediaUploader
{
    public Task UploadMedia(byte[] mediaBinary,ProcessMessage message);
}