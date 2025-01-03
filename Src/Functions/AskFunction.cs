using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.DTOs.Gemini;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;
public class AskFunction : BaseFunction {

    public AskFunction(ILogger<AskFunction> logger, GeminiApi api) : base(logger, api) {

    }

    private GeminiRequestDTO CreateGeminiRequest(QuestionRequestDTO question) {
        var request = new GeminiRequestDTO() {
            Contents = new List<ContentDTO>(){
                    new ContentDTO(){
                        Parts = new List<PartDTO>(){
                            new PartDTO(){
                                Text = question.Question
                            },
                        }
                    }
                }
        };

        if (question.FilesData?.Count > 0) {
            request.Contents[0].Parts.AddRange(question.FilesData.Select(fileData => new PartDTO() {
                FileData = fileData
            }));
        }

        if (!string.IsNullOrWhiteSpace(question.CacheName)) {
            request.CachedContent = question.CacheName;
        }

        return request;
    }


    [Function("ask")]
    public async Task<IActionResult> Ask([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req) {
        _logger.LogInformation("Ask request received.");
        var question = await req.ReadFromJsonAsync<QuestionRequestDTO>();
        if (question is null) {
            return new BadRequestObjectResult("Invalid request body");
        }
        if (!string.IsNullOrWhiteSpace(question.GameName) && string.IsNullOrWhiteSpace(question.CacheName)) {
            question.CacheName = await SearchForCache(question.GameName);
        }

        var geminiRequest = CreateGeminiRequest(question);
        var response = await _api.SendQuestion(geminiRequest);
        if (response is null) {
            return new BadRequestObjectResult("Failed to call Gemini API");
        }
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            return new BadRequestObjectResult(content);
        } else {
            return new OkObjectResult(content);
        }
    }

    [Function("load-game-rules")]
    public async Task<IActionResult> LoadGameRules([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req) {
        _logger.LogInformation("Load Game Rules request received.");
        var gameRulesRequest = await req.ReadFromJsonAsync<UploadGameRulesRequestDTO>();
        if (gameRulesRequest is null) {
            return new BadRequestObjectResult("Invalid ");
        }

        var caches = await _api.ListCaches(gameRulesRequest.GameName);
        if (caches is null || caches.Count == 0) {
            if (gameRulesRequest.RuleFiles is null || gameRulesRequest.RuleFiles.Count == 0) {
                return new BadRequestObjectResult("No rules file was uploaded");
            }

            var savedFiles = new List<string>();
            foreach (var ruleFile in gameRulesRequest.RuleFiles) {
                if (string.IsNullOrWhiteSpace(ruleFile.Uri)) {
                    continue;
                }
                var uploadedFile = await _api.SendPdf(ruleFile);
                if (uploadedFile is not null) {
                    savedFiles.Add(uploadedFile.Uri);
                }
            }
            if (savedFiles.Count == 0) {
                return new BadRequestObjectResult("Failed to save rules files");
            }

            var cacheName = await _api.SaveFilesToCache(gameRulesRequest.GameName,
                savedFiles.Select(uri => new FileDTO() {
                    Uri = uri,
                    MimeType = "application/pdf"
                }).ToList());

            if (string.IsNullOrWhiteSpace(cacheName)) {
                return new StatusCodeResult(500);
            }

            return new OkObjectResult(cacheName);
        }
        return new OkObjectResult(caches?.FirstOrDefault()?.Name);

    }

    private async Task<string?> SearchForCache(string nameOfTheGame) {
        var caches = await _api.ListCaches(nameOfTheGame);
        if (caches is null || caches.Count == 0) {
            return null;
        }
        return caches.First().Name;
    }
}

