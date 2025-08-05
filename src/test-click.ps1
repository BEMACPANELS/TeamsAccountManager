# マウスクリックシミュレーション検出スクリプト

Write-Host "マウスの位置とクリック状態を監視します..." -ForegroundColor Yellow
Write-Host "Ctrl+C で終了" -ForegroundColor Cyan

Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    public class MouseInfo {
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        
        public struct POINT {
            public int X;
            public int Y;
        }
    }
"@

$lastX = 0
$lastY = 0

while ($true) {
    $point = New-Object MouseInfo+POINT
    [MouseInfo]::GetCursorPos([ref]$point) | Out-Null
    
    # マウスボタンの状態をチェック (0x01 = 左クリック)
    $leftButton = [MouseInfo]::GetAsyncKeyState(0x01)
    
    if ($point.X -ne $lastX -or $point.Y -ne $lastY) {
        Write-Host "Mouse position: X=$($point.X), Y=$($point.Y)" -NoNewline
        $lastX = $point.X
        $lastY = $point.Y
    }
    
    if ($leftButton -ne 0) {
        Write-Host " [LEFT CLICK DETECTED!]" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 100
}