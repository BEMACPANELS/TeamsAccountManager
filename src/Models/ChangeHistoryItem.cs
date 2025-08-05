using System;
using System.Windows.Media;

namespace TeamsAccountManager.Models
{
    /// <summary>
    /// 変更履歴の表示用アイテム
    /// </summary>
    public class ChangeHistoryItem
    {
        public string UserId { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyDisplayName { get; set; } = string.Empty;
        public string? OriginalValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public ChangeStatus Status { get; set; }
        
        /// <summary>
        /// ステータスの表示テキスト
        /// </summary>
        public string StatusText
        {
            get
            {
                return Status switch
                {
                    ChangeStatus.Pending => "保留中",
                    ChangeStatus.Applied => "適用済み",
                    ChangeStatus.Failed => "失敗",
                    ChangeStatus.Reverted => "取り消し済み",
                    _ => "不明"
                };
            }
        }
        
        /// <summary>
        /// ステータスの表示色
        /// </summary>
        public Brush StatusColor
        {
            get
            {
                return Status switch
                {
                    ChangeStatus.Pending => Brushes.Orange,
                    ChangeStatus.Applied => Brushes.Green,
                    ChangeStatus.Failed => Brushes.Red,
                    ChangeStatus.Reverted => Brushes.Gray,
                    _ => Brushes.Black
                };
            }
        }
        
        /// <summary>
        /// 元に戻せるかどうか
        /// </summary>
        public bool CanRevert => Status == ChangeStatus.Applied;
        
        /// <summary>
        /// UserChangeから変換
        /// </summary>
        public static ChangeHistoryItem FromUserChange(UserChange change, User user)
        {
            return new ChangeHistoryItem
            {
                UserId = change.UserId,
                UserDisplayName = user.DisplayName ?? user.UserPrincipalName ?? "不明",
                PropertyName = change.PropertyName,
                PropertyDisplayName = GetPropertyDisplayName(change.PropertyName),
                OriginalValue = change.OriginalValue?.ToString(),
                NewValue = change.NewValue?.ToString(),
                ChangedAt = change.ChangedAt,
                Status = change.Status
            };
        }
        
        private static string GetPropertyDisplayName(string propertyName)
        {
            return propertyName switch
            {
                nameof(User.DisplayName) => "表示名",
                nameof(User.Department) => "部署",
                nameof(User.JobTitle) => "役職",
                nameof(User.OfficeLocation) => "オフィス所在地",
                nameof(User.PhoneNumber) => "電話番号",
                nameof(User.AccountEnabled) => "アカウント有効",
                _ => propertyName
            };
        }
    }
}