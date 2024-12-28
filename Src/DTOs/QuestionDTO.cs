namespace ProximoTurno.ManualDoJogo.DTOs;
public class QuestionDTO {
    public string NameOfTheGame { get; set; } = null!;
    public string Question { get; set; } = null!;
    public List<FileData>? FilesData { get; set; }
    public string? CacheName { get; set; } = null!;
}