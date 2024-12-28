using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.Services;

namespace ProximoTurno.ManualDoJogo.Functions;

public class BaseFunction {
    protected readonly GeminiApi _api;
    protected readonly ILogger _logger;

    public BaseFunction(ILogger logger, GeminiApi geminiApi) {
        _logger = logger;
        _api = geminiApi;
    }
}