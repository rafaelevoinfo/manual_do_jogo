using System.Net;
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
    // public const string MODEL = "models/gemini-2.0-flash-exp";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger _logger;

    public GeminiApi(ILogger<GeminiApi> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory) {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = configuration["GOOGLE_API_KEY"] ?? "";
    }

    public async Task<List<ModelDTO>> ListModels() {
        _logger.LogInformation($"Listando models");
        var response = await _httpClient.GetAsync($"{BASE_URL}/v1beta/models?key={_apiKey}");
        var responseBody = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode) {
            var responseModels = JsonSerializer.Deserialize<ListModelDTO>(responseBody, new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true
            });
            if (responseModels != null) {
                return responseModels.Models ?? new List<ModelDTO>();
            } else {
                throw new Exception("Models not found in list models response");
            }
        } else {
            throw new Exception("Failed to list models");
        }
    }

    public async Task<CachedContentDTO?> GetCache(string name) {
        _logger.LogInformation($"Buscando cache {name}");
        var response = await _httpClient.GetAsync($"{BASE_URL}/v1beta/cachedContents/{name}?key={_apiKey}");
        var responseBody = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode) {
            var cache = JsonSerializer.Deserialize<CachedContentDTO>(responseBody, new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true
            });
            if (cache is not null) {
                return cache;
            } else {
                throw new Exception("Cache not found in get cache response");
            }
        } else if ((int)response.StatusCode != 500) {
            return null;
        } else {
            throw new Exception("Failed to get cache");
        }
    }

    public async Task<List<CachedContentDTO>> ListCaches(string? game) {
        _logger.LogInformation($"Listando caches");
        var response = await _httpClient.GetAsync($"{BASE_URL}/v1beta/cachedContents?key={_apiKey}");
        var responseBody = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode) {
            var caches = JsonSerializer.Deserialize<ListCachedContentDTO>(responseBody, new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true
            });
            if (caches is not null) {
                if (string.IsNullOrWhiteSpace(game)) {
                    return caches.CachedContents ?? new List<CachedContentDTO>();
                }
                return (caches.CachedContents ?? new List<CachedContentDTO>())
                    .Where(c => string.Compare(c.DisplayName, game, StringComparison.InvariantCultureIgnoreCase) == 0)
                    .ToList();

            } else {
                _logger.LogError($"Não foi possível interpretar a resposta dos caches. Detalhes: {responseBody}");
                throw new Exception("Não foi possível interpretar a resposta dos caches");
            }
        } else {
            _logger.LogError($"Falha ao tentar listar caches. Detalhes: {responseBody}");
            throw new Exception("Falha ao tentar listar caches.");
        }
    }

    public async Task<bool> DeleteCache(string name) {
        var cacheName = WebUtility.UrlDecode(name);
        _logger.LogInformation($"Deletando o cache {cacheName}");
        var response = await _httpClient.DeleteAsync($"{BASE_URL}/v1beta/{cacheName}?key={_apiKey}");

        if (response.IsSuccessStatusCode) {
            return true;
        } else {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Falha ao tentar excluir o cache {cacheName}. Detalhes: {responseBody}");
            return false;
        }
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

    public async Task<FileDTO> SendPdf(FileDTO file) {
        _logger.LogInformation($"Downloading PDF from {file.Uri}");
        var response = await _httpClient.GetAsync(file.Uri);
        if (!response.IsSuccessStatusCode) {
            _logger.LogError($"Error downloading PDF from {file.Uri}");
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
                    display_name = file.Name
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

    public async Task<string> CreateCache(string displayName, ContentDTO systemInstruction, List<FileDTO> files) {
        if (files.Count == 0) {
            throw new Exception("Nenhum arquivo foi informado para salvar no cache");
        }
        if (systemInstruction is null) {
            throw new Exception("Instrução do sistema não informada");
        }

        var uploadedFiles = new List<FileDTO>();
        foreach (var file in files) {
            if (!string.IsNullOrWhiteSpace(file.Uri)) {
                var uploadFile = await SendPdf(file);
                uploadedFiles.Add(uploadFile);
            }
        }

        var cacheRequest = new CacheContentDTO() {
            Model = MODEL,
            DisplayName = displayName,
            //Name = $"cachedContents/{name}",//NAO setar, gerado automaticamente pelo google
            Contents = uploadedFiles.Select(file => new ContentDTO() {
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
            SystemInstruction = systemInstruction,
            ExpireTime = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        var payload = new StringContent(
            JsonSerializer.Serialize(cacheRequest, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            })
        );
        _logger.LogInformation($"Criando cache");
        _logger.LogInformation(await payload.ReadAsStringAsync());
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
            throw new Exception($"Failed to create cache. Detalhes: {responseBody}");
        }
    }

}