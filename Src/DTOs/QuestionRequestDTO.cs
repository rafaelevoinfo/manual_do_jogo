using ProximoTurno.ManualDoJogo.DTOs.Gemini;

namespace ProximoTurno.ManualDoJogo.DTOs;
public class QuestionRequestDTO {
    public string GameName { get; set; } = null!;
    public string Question { get; set; } = null!;
    public List<FileData>? FilesData { get; set; }
    public string? CacheName { get; set; } = null!;
}