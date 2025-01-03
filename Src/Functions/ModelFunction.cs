using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;
public class ModelFunction : BaseFunction {

    public ModelFunction(ILogger<ModelFunction> logger, GeminiApi api) : base(logger, api) {

    }

    [Function("list-models")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req) {
        _logger.LogInformation("List models request received.");
        var response = await _api.ListModels();
        return new OkObjectResult(response);
    }
}