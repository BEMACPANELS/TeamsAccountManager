@echo off
echo ===== Teams Account Manager 簡易テスト =====
echo.

cd /d "%~dp0"

if exist "src\bin\Release\net8.0-windows\TeamsAccountManager.exe" (
    echo 実行ファイル: src\bin\Release\net8.0-windows\TeamsAccountManager.exe
    echo.
    
    echo アプリケーションを起動中...
    echo.
    
    cd src\bin\Release\net8.0-windows
    TeamsAccountManager.exe
    cd ..\..\..\..
) else if exist "publish\TeamsAccountManager.exe" (
    echo 実行ファイル: publish\TeamsAccountManager.exe
    echo.
    
    echo appsettings.json をコピー中...
    copy /Y "src\appsettings.json" "publish\" >nul 2>&1
    
    echo アプリケーションを起動中...
    echo.
    
    "publish\TeamsAccountManager.exe"
) else (
    echo エラー: 実行ファイルが見つかりません
    echo 先にビルドを実行してください
)

echo.
pause