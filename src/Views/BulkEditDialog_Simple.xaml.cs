using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TeamsAccountManager.Models;

namespace TeamsAccountManager.Views
{
    public partial class BulkEditDialog_Simple : Window
    {
        private readonly List<User> _selectedUsers;
        private Dictionary<string, object> _changes = new Dictionary<string, object>();
        
        public BulkEditDialog_Simple(List<User> selectedUsers)
        {
            InitializeComponent();
            _selectedUsers = selectedUsers;
            
            SelectionInfoTextBlock.Text = $"{_selectedUsers.Count} 人のユーザーが選択されています";
        }
        
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == DepartmentCheckBox)
                DepartmentTextBox.IsEnabled = true;
            else if (sender == JobTitleCheckBox)
                JobTitleTextBox.IsEnabled = true;
            else if (sender == OfficeLocationCheckBox)
                OfficeLocationTextBox.IsEnabled = true;
            else if (sender == AccountEnabledCheckBox)
                AccountEnabledComboBox.IsEnabled = true;
        }
        
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender == DepartmentCheckBox)
            {
                DepartmentTextBox.IsEnabled = false;
                DepartmentTextBox.Clear();
            }
            else if (sender == JobTitleCheckBox)
            {
                JobTitleTextBox.IsEnabled = false;
                JobTitleTextBox.Clear();
            }
            else if (sender == OfficeLocationCheckBox)
            {
                OfficeLocationTextBox.IsEnabled = false;
                OfficeLocationTextBox.Clear();
            }
            else if (sender == AccountEnabledCheckBox)
            {
                AccountEnabledComboBox.IsEnabled = false;
                AccountEnabledComboBox.SelectedIndex = -1;
            }
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _changes.Clear();
            
            if (DepartmentCheckBox.IsChecked == true && !string.IsNullOrWhiteSpace(DepartmentTextBox.Text))
            {
                _changes["Department"] = DepartmentTextBox.Text;
            }
            
            if (JobTitleCheckBox.IsChecked == true && !string.IsNullOrWhiteSpace(JobTitleTextBox.Text))
            {
                _changes["JobTitle"] = JobTitleTextBox.Text;
            }
            
            if (OfficeLocationCheckBox.IsChecked == true && !string.IsNullOrWhiteSpace(OfficeLocationTextBox.Text))
            {
                _changes["OfficeLocation"] = OfficeLocationTextBox.Text;
            }
            
            if (AccountEnabledCheckBox.IsChecked == true && AccountEnabledComboBox.SelectedItem != null)
            {
                var selectedItem = (ComboBoxItem)AccountEnabledComboBox.SelectedItem;
                _changes["AccountEnabled"] = selectedItem.Tag.ToString() == "True";
            }
            
            if (_changes.Count == 0)
            {
                MessageBox.Show("変更する項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        public Dictionary<string, object> GetChanges()
        {
            return _changes;
        }
    }
}