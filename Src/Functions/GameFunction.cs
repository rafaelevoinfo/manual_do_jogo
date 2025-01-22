using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Configuration;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.Services;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;

namespace ProximoTurno.ManualDoJogo.Functions;
public class GameFunctions {
    private readonly ILogger<GameFunctions> _logger;
    private readonly DatabaseApi _databaseApi;
    public GameFunctions(ILogger<GameFunctions> logger, IConfiguration configuration, DatabaseApi databaseApi) {
        _logger = logger;
        _databaseApi = databaseApi;

    }

    [Function("list-games")]
    [OpenApiOperation(operationId: "ListGames")]
    [OpenApiParameter("gameId", In = ParameterLocation.Query, Required = false)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<GameDTO>), Description = "Lista de jogos salvos")]
    public async Task<IResult> ListGames(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "list-games")] HttpRequest req) {
        try {
            req.Query.TryGetValue("gameId", out var gameId);
            var games = await _databaseApi.List(gameId);
            if (games is null) {
                return Results.Problem("Não foi possível realizar a busca.", null, 500);
            }
            return Results.Ok(games);

        } catch (Exception ex) {
            return Results.Problem(ex.Message, null, 500);
        }
    }

    [Function("save-game")]
    [OpenApiOperation(operationId: "SaveGame")]
    [OpenApiRequestBody("application/json", typeof(GameDTO))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string))]
    public async Task<IResult> SaveGame([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, string id) {
        var gameDto = await req.ReadFromJsonAsync<GameDTO>();
        if (await _databaseApi.SaveGame(gameDto!)) {
            return Results.Ok();
        } else {
            return Results.BadRequest("Não foi possível salvar o jogo");
        }
    }

    [Function("delete-game")]
    [OpenApiOperation(operationId: "DeleteGame")]
    [OpenApiParameter("gameId", In = ParameterLocation.Query, Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string))]
    public async Task<IResult> DeleteGame([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "delete-game/{gameId?}")] HttpRequest req, string gameId) {
        if (await _databaseApi.DeleteGame(gameId)) {
            return Results.Ok();
        } else {
            return Results.BadRequest("Não foi possível excluir o jogo");
        }
    }


}