using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.DTOs.Gemini;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;
public class CacheFunctions : BaseFunction {
    public CacheFunctions(ILogger<CachedContentDTO> logger, GeminiApi geminiApi) : base(logger, geminiApi) {
    }

    [Function("save-cache")]
    [OpenApiOperation(operationId: "SaveCache")]
    [OpenApiRequestBody("application/json", typeof(SaveCacheRequestDTO))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Nome do cache salvo")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "cache")] HttpRequest req) {
        _logger.LogInformation("Save Cache request received.");
        var saveCacheRequest = await req.ReadFromJsonAsync<SaveCacheRequestDTO>();
        if (saveCacheRequest is null || saveCacheRequest.Files is null || saveCacheRequest.Files.Count == 0) {
            return new BadRequestObjectResult("Invalid request body");
        }
        var cacheName = await _api.CreateCache(saveCacheRequest.CacheName, new ContentDTO() {
            Parts = new List<PartDTO>(){
                new PartDTO(){
                    Text = saveCacheRequest.SystemInstruction
                }
            },
            Role = "system"
        }, saveCacheRequest.Files);
        return new OkObjectResult(cacheName);
    }

    [Function("list-cache")]
    [OpenApiOperation(operationId: "List")]
    [OpenApiParameter("game", In = ParameterLocation.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<CachedContentDTO>),
            Description = "Lista de caches salvos")]
    public async Task<IResult> List([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req) {
        _logger.LogInformation("List Cache request received.");
        req.Query.TryGetValue("game", out var game);
        var caches = await _api.ListCaches(game.FirstOrDefault() ?? "");
        return Results.Ok(caches);
    }

    [Function("delete-cache")]
    [OpenApiOperation(operationId: "Delete")]
    [OpenApiParameter("name", In = ParameterLocation.Path, Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string))]
    public async Task<IResult> Delete([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "delete-cache/{name}")] HttpRequest req, string name) {
        if (await _api.DeleteCache(name)) {
            return Results.Ok();
        } else {
            return Results.BadRequest("Não foi possível deletar o cache");
        }
    }






}