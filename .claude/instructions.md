# Teams Account Manager プロジェクト固有指示

## MCP (Model Context Protocol) 統合設定

このプロジェクトでは、以下のMCPサーバーを活用して開発効率を向上させます。

### 1. Microsoft Learn MCP Server
- **目的**: Microsoft Graph APIの正確なドキュメント参照
- **エンドポイント**: `https://learn.microsoft.com/api/mcp`
- **活用方法**:
  - Graph APIのプロパティ名確認
  - 権限スコープの確認
  - エラーメッセージの調査
  - ベストプラクティスの参照

### 2. MCP Server Serena
- **目的**: 開発効率化とコード品質向上
- **活用方法**:
  - MVVM パターンのボイラープレート生成
  - エラーハンドリングコードの生成
  - 多言語リソースファイルの管理
  - 単体テストコードの生成

## 開発時の注意事項

### Graph API フィールド参照
Microsoft Graph APIのフィールド名やプロパティを使用する際は、以下の手順で確認してください：

1. Microsoft Learn MCP Serverで最新のドキュメントを検索
2. 正確なプロパティ名とデータ型を確認
3. 必要な権限スコープを確認
4. サンプルコードがあれば参照

### コード生成時の優先事項
1. **型安全性**: nullable reference types を活用
2. **エラーハンドリング**: 適切な例外処理とログ出力
3. **多言語対応**: ハードコードされた文字列の排除
4. **MVVM準拠**: ViewModelとViewの適切な分離

## MCP Server活用例

### Microsoft Learn MCP Server使用例
```
Query: "Microsoft Graph API user properties list"
Response: 最新のユーザープロパティ一覧とその詳細
```

### MCP Server Serena使用例
```
Request: "Generate MVVM ViewModel for user list management"
Response: 適切なViewModelクラスの雛形生成
```

## 開発フロー

1. **機能設計時**: Microsoft Learn MCP ServerでAPIリファレンス確認
2. **実装時**: MCP Server Serenaでコード雛形生成
3. **テスト時**: エラーケースをMicrosoft Learn MCP Serverで調査
4. **デバッグ時**: エラーメッセージの詳細をMCP Serverで検索

この指示に従って、効率的かつ品質の高いコードを作成してください。