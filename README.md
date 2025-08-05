# Teams Account Manager

Microsoft Teams/Microsoft 365のユーザーアカウントを一括管理するためのWPFアプリケーション

## バージョン
v0.9.1 (2025-08-04)

## 概要
Teams Account Managerは、Microsoft Graph APIを使用してMicrosoft 365のユーザーアカウントを効率的に管理するためのデスクトップアプリケーションです。

## 主な機能

### ✅ 実装済み機能
- **認証機能**
  - Azure AD/Microsoft Entra ID認証
  - シングルサインオン（SSO）対応
  
- **ユーザー管理**
  - ユーザー一覧の表示
  - インライン編集機能
  - 一括編集機能
  - 変更履歴の追跡
  
- **データ操作**
  - Excel/CSVエクスポート
  - Excel/CSVインポート
  - リアルタイム検索
  - 列ごとのフィルタリング
  
- **UI機能**
  - 多言語対応（日本語/英語/ベトナム語）
  - レスポンシブデザイン（ウィンドウサイズに応じた自動調整）
  - シンプルでクリーンなインターフェース

### 🚧 開発中機能
- 高度な検索フィルター
- バッチ処理の最適化
- 詳細なログ機能

## 技術スタック
- **フレームワーク**: .NET 8.0, WPF
- **認証**: Microsoft Identity Client (MSAL)
- **API**: Microsoft Graph API
- **パターン**: MVVM
- **DI**: Microsoft.Extensions.DependencyInjection
- **Excel/CSV**: ClosedXML, CsvHelper

## セットアップ

### 前提条件
- .NET 8.0 SDK
- Windows 10/11
- Azure ADアプリケーション登録

### インストール手順

1. リポジトリをクローン
```bash
git clone https://github.com/BEMACPANELS/TeamsAccountManager.git
cd TeamsAccountManager
```

2. Azure AD設定
`src/appsettings.json`を編集：
```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "RedirectUri": "http://localhost"
  }
}
```

3. ビルドと実行
```bash
cd src
dotnet build
dotnet run
```

## 使用方法

1. **ログイン**
   - アプリケーションを起動し、「ログイン」ボタンをクリック
   - Microsoft 365アカウントでサインイン

2. **ユーザー管理**
   - ユーザー一覧が自動的に読み込まれます
   - セルをダブルクリックして直接編集
   - 複数選択して一括編集も可能

3. **データのエクスポート/インポート**
   - 「📥 エクスポート」でExcel/CSV形式で保存
   - 「📤 インポート」で外部データを読み込み

4. **フィルタリング**
   - 各列のヘッダー下のテキストボックスでフィルタリング
   - 検索ボックスで全体検索

## 開発者向け情報

### プロジェクト構造
```
TeamsAccountManager/
├── src/
│   ├── Models/          # データモデル
│   ├── Views/           # WPFビュー
│   ├── ViewModels/      # ビューモデル
│   ├── Services/        # ビジネスロジック
│   └── Resources/       # リソースファイル
├── docs/                # ドキュメント
└── tmp/                 # 作業用ファイル
```

### ビルド方法
```bash
# デバッグビルド
dotnet build

# リリースビルド
dotnet build -c Release

# 単一実行ファイルの作成
dotnet publish -c Release -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false
```

## ライセンス
このプロジェクトは内部使用を目的としています。

## 更新履歴

### v0.9.1 (2025-08-04)
- フィルター機能の改善
- UIレイアウトの最適化
- バージョン表示機能の追加
- 列幅の自動調整機能

### v0.9.0 (2025-08-03)
- 初期リリース
- 基本的なユーザー管理機能
- エクスポート/インポート機能
- 多言語対応

## サポート
問題や提案がある場合は、GitHubのIssuesに報告してください。