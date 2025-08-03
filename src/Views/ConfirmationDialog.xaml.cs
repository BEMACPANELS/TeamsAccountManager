using System.Collections.Generic;
using System.Windows.Controls;
using TeamsAccountManager.Models;

namespace TeamsAccountManager.Views
{
    /// <summary>
    /// ConfirmationDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfirmationDialog : UserControl
    {
        public string Message { get; set; } = string.Empty;
        public List<User> Users { get; set; } = new();

        public ConfirmationDialog()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}