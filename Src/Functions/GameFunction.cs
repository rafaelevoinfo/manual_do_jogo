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

    [Function("count-games")]
    [OpenApiOperation(operationId: "CountGames")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(GamesTotalDTO), Description = "Quantidade de jogos cadastrados")]
    public async Task<IResult> CountGames([HttpTrigger(AuthorizationLevel.Function, "get", Route = "count-games")] HttpRequest req) {
        try {
            var count = await _databaseApi.CountGames();
            return Results.Ok(count);
        } catch (Exception ex) {
            return Results.Problem(ex.Message, null, 500);
        }
    }

    [Function("list-games")]
    [OpenApiOperation(operationId: "ListGames")]
    [OpenApiParameter("gameTitle", In = ParameterLocation.Query, Required = false)]
    [OpenApiParameter("page", In = ParameterLocation.Query, Required = false)]
    [OpenApiParameter("itemsPerPage", In = ParameterLocation.Query, Required = false)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<GameDTO>), Description = "Lista de jogos salvos")]
    public async Task<IResult> ListGames(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "list-games")] HttpRequest req) {
        try {
            req.Query.TryGetValue("gameTitle", out var gameTitle);
            req.Query.TryGetValue("page", out var page);
            if (!int.TryParse(page.FirstOrDefault(), out var pageNumber)) {
                pageNumber = 1;
            }
            req.Query.TryGetValue("itemsPerPage", out var itemsPerPageStr);
            if (!int.TryParse(itemsPerPageStr.FirstOrDefault(), out var itemsPerPage)) {
                itemsPerPage = DatabaseApi.ITEMS_PER_PAGE;
            }
            var games = await _databaseApi.List(gameTitle, pageNumber, itemsPerPage);
            return Results.Ok(games ?? new List<GameDTO>());

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

    [Function("get-game")]
    [OpenApiOperation(operationId: "GetGame")]
    [OpenApiParameter("gameId", In = ParameterLocation.Query, Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string))]
    public async Task<IResult> GetGame([HttpTrigger(AuthorizationLevel.Function, "get", Route = "get-game/{gameId?}")] HttpRequest req, string gameId) {
        var game = await _databaseApi.Get(gameId);
        if (game is not null) {
            return Results.Ok(game);
        } else {
            return Results.NotFound("Não foi possível encontrar o jogo");
        }
    }


}