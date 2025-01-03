using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.DTOs.Gemini;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;
public class FileFunctions : BaseFunction {

    public FileFunctions(ILogger<FileFunctions> logger, GeminiApi api) : base(logger, api) {

    }

    [Function("send-pdf")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req) {
        _logger.LogInformation("Send PDF request received.");
        if (req.Query.TryGetValue("pdfUrl", out var pdfUrl) && pdfUrl.Count > 0 && pdfUrl.FirstOrDefault() != null) {
            string? name = null;
            if (req.Query.TryGetValue("game", out var fileName)) {
                name = fileName.FirstOrDefault();
            }
            var fileDto = await _api.SendPdf(new FileDTO() {
                Name = name,
                Uri = pdfUrl.FirstOrDefault()!
            });
            return new OkObjectResult(fileDto);
        } else {
            return new BadRequestObjectResult("Invalid request body");
        }
    }


}

