using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace TeamsAccountManager.Services
{
    /// <summary>
    /// Microsoft Learn MCP Server サービス
    /// 直接 Microsoft Learn API エンドポイントにアクセス
    /// </summary>
    public class MsLearnService
    {
        private readonly ILogger<MsLearnService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public MsLearnService(ILogger<MsLearnService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
            
            // Microsoft Learn MCP Server endpoint
            _httpClient.BaseAddress = new Uri("https://learn.microsoft.com/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Teams-Account-Manager/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// Microsoft Graph API ドキュメントを検索
        /// </summary>
        public async Task<MsLearnSearchResult> SearchGraphApiDocumentationAsync(string query)
        {
            try
            {
                // Microsoft Learn の検索エンドポイントを使用
                var searchUrl = $"api/search?query={Uri.EscapeDataString(query + " Microsoft Graph API")}&locale=en-us&facet=category:documentation";
                
                _logger.LogInformation($"Microsoft Learn API検索: {query}");
                
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<MsLearnApiResponse>(content);
                    
                    if (searchResult?.Results != null)
                    {
                        var results = searchResult.Results
                            .Where(r => r.Url?.Contains("graph") == true || r.Title?.Contains("Graph") == true)
                            .Take(10)
                            .Select(r => new MsLearnSearchItem
                            {
                                Title = r.Title ?? "",
                                Url = r.Url ?? "",
                                Description = r.Description ?? "",
                                Relevance = CalculateRelevance(r, query)
                            })
                            .OrderByDescending(r => r.Relevance)
                            .ToList();

                        _logger.LogInformation($"検索成功: {results.Count}件の結果");
                        return new MsLearnSearchResult { Results = results };
                    }
                }

                _logger.LogWarning($"検索失敗: {response.StatusCode}");
                return new MsLearnSearchResult { Error = $"HTTP {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Microsoft Learn API検索中にエラー: {query}");
                return new MsLearnSearchResult { Error = ex.Message };
            }
        }

        /// <summary>
        /// Graph API のユーザープロパティ情報を取得
        /// </summary>
        public async Task<List<GraphApiPropertyInfo>> GetUserPropertiesAsync()
        {
            var searchResult = await SearchGraphApiDocumentationAsync("user resource type properties fields");
            
            var properties = new List<GraphApiPropertyInfo>();

            // Microsoft Graph API のユーザープロパティ（既知の情報を基に生成）
            properties.AddRange(new[]
            {
                new GraphApiPropertyInfo
                {
                    Name = "id",
                    Type = "string",
                    Required = true,
                    Description = "ユーザーの一意識別子",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "displayName",
                    Type = "string",
                    Required = true,
                    Description = "アドレス帳に表示される名前",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "userPrincipalName",
                    Type = "string",
                    Required = true,
                    Description = "ユーザープリンシパル名（UPN）",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "mail",
                    Type = "string",
                    Required = false,
                    Description = "ユーザーのSMTPアドレス",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "jobTitle",
                    Type = "string",
                    Required = false,
                    Description = "ユーザーの役職",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "department",
                    Type = "string",
                    Required = false,
                    Description = "ユーザーが所属する部署名",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "businessPhones",
                    Type = "string[]",
                    Required = false,
                    Description = "ユーザーの会社電話番号",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "mobilePhone",
                    Type = "string",
                    Required = false,
                    Description = "ユーザーの携帯電話番号",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "officeLocation",
                    Type = "string",
                    Required = false,
                    Description = "ユーザーの物理的なオフィスの場所",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                },
                new GraphApiPropertyInfo
                {
                    Name = "accountEnabled",
                    Type = "boolean",
                    Required = false,
                    Description = "アカウントが有効かどうか",
                    DocumentationUrl = "https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0#properties"
                }
            });

            _logger.LogInformation($"Graph API ユーザープロパティ情報取得: {properties.Count}件");
            return properties;
        }

        /// <summary>
        /// 関連度を計算
        /// </summary>
        private float CalculateRelevance(MsLearnApiResult result, string query)
        {
            float score = 0;
            var queryLower = query.ToLower();

            // タイトルに含まれる場合
            if (result.Title?.ToLower().Contains(queryLower) == true)
                score += 2.0f;

            // 説明に含まれる場合
            if (result.Description?.ToLower().Contains(queryLower) == true)
                score += 1.0f;

            // Graph API関連のキーワード
            var graphKeywords = new[] { "graph", "api", "user", "properties", "microsoft" };
            foreach (var keyword in graphKeywords)
            {
                if (result.Title?.ToLower().Contains(keyword) == true)
                    score += 0.5f;
                if (result.Url?.ToLower().Contains(keyword) == true)
                    score += 0.3f;
            }

            return score;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Microsoft Learn API レスポンス
    /// </summary>
    public class MsLearnApiResponse
    {
        public List<MsLearnApiResult>? Results { get; set; }
        public int? TotalCount { get; set; }
    }

    /// <summary>
    /// Microsoft Learn API 結果アイテム
    /// </summary>
    public class MsLearnApiResult
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
        public DateTime? LastModified { get; set; }
    }

    /// <summary>
    /// Microsoft Learn 検索結果
    /// </summary>
    public class MsLearnSearchResult
    {
        public List<MsLearnSearchItem>? Results { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Microsoft Learn 検索アイテム
    /// </summary>
    public class MsLearnSearchItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Relevance { get; set; }
    }

    /// <summary>
    /// Graph API プロパティ情報
    /// </summary>
    public class GraphApiPropertyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? DocumentationUrl { get; set; }
        public string[]? RequiredPermissions { get; set; }
    }
}