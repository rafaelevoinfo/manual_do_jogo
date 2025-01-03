namespace ProximoTurno.ManualDoJogo.DTOs.Gemini;

public class UploadFileResponseDTO {
    public FileDTO? File { get; set; }
}

public class FileDTO {
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string Uri { get; set; } = null!;
    public string? MimeType { get; set; }
    public string? SizeBytes { get; set; }
    public string? CreateTime { get; set; }
    public string? UpdateTime { get; set; }
    public string? ExpirationTime { get; set; }
    public string? Sha256Hash { get; set; }
    public string? State { get; set; }
    public Status? Error { get; set; }

}

public class Status {
    public string? Code { get; set; }
    public string? Message { get; set; }
    // public string? Details { get; set; }
}