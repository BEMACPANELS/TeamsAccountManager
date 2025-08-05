using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TeamsAccountManager.Models
{
    /// <summary>
    /// Microsoft 365ユーザー情報モデル
    /// </summary>
    public class User : INotifyPropertyChanged
    {
        /// <summary>
        /// ユーザーの一意識別子
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 会社の電話番号
        /// </summary>
        public string[]? BusinessPhones { get; set; }

        /// <summary>
        /// 表示名
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 名
        /// </summary>
        public string? GivenName { get; set; }

        /// <summary>
        /// 役職
        /// </summary>
        public string? JobTitle { get; set; }

        /// <summary>
        /// メールアドレス
        /// </summary>
        public string? Mail { get; set; }

        /// <summary>
        /// 携帯電話番号
        /// </summary>
        public string? MobilePhone { get; set; }

        /// <summary>
        /// オフィスの場所
        /// </summary>
        public string? OfficeLocation { get; set; }

        /// <summary>
        /// 優先言語
        /// </summary>
        public string? PreferredLanguage { get; set; }

        /// <summary>
        /// 姓
        /// </summary>
        public string? Surname { get; set; }

        /// <summary>
        /// ユーザープリンシパル名
        /// </summary>
        public string UserPrincipalName { get; set; } = string.Empty;

        // 拡張プロパティ（$selectで取得）

        /// <summary>
        /// 部署
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// 会社名
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// 国
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// 市区町村
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// 郵便番号
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// 都道府県
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// 使用場所
        /// </summary>
        public string? UsageLocation { get; set; }

        /// <summary>
        /// アカウント有効/無効
        /// </summary>
        public bool? AccountEnabled { get; set; }

        /// <summary>
        /// ユーザータイプ (Member/Guest)
        /// </summary>
        public string? UserType { get; set; }


        // アプリケーション用プロパティ

        /// <summary>
        /// 変更フラグ
        /// </summary>
        public bool IsModified { get; set; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 選択フラグ
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 最終サインイン日時
        /// </summary>
        public DateTime? LastSignIn { get; set; }

        /// <summary>
        /// メールアドレス（Mailがnullの場合はUserPrincipalNameを返す）
        /// </summary>
        public string Email 
        { 
            get => Mail ?? UserPrincipalName;
            set => Mail = value;
        }
        
        /// <summary>
        /// メールアドレスのドメイン部分（@以降）
        /// </summary>
        public string EmailDomain
        {
            get
            {
                var email = Email;
                if (string.IsNullOrEmpty(email))
                    return string.Empty;
                    
                var atIndex = email.IndexOf('@');
                return atIndex >= 0 ? email.Substring(atIndex + 1) : string.Empty;
            }
        }

        /// <summary>
        /// 電話番号（ビジネス電話または携帯電話）
        /// </summary>
        public string PhoneNumber
        {
            get
            {
                if (!string.IsNullOrEmpty(MobilePhone))
                    return MobilePhone;
                if (BusinessPhones != null && BusinessPhones.Length > 0)
                    return BusinessPhones[0];
                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    MobilePhone = value;
                    // ビジネス電話も更新
                    BusinessPhones = new[] { value };
                }
            }
        }

        /// <summary>
        /// ゲストユーザーかどうか
        /// </summary>
        public bool IsGuest => UserType?.Equals("Guest", StringComparison.OrdinalIgnoreCase) ?? 
                              UserPrincipalName?.Contains("#EXT#", StringComparison.OrdinalIgnoreCase) ?? false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public User()
        {
        }

        /// <summary>
        /// Microsoft Graphのユーザーオブジェクトから変換
        /// </summary>
        public static User FromGraphUser(Microsoft.Graph.Models.User graphUser)
        {
            return new User
            {
                Id = graphUser.Id ?? string.Empty,
                BusinessPhones = graphUser.BusinessPhones?.ToArray(),
                DisplayName = graphUser.DisplayName ?? string.Empty,
                GivenName = graphUser.GivenName,
                JobTitle = graphUser.JobTitle,
                Mail = graphUser.Mail,
                MobilePhone = graphUser.MobilePhone,
                OfficeLocation = graphUser.OfficeLocation,
                PreferredLanguage = graphUser.PreferredLanguage,
                Surname = graphUser.Surname,
                UserPrincipalName = graphUser.UserPrincipalName ?? string.Empty,
                Department = graphUser.Department,
                CompanyName = graphUser.CompanyName,
                Country = graphUser.Country,
                City = graphUser.City,
                PostalCode = graphUser.PostalCode,
                State = graphUser.State,
                UsageLocation = graphUser.UsageLocation,
                AccountEnabled = graphUser.AccountEnabled
            };
        }

        /// <summary>
        /// Microsoft Graphのユーザーオブジェクトに変換
        /// </summary>
        public Microsoft.Graph.Models.User ToGraphUser()
        {
            return new Microsoft.Graph.Models.User
            {
                Id = Id,
                BusinessPhones = BusinessPhones?.ToList(),
                DisplayName = DisplayName,
                GivenName = GivenName,
                JobTitle = JobTitle,
                Mail = Mail,
                MobilePhone = MobilePhone,
                OfficeLocation = OfficeLocation,
                PreferredLanguage = PreferredLanguage,
                Surname = Surname,
                UserPrincipalName = UserPrincipalName,
                Department = Department,
                CompanyName = CompanyName,
                Country = Country,
                City = City,
                PostalCode = PostalCode,
                State = State,
                UsageLocation = UsageLocation,
                AccountEnabled = AccountEnabled
            };
        }

        /// <summary>
        /// 複製を作成
        /// </summary>
        public User Clone()
        {
            return new User
            {
                Id = Id,
                BusinessPhones = BusinessPhones?.ToArray(),
                DisplayName = DisplayName,
                GivenName = GivenName,
                JobTitle = JobTitle,
                Mail = Mail,
                MobilePhone = MobilePhone,
                OfficeLocation = OfficeLocation,
                PreferredLanguage = PreferredLanguage,
                Surname = Surname,
                UserPrincipalName = UserPrincipalName,
                Department = Department,
                CompanyName = CompanyName,
                Country = Country,
                City = City,
                PostalCode = PostalCode,
                State = State,
                UsageLocation = UsageLocation,
                AccountEnabled = AccountEnabled,
                UserType = UserType,
                IsModified = IsModified,
                ErrorMessage = ErrorMessage,
                IsSelected = IsSelected,
                LastSignIn = LastSignIn
            };
        }

        /// <summary>
        /// プロパティ変更通知イベント
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// プロパティ変更通知
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}