using ProximoTurno.ManualDoJogo.DTOs.Gemini;

namespace ProximoTurno.ManualDoJogo.DTOs;
public class SaveCacheRequestDTO {
    public string CacheName { get; set; } = null!;
    public string SystemInstruction { get; set; } = null!;
    public List<FileDTO>? Files { get; set; }
}