using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.DTOs.Gemini;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;
public class FileFunction : BaseFunction {

    public FileFunction(ILogger<FileFunction> logger, GeminiApi api) : base(logger, api) {

    }

    [Function("send-pdf")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req) {
        _logger.LogInformation("Send PDF request received.");
        if (req.Query.TryGetValue("pdfUrl", out var pdfUrl)) {
            var fileDto = await _api.SendPdf(pdfUrl!);
            return new OkObjectResult(fileDto);
        } else {
            return new BadRequestObjectResult("Invalid request body");
        }
    }

    [Function("save-cache")]
    public async Task<IActionResult> SaveCache([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req) {
        _logger.LogInformation("Save Cache request received.");
        var files = await req.ReadFromJsonAsync<List<FileDTO>>();
        if (files is null || files.Count == 0) {
            return new BadRequestObjectResult("Invalid request body");
        }
        var cacheName = await _api.SaveFilesToCache(files);
        return new OkObjectResult(cacheName);
    }
}

