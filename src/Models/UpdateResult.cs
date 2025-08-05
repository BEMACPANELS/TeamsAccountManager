namespace TeamsAccountManager.Models
{
    /// <summary>
    /// 更新処理の結果
    /// </summary>
    public class UpdateResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}