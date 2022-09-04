using System.Text.Json;
using System.Text.RegularExpressions;
using Tweetinvi.Models;

namespace apigateway.Messages;

public class ProcessMessage
{
    public string Media_Name { get; }
    public string Media_Url { get;  }
    public string Reply_Tweet_ID_Str { get;  }
    public long? Reply_Tweet_ID { get; }
    public string Reply_Tweet_User_Handle { get;}
    public decimal? Start { get; } = -1;
    public decimal? End { get; } = -1;
    public int? VideoDuration { get; }
    
    private const string TimeIntervalPattern = @"start ([0-9][0-9]?[0-9]?) end ([0-9][0-9]?[0-9]?)";
    
    public ProcessMessage(ITweet mediaTweet,ITweet replyTweet)
    {
        Media_Name = mediaTweet.IdStr;
        var matches = Regex.Match(replyTweet.Text, TimeIntervalPattern).Groups;
        if (matches.Count == 3)
        {
            Start = Convert.ToDecimal(matches[1].Value);
            End = Convert.ToDecimal(matches[2].Value);
        }
        
        Media_Url = mediaTweet.Entities?.Medias.First(m => m.MediaType is "video").VideoDetails.Variants.First().URL;
        VideoDuration = mediaTweet.Entities?.Medias.First(m => m.MediaType is "video").VideoDetails.DurationInMilliseconds ?? -1;
        Reply_Tweet_User_Handle = replyTweet.CreatedBy.ToString() ?? throw new ArgumentNullException(nameof(Reply_Tweet_User_Handle),"Reply user handle can't be null!");
        Reply_Tweet_ID = replyTweet.Id;
        Reply_Tweet_ID_Str = replyTweet.IdStr;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}