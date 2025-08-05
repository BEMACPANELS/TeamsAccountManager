# Teams Account Manager 実行スクリプト
# 使用方法: .\run.ps1 [-Configuration <Debug|Release>]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

# 色付きメッセージ出力
function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

Write-ColorMessage "`n===== Teams Account Manager 起動 =====`n" "Cyan"

# プロジェクトパス
$projectPath = Join-Path $PSScriptRoot "src\TeamsAccountManager.csproj"

if (-not (Test-Path $projectPath)) {
    Write-ColorMessage "❌ プロジェクトファイルが見つかりません: $projectPath" "Red"
    exit 1
}

# Azure AD設定確認
$appSettingsPath = Join-Path $PSScriptRoot "src\appsettings.json"
if (Test-Path $appSettingsPath) {
    $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
    
    if ($appSettings.AzureAd.TenantId -eq "YOUR_TENANT_ID_HERE") {
        Write-ColorMessage "⚠️  警告: Azure AD設定が未設定です！" "Yellow"
        Write-ColorMessage "認証が必要な機能は使用できません。" "Yellow"
        Write-ColorMessage "`nappsettings.json で以下の設定を行ってください:" "White"
        Write-ColorMessage "- TenantId: Azure ADテナントID" "White"
        Write-ColorMessage "- ClientId: アプリケーション（クライアント）ID" "White"
        Write-ColorMessage ""
        
        $continue = Read-Host "このまま続行しますか？ (y/N)"
        if ($continue -ne "y") {
            exit 0
        }
    }
}

# アプリケーション実行
Write-ColorMessage "🚀 アプリケーションを起動中 (Configuration: $Configuration)..." "Yellow"
Write-ColorMessage "終了するには Ctrl+C を押してください`n" "Gray"

dotnet run --project $projectPath -c $Configuration