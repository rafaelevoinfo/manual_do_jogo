using System.Text.Json.Serialization;

namespace ProximoTurno.ManualDoJogo.DTOs.Gemini;

public class GeminiRequestDTO {
    public List<ContentDTO> Contents { get; set; } = null!;

    [JsonPropertyName("system_instructions")]
    public ContentDTO SystemInstructions { get; set; } = null!;

    public string? CachedContent { get; set; }

}

public class CacheContentDTO {
    public string? Model { get; set; }
    public List<ContentDTO>? Contents { get; set; }

    [JsonPropertyName("system_instruction")]
    public ContentDTO? SystemInstruction { get; set; }
    public string? Ttl { get; set; }
    [JsonPropertyName("expire_time")]
    public string? ExpireTime { get; set; }
    public string? Name { get; set; }
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

}


public class ContentDTO {
    public List<PartDTO> Parts { get; set; } = null!;
    public string Role { get; set; } = null!;
}

public class PartDTO {
    public string Text { get; set; } = null!;
    [JsonPropertyName("file_data")]
    public FileData? FileData { get; set; }
}

public class FileData {
    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = null!;
    [JsonPropertyName("file_uri")]
    public string FileUri { get; set; } = null!;
}