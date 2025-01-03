
using ProximoTurno.ManualDoJogo.DTOs.Gemini;

namespace ProximoTurno.ManualDoJogo.DTOs;
public class UploadGameRulesRequestDTO {
    public string GameName { get; set; } = null!;
    public List<FileDTO>? RuleFiles { get; set; }

}