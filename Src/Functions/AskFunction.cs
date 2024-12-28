using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;
public class AskFunction : BaseFunction {

    public AskFunction(ILogger<AskFunction> logger, GeminiApi api) : base(logger, api) {

    }

    private GeminiRequestDTO CreateGeminiRequest(QuestionDTO question) {
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
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req) {
        _logger.LogInformation("Ask request received.");
        var question = await req.ReadFromJsonAsync<QuestionDTO>();
        if (question is null) {
            return new BadRequestObjectResult("Invalid request body");
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
}

