using ProximoTurno.ManualDoJogo.DTOs.Gemini;

namespace ProximoTurno.ManualDoJogo.DTOs;

public class GameDTO {
    public string Id { get; set; }
    public string Title { get; set; }
    public string ImageUrl { get; set; }
    public string? Publisher { get; set; }
    public string? Description { get; set; }
    public List<FileDTO> RulesUri { get; set; }
}