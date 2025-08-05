# Teams Account Manager デバッグ実行スクリプト
# エラーメッセージを確認するために使用

Write-Host "===== Teams Account Manager デバッグ実行 =====" -ForegroundColor Cyan
Write-Host ""

# 実行ファイルのパス
$exePath = ".\src\bin\Release\net8.0-windows\TeamsAccountManager.exe"
$publishPath = ".\publish\TeamsAccountManager.exe"
$singleFilePath = ".\src\bin\Release\net8.0-windows\win-x64\publish\TeamsAccountManager.exe"

# 実行ファイルの存在確認
if (Test-Path $singleFilePath) {
    $targetPath = $singleFilePath
    Write-Host "単一ファイル版を実行します: $singleFilePath" -ForegroundColor Yellow
    Write-Host "ファイルサイズ: $([math]::Round((Get-Item $targetPath).Length / 1MB, 2)) MB" -ForegroundColor Cyan
} elseif (Test-Path $publishPath) {
    $targetPath = $publishPath
    Write-Host "パブリッシュ版を実行します: $publishPath" -ForegroundColor Yellow
} elseif (Test-Path $exePath) {
    $targetPath = $exePath
    Write-Host "ビルド版を実行します: $exePath" -ForegroundColor Yellow
} else {
    Write-Host "実行ファイルが見つかりません！" -ForegroundColor Red
    Write-Host "先にビルドまたはパブリッシュを実行してください。" -ForegroundColor Red
    exit 1
}

# 設定ファイルの確認
$configPath = Split-Path $targetPath -Parent
$appSettingsPath = Join-Path $configPath "appsettings.json"

if (Test-Path $appSettingsPath) {
    Write-Host "✓ appsettings.json が見つかりました" -ForegroundColor Green
} else {
    Write-Host "✗ appsettings.json が見つかりません！" -ForegroundColor Red
    Write-Host "  期待されるパス: $appSettingsPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "アプリケーションを起動中..." -ForegroundColor Cyan
Write-Host "エラーが発生した場合は、以下に表示されます：" -ForegroundColor Yellow
Write-Host ""

# 環境変数を設定してログレベルを詳細に
$env:DOTNET_ENVIRONMENT = "Development"
$env:Logging__LogLevel__Default = "Debug"

# アプリケーション実行
try {
    & $targetPath
} catch {
    Write-Host ""
    Write-Host "===== エラーが発生しました =====" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "詳細:" -ForegroundColor Yellow
    Write-Host $_.Exception.ToString()
}

Write-Host ""
Write-Host "何かキーを押すと終了します..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")