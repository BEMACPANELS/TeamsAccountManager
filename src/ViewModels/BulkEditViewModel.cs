using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using TeamsAccountManager.Models;

namespace TeamsAccountManager.ViewModels
{
    public partial class BulkEditViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _bulkDepartment;

        [ObservableProperty]
        private string? _bulkJobTitle;

        [ObservableProperty]
        private string? _bulkOfficeLocation;

        [ObservableProperty]
        private string? _bulkAccountEnabled;

        [ObservableProperty]
        private int _selectedUsersCount;

        private readonly List<User> _selectedUsers;

        public BulkEditViewModel(List<User> selectedUsers)
        {
            _selectedUsers = selectedUsers;
            SelectedUsersCount = selectedUsers.Count;
        }

        /// <summary>
        /// 適用する変更を取得
        /// </summary>
        public BulkEditChanges GetChanges()
        {
            return new BulkEditChanges
            {
                Department = BulkDepartment,
                JobTitle = BulkJobTitle,
                OfficeLocation = BulkOfficeLocation,
                AccountEnabled = BulkAccountEnabled == "Enabled" ? true : 
                                BulkAccountEnabled == "Disabled" ? false : null,
                TargetUsers = _selectedUsers
            };
        }
    }

    /// <summary>
    /// 一括編集の変更内容
    /// </summary>
    public class BulkEditChanges
    {
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? OfficeLocation { get; set; }
        public bool? AccountEnabled { get; set; }
        public List<User> TargetUsers { get; set; } = new();

        /// <summary>
        /// 変更があるかどうか
        /// </summary>
        public bool HasChanges =>
            !string.IsNullOrEmpty(Department) ||
            !string.IsNullOrEmpty(JobTitle) ||
            !string.IsNullOrEmpty(OfficeLocation) ||
            AccountEnabled.HasValue;
    }
}