namespace ProximoTurno.ManualDoJogo.DTOs.Gemini;

public class GeminiResponseDTO {
    public List<CandidateDTO> Candidates { get; set; }
    public UsageMetadataDTO UsageMetadataDTO { get; set; }
    public string ModelVersion { get; set; }
}

public class CandidateDTO {
    public ContentDTO Content { get; set; }
    public string FinishReason { get; set; }
    public double AvgLogprobs { get; set; }
}

public class UsageMetadataDTO {
    public int PromptTokenCount { get; set; }
    public int CandidatesTokenCount { get; set; }
    public int TotalTokenCount { get; set; }
}