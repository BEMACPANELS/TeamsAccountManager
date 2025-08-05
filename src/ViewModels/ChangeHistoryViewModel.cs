using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TeamsAccountManager.Models;

namespace TeamsAccountManager.ViewModels
{
    public partial class ChangeHistoryViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ChangeHistoryItem> _changeHistory = new();

        [ObservableProperty]
        private bool _hasHistory;

        public ICommand RevertChangeCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        private readonly Action<ChangeHistoryItem> _revertAction;
        private readonly Action _clearAction;

        public ChangeHistoryViewModel(
            Dictionary<string, List<UserChange>> allChanges,
            List<User> users,
            Action<ChangeHistoryItem> revertAction,
            Action clearAction)
        {
            _revertAction = revertAction;
            _clearAction = clearAction;

            RevertChangeCommand = new RelayCommand<ChangeHistoryItem>(RevertChange);
            ClearHistoryCommand = new RelayCommand(ClearHistory);

            LoadHistory(allChanges, users);
        }

        private void LoadHistory(Dictionary<string, List<UserChange>> allChanges, List<User> users)
        {
            ChangeHistory.Clear();

            foreach (var kvp in allChanges)
            {
                var userId = kvp.Key;
                var user = users.FirstOrDefault(u => u.Id == userId);
                
                if (user != null)
                {
                    foreach (var change in kvp.Value.OrderByDescending(c => c.ChangedAt))
                    {
                        ChangeHistory.Add(ChangeHistoryItem.FromUserChange(change, user));
                    }
                }
            }

            HasHistory = ChangeHistory.Any();
        }

        private void RevertChange(ChangeHistoryItem? item)
        {
            if (item != null && item.CanRevert)
            {
                _revertAction(item);
                
                // UIを更新
                item.Status = ChangeStatus.Reverted;
                var index = ChangeHistory.IndexOf(item);
                if (index >= 0)
                {
                    // PropertyChangedを発火させるため一旦削除して再追加
                    ChangeHistory.RemoveAt(index);
                    ChangeHistory.Insert(index, item);
                }
            }
        }

        private void ClearHistory()
        {
            _clearAction();
            ChangeHistory.Clear();
            HasHistory = false;
        }
    }
}