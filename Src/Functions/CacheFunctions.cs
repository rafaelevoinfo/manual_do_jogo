using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.DTOs.Gemini;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;
public class CacheFunctions : BaseFunction {
    public CacheFunctions(ILogger<CachedContentDTO> logger, GeminiApi geminiApi) : base(logger, geminiApi) {
    }

    [Function("save-cache")]
    public async Task<IActionResult> SaveCache([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req) {
        _logger.LogInformation("Save Cache request received.");
        var saveCacheRequest = await req.ReadFromJsonAsync<SaveCacheRequestDTO>();
        if (saveCacheRequest is null || saveCacheRequest.Files is null || saveCacheRequest.Files.Count == 0) {
            return new BadRequestObjectResult("Invalid request body");
        }
        var cacheName = await _api.SaveFilesToCache(saveCacheRequest.CacheName, saveCacheRequest.Files);
        return new OkObjectResult(cacheName);
    }

    [Function("cache")]
    public async Task<IActionResult> List([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req) {
        _logger.LogInformation("List Cache request received.");
        if (req.Query.TryGetValue("game", out var game)) {
            var caches = await _api.ListCaches(game.FirstOrDefault() ?? "");
            return new OkObjectResult(caches);
        } else {
            return new BadRequestObjectResult("Nome do jogo não informado");
        }
    }
}