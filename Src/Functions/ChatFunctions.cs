using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.DTOs.Gemini;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;
public class ChatFunctions : BaseFunction {
    private readonly DatabaseApi _databaseApi;
    public ChatFunctions(ILogger<ChatFunctions> logger, GeminiApi api, DatabaseApi databaseApi) : base(logger, api) {
        _databaseApi = databaseApi;
    }

    private GeminiRequestDTO CreateGeminiRequest(MessageRequestDTO question) {
        var request = new GeminiRequestDTO() {
            Contents = question.Messages
        };

        if (question.FilesData?.Count > 0) {
            request.Contents[0].Parts.AddRange(question.FilesData.Select(fileData => new PartDTO() {
                FileData = fileData
            }));
        }

        if (!string.IsNullOrWhiteSpace(question.GameRulesCacheName)) {
            request.CachedContent = question.GameRulesCacheName;
        }

        return request;
    }


    [Function("send-message")]
    [OpenApiOperation(operationId: "SendMessage")]
    [OpenApiRequestBody("application/json", typeof(MessageRequestDTO))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string))]
    public async Task<IResult> SendMessage([HttpTrigger(AuthorizationLevel.Function, "post", Route = "chat")] HttpRequest req) {
        _logger.LogInformation("Enviando mensagem");
        try {
            var message = await req.ReadFromJsonAsync<MessageRequestDTO>();
            if (message is null) {
                return Results.BadRequest("Requisição inválida.");
            }
            if (string.IsNullOrWhiteSpace(message.GameId)) {
                return Results.BadRequest("Id do jogo não informado");
            }

            if (string.IsNullOrWhiteSpace(message.GameRulesCacheName)) {
                return Results.BadRequest("As regras do jogo não foram carregadas");
            }

            var geminiRequest = CreateGeminiRequest(message);
            var response = await _api.SendQuestion(geminiRequest);
            if (response is null) {
                return Results.Problem("Não foi possível comunicar com a IA", null, 500);
            }

            if (!response.IsSuccessStatusCode) {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao enviar mensgem para a IA. Status Code: {0}. Content: {1}", response.StatusCode, content);
                return Results.Problem("Houve um erro ao comunicar com a IA");
            } else {
                var content = await response.Content.ReadFromJsonAsync<GeminiResponseDTO>();
                if (content is null || content.Candidates.Count == 0 || content.Candidates.First().Content is null || content.Candidates.First().Content.Parts.Count == 0) {
                    return Results.Problem("Erro ao processar a resposta da IA");
                }

                return Results.Ok(content.Candidates
                    .First().Content.Parts
                    .Select(part => part.Text)
                    .Aggregate((a, b) => a + "\n" + b));
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Erro ao tentar enviar uma mensagem");
            return Results.Problem(ex.Message, null, 500);
        }
    }

    [Function("start-chat")]
    [OpenApiOperation(operationId: "StartChat")]
    [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Game Id")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string),
            Description = "The cache name of saved rules")]
    public async Task<IResult> StartChat([HttpTrigger(AuthorizationLevel.Function, "post", Route = "start-chat/{gameId}")] HttpRequest req, string gameId) {
        _logger.LogInformation("Iniciando chat");
        if (string.IsNullOrWhiteSpace(gameId)) {
            return Results.BadRequest("Id do jogo não informado");
        }
        try {
            var cacheName = await SearchForCache(gameId);
            if (string.IsNullOrWhiteSpace(cacheName)) {
                var game = await _databaseApi.Get(gameId);
                if (game is null) {
                    return Results.BadRequest("Jogo não encontrado");
                }
                var systemInstruction = new ContentDTO() {
                    Parts = new List<PartDTO>(){
                    new PartDTO(){
                        Text = $"Você é um especialista nas regras do jogo de tabuleiro '{game.Title}'. Lhe enviamos todos os manuais sobre o jogo e precisamos que nos ajude com qualquer dúvida que surgir."
                    }
                },
                    Role = "system"
                };
                cacheName = await _api.CreateCache(gameId, systemInstruction, game.RulesUri);
            }
            return Results.Ok(cacheName);
        } catch (Exception ex) {
            _logger.LogError(ex, "Erro ao iniciar o chat. Detalhes {0}", ex.Message);
            return Results.Problem("Erro ao iniciar o chat", null, 500);
        }

    }

    private async Task<string?> SearchForCache(string nameOfTheGame) {
        var caches = await _api.ListCaches(nameOfTheGame);
        if (caches is null || caches.Count == 0) {
            return null;
        }
        _logger.LogDebug("Cache found: {0}", caches.First().Name);
        return caches.First().Name;
    }
}

