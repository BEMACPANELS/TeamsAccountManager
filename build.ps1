# Teams Account Manager ビルドスクリプト
# 使用方法: .\build.ps1 [-Configuration <Debug|Release>] [-Publish]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$Publish,
    [switch]$Clean
)

# 色付きメッセージ出力
function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# エラーチェック
function Check-Error {
    if ($LASTEXITCODE -ne 0) {
        Write-ColorMessage "❌ エラーが発生しました。処理を中止します。" "Red"
        exit 1
    }
}

# ヘッダー表示
Write-ColorMessage "`n===== Teams Account Manager ビルドスクリプト =====`n" "Cyan"

# プロジェクトパス
$projectPath = Join-Path $PSScriptRoot "src\TeamsAccountManager.csproj"

if (-not (Test-Path $projectPath)) {
    Write-ColorMessage "❌ プロジェクトファイルが見つかりません: $projectPath" "Red"
    exit 1
}

# .NET SDK バージョン確認
Write-ColorMessage "🔍 .NET SDK バージョンを確認中..." "Yellow"
dotnet --version
Check-Error

# クリーンビルド
if ($Clean) {
    Write-ColorMessage "`n🧹 クリーンを実行中..." "Yellow"
    dotnet clean $projectPath -c $Configuration
    Check-Error
}

# NuGetパッケージの復元
Write-ColorMessage "`n📦 NuGetパッケージを復元中..." "Yellow"
dotnet restore $projectPath
Check-Error

# ビルド実行
Write-ColorMessage "`n🔨 ビルドを実行中 (Configuration: $Configuration)..." "Yellow"
dotnet build $projectPath -c $Configuration
Check-Error

Write-ColorMessage "`n✅ ビルドが正常に完了しました！" "Green"

# パブリッシュ
if ($Publish) {
    Write-ColorMessage "`n📤 パブリッシュを実行中..." "Yellow"
    
    $publishPath = Join-Path $PSScriptRoot "publish"
    
    # 単一ファイルとして発行（.NET 8ランタイム依存）
    dotnet publish $projectPath `
        -c $Configuration `
        -r win-x64 `
        --self-contained false `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=false `
        -o $publishPath
    
    Check-Error
    
    Write-ColorMessage "`n✅ パブリッシュが完了しました！" "Green"
    Write-ColorMessage "📁 出力先: $publishPath" "Cyan"
    
    # 出力ファイル一覧
    Write-ColorMessage "`n📋 出力ファイル:" "Yellow"
    Get-ChildItem $publishPath | ForEach-Object {
        Write-Host "  - $($_.Name)"
    }
}

# Azure AD設定の確認
$appSettingsPath = Join-Path $PSScriptRoot "src\appsettings.json"
if (Test-Path $appSettingsPath) {
    $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
    
    if ($appSettings.AzureAd.TenantId -eq "YOUR_TENANT_ID_HERE") {
        Write-ColorMessage "`n⚠️  警告: Azure AD設定が未設定です！" "Yellow"
        Write-ColorMessage "  appsettings.json で以下の設定を行ってください:" "Yellow"
        Write-ColorMessage "  - TenantId: Azure ADテナントID" "White"
        Write-ColorMessage "  - ClientId: アプリケーション（クライアント）ID" "White"
    }
}

Write-ColorMessage "`n===== ビルド処理完了 =====`n" "Cyan"