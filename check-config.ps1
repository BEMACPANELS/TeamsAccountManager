# 設定ファイルの内容を確認するスクリプト

Write-Host "=== 設定ファイル確認スクリプト ===" -ForegroundColor Cyan

# 各場所のappsettings.jsonを確認
$locations = @(
    ".\appsettings.json",
    ".\src\appsettings.json",
    ".\src\bin\Release\net8.0-windows\appsettings.json",
    ".\src\bin\Debug\net8.0-windows\appsettings.json"
)

foreach ($location in $locations) {
    if (Test-Path $location) {
        Write-Host "`n📁 $location" -ForegroundColor Yellow
        $content = Get-Content $location -Raw | ConvertFrom-Json
        Write-Host "  TenantId: $($content.AzureAd.TenantId)" -ForegroundColor Green
        Write-Host "  ClientId: $($content.AzureAd.ClientId)" -ForegroundColor Green
    } else {
        Write-Host "`n❌ $location - 見つかりません" -ForegroundColor Red
    }
}

# 実行ファイルの場所を確認
Write-Host "`n=== 実行ファイルの場所 ===" -ForegroundColor Cyan
$exePath = ".\src\bin\Release\net8.0-windows\TeamsAccountManager.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    Write-Host "✓ 実行ファイル: $exePath" -ForegroundColor Green
    Write-Host "  最終更新: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "❌ 実行ファイルが見つかりません" -ForegroundColor Red
}

# プロセスの確認
Write-Host "`n=== 実行中のプロセス ===" -ForegroundColor Cyan
$process = Get-Process | Where-Object { $_.ProcessName -like "*TeamsAccountManager*" }
if ($process) {
    Write-Host "⚠️  TeamsAccountManagerが実行中です！" -ForegroundColor Yellow
    Write-Host "  PID: $($process.Id)" -ForegroundColor Gray
    Write-Host "  パス: $($process.Path)" -ForegroundColor Gray
} else {
    Write-Host "✓ TeamsAccountManagerは実行されていません" -ForegroundColor Green
}

Write-Host "`n=== 完了 ===" -ForegroundColor Cyan