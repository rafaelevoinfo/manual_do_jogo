using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.DTOs;
using ProximoTurno.ManualDoJogo.DTOs.Gemini;

namespace ProximoTurno.ManualDoJogo.Services;

public class GeminiApi {
    public const string BASE_URL = "https://generativelanguage.googleapis.com";
    public const string MODEL = "models/gemini-1.5-flash-002";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger _logger;

    public GeminiApi(ILogger<GeminiApi> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory) {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = configuration["GOOGLE_API_KEY"] ?? "";
    }

    public async Task<HttpResponseMessage> SendQuestion(GeminiRequestDTO request) {
        var payload = new StringContent(
           JsonSerializer.Serialize(request, new JsonSerializerOptions() {
               PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
               DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
           })
        );
        return await _httpClient.PostAsync($"{BASE_URL}/v1beta/{MODEL}:generateContent?key={_apiKey}", payload);
    }

    public async Task<FileDTO> SendPdf(string pdfUrl) {
        var fileName = Path.GetFileName(pdfUrl);
        var response = await _httpClient.GetAsync(pdfUrl);
        if (!response.IsSuccessStatusCode) {
            _logger.LogError($"Error downloading PDF from {pdfUrl}");
            throw new Exception("Failed to download PDF");
        }

        using var pdfStream = await response.Content.ReadAsStreamAsync();

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-Goog-Upload-Protocol", "resumable");
        _httpClient.DefaultRequestHeaders.Add("X-Goog-Upload-Command", "start");
        _httpClient.DefaultRequestHeaders.Add("X-Goog-Upload-Header-Content-Length", pdfStream.Length.ToString());
        _httpClient.DefaultRequestHeaders.Add("X-Goog-Upload-Header-Content-Type", "application/pdf");

        var payload = new StringContent(JsonSerializer.Serialize(
            new {
                file = new {
                    display_name = fileName
                }
            }
        ));
        response = await _httpClient.PostAsync($"{BASE_URL}/upload/v1beta/files?key={_apiKey}", payload);
        if (response.IsSuccessStatusCode) {
            var resultContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"Upload response: {resultContent}");
            _logger.LogDebug($"Upload response headers: {response.Headers.Select(h => $"{h.Key}: {h.Value}")}");
            if (response.Headers.TryGetValues("x-goog-upload-url", out var values)) {
                var uploadUrl = values.First();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Goog-Upload-Offset", "0");
                _httpClient.DefaultRequestHeaders.Add("X-Goog-Upload-Command", "upload, finalize");

                using var streamContent = new StreamContent(pdfStream);
                using var requestUpload = new HttpRequestMessage(HttpMethod.Post, uploadUrl) {
                    Content = streamContent
                };

                response = await _httpClient.SendAsync(requestUpload);
                if (response.IsSuccessStatusCode) {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Upload response: {responseBody}");
                    var uploadResponse = JsonSerializer.Deserialize<UploadFileResponseDTO>(responseBody, new JsonSerializerOptions() {
                        PropertyNameCaseInsensitive = true
                    });
                    if (uploadResponse?.File?.Uri != null) {
                        return uploadResponse.File;
                    } else {
                        throw new Exception("Uri not found in upload response");
                    }
                } else {
                    throw new Exception("Failed to upload PDF");
                }
            } else {
                throw new Exception("Failed to get upload URL");
            }
        } else {
            throw new Exception("Failed to start upload PDF");
        }

    }

    public async Task<string> SaveFilesToCache(List<FileDTO> filesUri) {
        var cacheRequest = new CacheContentDTO() {
            Model = MODEL,
            Contents = filesUri.Select(file => new ContentDTO() {
                Parts = new List<PartDTO>(){
                    new PartDTO(){
                        FileData = new FileData(){
                            MimeType = file?.MimeType ?? "application/pdf",
                            FileUri = file?.Uri ?? ""
                        }
                    }
                },
                Role = "user",
            }).ToList(),
            SystemInstruction = new ContentDTO() {
                Parts = new List<PartDTO>(){
                    new PartDTO(){
                        Text = "Você é um experte em regras de jogos de tabuleiro. Um grupo de amigos estão jogando e lhe enviaram os pdfs com todas as regras do jogo. Ajude-os com qualquer dúvida sobre o jogo."
                    }
                },
                Role = "system"
            },
            ExpireTime = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        var payload = new StringContent(
            JsonSerializer.Serialize(cacheRequest, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            })
        );
        var response = await _httpClient.PostAsync($"{BASE_URL}/v1beta/cachedContents?key={_apiKey}", payload);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode) {
            _logger.LogDebug($"Cache response: {responseBody}");
            var responseCacheContent = JsonSerializer.Deserialize<CacheContentDTO>(responseBody, new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true
            });
            if (responseCacheContent?.Name != null) {
                return responseCacheContent.Name;
            } else {
                throw new Exception("Name not found in cache response");
            }
        } else {

            throw new Exception("Failed to create cache");
        }
    }

}