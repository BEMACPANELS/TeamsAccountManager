using System;
using System.Collections.Generic;

namespace TeamsAccountManager.Models
{
    /// <summary>
    /// データ操作完了イベント引数
    /// </summary>
    public class DataOperationEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// エクスポート要求イベント引数
    /// </summary>
    public class ExportRequestedEventArgs : EventArgs
    {
        public bool IsExcel { get; set; }
    }

    /// <summary>
    /// インポート要求イベント引数
    /// </summary>
    public class ImportRequestedEventArgs : EventArgs
    {
        public bool IsExcel { get; set; }
    }

    /// <summary>
    /// インポート確認イベント引数
    /// </summary>
    public class ImportConfirmationEventArgs : EventArgs
    {
        public List<User> Users { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 進捗イベント引数
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string Message { get; set; } = string.Empty;
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    }

    /// <summary>
    /// 検証結果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}