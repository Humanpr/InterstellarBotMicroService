namespace MediaProcessor;

public class ProcessMessage
{
    public string Media_Name { get; set; }
    public string Media_Url { get; set; }
    public string Reply_Tweet_ID_Str { get; set; }
    public long? Reply_Tweet_ID { get; set; }
    public string Reply_Tweet_User_Handle { get; set; }
    public decimal? Start { get; set; } = -1;
    public decimal? End { get; set; } = -1;
    public int? VideoDuration { get; set; }
}