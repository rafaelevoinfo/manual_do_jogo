using ProximoTurno.ManualDoJogo.DTOs.Gemini;

namespace ProximoTurno.ManualDoJogo.DTOs;
public class MessageRequestDTO {
    public string GameId { get; set; } = null!;
    public List<ContentDTO> Messages { get; set; } = null!;
    public List<FileData>? FilesData { get; set; }
    public string? GameRulesCacheName { get; set; } = null!;
}

