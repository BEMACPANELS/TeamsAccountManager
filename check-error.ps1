# エラーチェックスクリプト

Write-Host "===== アプリケーションエラーチェック =====" -ForegroundColor Cyan
Write-Host ""

# .NET Runtime のイベントログを確認
Write-Host "最近の.NETエラーを確認中..." -ForegroundColor Yellow
$errors = Get-EventLog -LogName Application -Source ".NET Runtime" -Newest 5 -ErrorAction SilentlyContinue

if ($errors) {
    foreach ($error in $errors) {
        if ($error.Message -like "*TeamsAccountManager*") {
            Write-Host ""
            Write-Host "エラー発生時刻: $($error.TimeGenerated)" -ForegroundColor Red
            Write-Host "エラー内容:" -ForegroundColor Red
            Write-Host $error.Message
            Write-Host ""
        }
    }
}

# 実行ファイルの情報を表示
$exePath = ".\src\bin\Release\net8.0-windows\TeamsAccountManager.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    Write-Host "実行ファイル情報:" -ForegroundColor Yellow
    Write-Host "  パス: $exePath"
    Write-Host "  サイズ: $([math]::Round($fileInfo.Length / 1KB, 2)) KB"
    Write-Host "  更新日時: $($fileInfo.LastWriteTime)"
}

# .NET ランタイムの確認
Write-Host ""
Write-Host ".NET ランタイムの確認:" -ForegroundColor Yellow
dotnet --list-runtimes | Where-Object { $_ -like "*WindowsDesktop*" }

Write-Host ""
Write-Host "完了" -ForegroundColor Green