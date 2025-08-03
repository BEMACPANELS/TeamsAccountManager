using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace TeamsAccountManager.Services
{
    /// <summary>
    /// MCP (Model Context Protocol) サービス
    /// </summary>
    public class McpService
    {
        private readonly ILogger<McpService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, DateTime> _cache;
        private readonly Dictionary<string, object> _cacheData;

        public McpService(ILogger<McpService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
            _cache = new Dictionary<string, DateTime>();
            _cacheData = new Dictionary<string, object>();

            // タイムアウト設定
            _httpClient.Timeout = TimeSpan.FromSeconds(
                _configuration.GetValue<int>("MCP:MicrosoftLearn:TimeoutSeconds", 10));
        }

        /// <summary>
        /// Microsoft Learn MCP Server からドキュメントを検索
        /// </summary>
        public async Task<McpSearchResult> SearchMicrosoftLearnAsync(string query)
        {
            try
            {
                var endpoint = _configuration["MCP:MicrosoftLearn:Endpoint"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    throw new InvalidOperationException("Microsoft Learn MCP endpoint が設定されていません");
                }

                // キャッシュチェック
                var cacheKey = $"mslearn_{query}";
                if (IsCacheValid(cacheKey))
                {
                    _logger.LogDebug($"キャッシュからレスポンスを返却: {query}");
                    return (McpSearchResult)_cacheData[cacheKey];
                }

                var requestPayload = new
                {
                    query = query,
                    max_results = 10,
                    include_content = true
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Microsoft Learn MCP Server に検索リクエスト: {query}");
                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<McpSearchResult>(responseContent);
                    
                    if (result != null)
                    {
                        // キャッシュに保存
                        UpdateCache(cacheKey, result);
                        _logger.LogInformation($"検索成功: {result.Results?.Count ?? 0} 件の結果");
                        return result;
                    }
                }

                _logger.LogWarning($"検索リクエスト失敗: {response.StatusCode}");
                return new McpSearchResult { Error = $"HTTP {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Microsoft Learn MCP Server 検索中にエラー: {query}");
                return new McpSearchResult { Error = ex.Message };
            }
        }

        /// <summary>
        /// MCP Server Serena にコード生成リクエスト
        /// </summary>
        public async Task<McpCodeGenerationResult> GenerateCodeAsync(string prompt, string codeType = "csharp")
        {
            try
            {
                var endpoint = _configuration["MCP:Serena:Endpoint"];
                var apiKey = _configuration["MCP:Serena:ApiKey"];

                if (string.IsNullOrEmpty(endpoint))
                {
                    throw new InvalidOperationException("Serena MCP endpoint が設定されていません");
                }

                var requestPayload = new
                {
                    prompt = prompt,
                    code_type = codeType,
                    framework = "WPF",
                    pattern = "MVVM",
                    language = "C#",
                    target_framework = "net8.0"
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // API キーがある場合はヘッダーに追加
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                _logger.LogInformation($"Serena MCP Server にコード生成リクエスト: {prompt}");
                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<McpCodeGenerationResult>(responseContent);
                    
                    if (result != null)
                    {
                        _logger.LogInformation("コード生成成功");
                        return result;
                    }
                }

                _logger.LogWarning($"コード生成リクエスト失敗: {response.StatusCode}");
                return new McpCodeGenerationResult { Error = $"HTTP {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Serena MCP Server コード生成中にエラー: {prompt}");
                return new McpCodeGenerationResult { Error = ex.Message };
            }
        }

        /// <summary>
        /// Graph API プロパティ情報を取得
        /// </summary>
        public async Task<List<GraphApiProperty>> GetGraphApiPropertiesAsync(string resourceType = "user")
        {
            var query = $"Microsoft Graph API {resourceType} properties fields documentation";
            var searchResult = await SearchMicrosoftLearnAsync(query);

            if (searchResult.Results != null && searchResult.Results.Any())
            {
                // 検索結果からプロパティ情報を抽出
                return ExtractPropertiesFromSearchResult(searchResult);
            }

            return new List<GraphApiProperty>();
        }

        /// <summary>
        /// キャッシュの有効性確認
        /// </summary>
        private bool IsCacheValid(string key)
        {
            if (!_configuration.GetValue<bool>("MCP:MicrosoftLearn:CacheEnabled", true))
            {
                return false;
            }

            if (_cache.TryGetValue(key, out var cacheTime))
            {
                var duration = _configuration.GetValue<int>("MCP:MicrosoftLearn:CacheDurationMinutes", 60);
                return DateTime.Now - cacheTime < TimeSpan.FromMinutes(duration);
            }

            return false;
        }

        /// <summary>
        /// キャッシュ更新
        /// </summary>
        private void UpdateCache(string key, object data)
        {
            _cache[key] = DateTime.Now;
            _cacheData[key] = data;
        }

        /// <summary>
        /// 検索結果からプロパティ情報を抽出
        /// </summary>
        private List<GraphApiProperty> ExtractPropertiesFromSearchResult(McpSearchResult searchResult)
        {
            var properties = new List<GraphApiProperty>();

            // 実際の実装では、検索結果のコンテンツをパースして
            // プロパティ情報を抽出します
            // ここでは簡易実装として既知のプロパティを返します

            properties.AddRange(new[]
            {
                new GraphApiProperty { Name = "id", Type = "string", Required = true, Description = "ユーザーの一意識別子" },
                new GraphApiProperty { Name = "displayName", Type = "string", Required = true, Description = "表示名" },
                new GraphApiProperty { Name = "userPrincipalName", Type = "string", Required = true, Description = "ユーザープリンシパル名" },
                new GraphApiProperty { Name = "mail", Type = "string", Required = false, Description = "メールアドレス" },
                new GraphApiProperty { Name = "jobTitle", Type = "string", Required = false, Description = "役職" },
                new GraphApiProperty { Name = "department", Type = "string", Required = false, Description = "部署" },
                new GraphApiProperty { Name = "accountEnabled", Type = "boolean", Required = false, Description = "アカウント有効状態" }
            });

            return properties;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// MCP検索結果
    /// </summary>
    public class McpSearchResult
    {
        public List<McpSearchItem>? Results { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// MCP検索アイテム
    /// </summary>
    public class McpSearchItem
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? Content { get; set; }
        public float? Relevance { get; set; }
    }

    /// <summary>
    /// MCPコード生成結果
    /// </summary>
    public class McpCodeGenerationResult
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public List<string>? Dependencies { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Graph API プロパティ情報
    /// </summary>
    public class GraphApiProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string? Description { get; set; }
        public string[]? Permissions { get; set; }
    }
}