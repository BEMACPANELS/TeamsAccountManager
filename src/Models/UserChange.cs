using System;

namespace TeamsAccountManager.Models
{
    /// <summary>
    /// ユーザー情報の変更を追跡するクラス
    /// </summary>
    public class UserChange
    {
        public string UserId { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public object? OriginalValue { get; set; }
        public object? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public ChangeStatus Status { get; set; } = ChangeStatus.Pending;
        
        /// <summary>
        /// 変更されたかどうかを判定
        /// </summary>
        public bool HasChanged
        {
            get
            {
                if (OriginalValue == null && NewValue == null) return false;
                if (OriginalValue == null || NewValue == null) return true;
                return !OriginalValue.Equals(NewValue);
            }
        }
    }

    /// <summary>
    /// 変更のステータス
    /// </summary>
    public enum ChangeStatus
    {
        Pending,    // 保留中
        Applied,    // 適用済み
        Failed,     // 失敗
        Reverted    // 元に戻された
    }
}