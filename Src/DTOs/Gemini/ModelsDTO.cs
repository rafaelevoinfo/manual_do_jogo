namespace ProximoTurno.ManualDoJogo.DTOs.Gemini;

public class ListModelDTO {
    public List<ModelDTO>? Models { get; set; }
    public string? NextPageToken { get; set; }
}


public class ModelDTO {
    public string? Name { get; set; }
    public string? BaseModelId { get; set; }
    public string? Version { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int InputTokenLimit { get; set; }
    public int OutputTokenLimit { get; set; }
    public List<string>? SupportedGenerationMethods { get; set; }
    public double Temperature { get; set; }
    public double MaxTemperature { get; set; }
    public double TopP { get; set; }
    public int TopK { get; set; }
}