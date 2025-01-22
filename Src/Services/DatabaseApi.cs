using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.DTOs;

namespace ProximoTurno.ManualDoJogo.Services;

public class DatabaseApi {
    private readonly CosmosClient _cosmosClient;
    private Database _database = null!;
    private Container _container = null!;
    private static DatabaseApi _instance = null!;
    private ILogger<DatabaseApi> _logger;

    public DatabaseApi(ILogger<DatabaseApi> logger, IConfiguration configuration) {
        _logger = logger;
        _cosmosClient = new CosmosClient(configuration["COSMO_ENDPOINT_URI"], configuration["COSMO_PRIMARY_KEY"],
            new CosmosClientOptions() {
                ApplicationName = "ManualJogo",
                SerializerOptions = new CosmosSerializationOptions() {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
    }

    public static DatabaseApi Init(ILogger<DatabaseApi> logger, IConfiguration configuration) {
        if (_instance is null) {
            _instance = new DatabaseApi(logger, configuration);
            _instance.CreateDatabaseStructure();
        }

        return _instance;

    }

    public async Task<bool> DeleteGame(string id) {
        try {
            var response = await _container.DeleteItemAsync<GameDTO>(id, new PartitionKey(id));
            _logger.LogDebug($"Delete Game Status Code: {response.StatusCode}");
            return response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent;
        } catch (Exception e) {
            _logger.LogError("Erro ao excluir um jogo. Detalhes: {error}", e);
        }
        return false;
    }

    public async Task<List<GameDTO>?> List(string? idOfGame) {
        var games = new List<GameDTO>();
        try {
            var query = new StringBuilder("SELECT * FROM games");
            if (!string.IsNullOrWhiteSpace(idOfGame)) {
                query.Append($" where games.id like '%{idOfGame}%' ");
            }
            var querySql = query.ToString();

            _logger.LogDebug("Running query: {0}\n", querySql);

            QueryDefinition queryDefinition = new QueryDefinition(querySql);
            var resultIterator = _container.GetItemQueryIterator<GameDTO>(queryDefinition);
            while (resultIterator.HasMoreResults) {
                var itens = await resultIterator.ReadNextAsync();
                games.AddRange(itens.ToList());
            }
            return games;
        } catch (Exception e) {
            _logger.LogError("Erro ao buscar os jogos. Detalhes: {error}", e);
        }

        return null;
    }

    public async Task<bool> SaveGame(GameDTO game) {
        if (game.RulesUri is null || game.RulesUri.Count == 0) {
            _logger.LogError("Nenhum manual de regras foi informado.");
            return false;
        }
        try {
            // Read the item to see if it exists.  
            ItemResponse<GameDTO> gameResponse = await _container.ReadItemAsync<GameDTO>(game.Id, new PartitionKey(game.Id));
            var gameSaved = gameResponse.Resource;
            gameSaved.Title = game.Title;
            gameSaved.ImageUrl = game.ImageUrl;
            gameSaved.Publisher = game.Publisher;
            gameSaved.Description = game.Description;
            gameSaved.RulesUri = game.RulesUri;
            var response = await _container.ReplaceItemAsync<GameDTO>(gameSaved, gameSaved.Id);
            return ((int)response.StatusCode == 200) || ((int)response.StatusCode == 201);
        } catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            try {
                ItemResponse<GameDTO> response = await _container.CreateItemAsync<GameDTO>(game, new PartitionKey(game.Id));
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                _logger.LogDebug("Created item in database with id: {0} Operation consumed {1} RUs.\n", response.Resource.Id, response.RequestCharge);
                return ((int)response.StatusCode == 200) || ((int)response.StatusCode == 201);
            } catch (Exception e) {
                _logger.LogError("Erro ao incluir um jogo. Detalhes: {error}", e);
                return false;
            }

        } catch (Exception e) {
            _logger.LogError("Erro ao incluir um jogo. Detalhes: {error}", e);
            return false;
        }


    }


    private void CreateDatabaseStructure() {
        _database = _cosmosClient.CreateDatabaseIfNotExistsAsync("manual").Result;
        _container = _database.CreateContainerIfNotExistsAsync("games", "/id").Result;
    }
}