namespace ProximoTurno.ManualDoJogo.DTOs.Gemini;

public class ListCachedContentDTO {
    public List<CachedContentDTO>? CachedContents { get; set; }
    public string? NextPageToken { get; set; }
}

public class CachedContentDTO {
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public DateTime? ExpireTime { get; set; }
    public string? Ttl { get; set; }
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    //   "contents": [
    //     {
    //       object (Content)
    //     }
    //   ],
    //   "tools": [
    //     {
    //       object (Tool)
    //     }
    //   ],
    //   "createTime": string,
    //   "updateTime": string,
    //   "usageMetadata": {
    //     object (UsageMetadata)
    //   },

    //   // Union field expiration can be only one of the following:
    //   "expireTime": string,
    //   "ttl": string
    //   // End of list of possible types for union field expiration.
    //   "name": string,
    //   "displayName": string,
    //   "model": string,
    //   "systemInstruction": {
    //     object (Content)
    //   },
    //   "toolConfig": {
    //     object (ToolConfig)
    //   }
}