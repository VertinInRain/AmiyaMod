# 自动冒烟测试脚本：等待卡死游戏退出 -> 安装修复版 DLL -> 直接启动游戏验证 -> 输出报告 -> 关闭实例
$ErrorActionPreference = 'Continue'
$report = 'C:\Users\33761\Desktop\mod2\docs\smoke_test_report.txt'
$logPath = "$env:APPDATA\SlayTheSpire2\logs\godot.log"
$gameDir = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
$buildDll = 'C:\Users\33761\Desktop\mod2\AmiyaMod\.godot\mono\temp\bin\Release\Amiya.dll'
$installedDll = "$gameDir\mods\Amiya\Amiya.dll"

"[$(Get-Date)] 开始监控 PID 12152" | Set-Content $report -Encoding UTF8
$deadline = (Get-Date).AddHours(3)
$installed = $false
while ((Get-Date) -lt $deadline) {
    $p = Get-Process -Id 12152 -ErrorAction SilentlyContinue
    if (-not $p) {
        Copy-Item $buildDll $installedDll -Force
        $fi = Get-Item $installedDll
        $installed = $true
        "[$(Get-Date)] 已安装修复版 $($fi.Length) bytes" | Add-Content $report -Encoding UTF8
        break
    }
    Start-Sleep -Seconds 10
}
if (-not $installed) {
    "[$(Get-Date)] 超时：游戏进程未退出，放弃" | Add-Content $report -Encoding UTF8
    exit 0
}

Start-Sleep -Seconds 3
$before = (Get-Item $logPath).LastWriteTime
$smoke = Start-Process -FilePath "$gameDir\SlayTheSpire2.exe" -WorkingDirectory $gameDir -PassThru
"[$(Get-Date)] 冒烟实例启动 PID=$($smoke.Id)" | Add-Content $report -Encoding UTF8
Start-Sleep -Seconds 170

$log = Get-Content $logPath
"--- 主菜单 ---" | Add-Content $report -Encoding UTF8
$log | Where-Object { $_ -match 'Time to main menu' } | Select-Object -Last 1 | ForEach-Object { $_.Trim() } | Add-Content $report -Encoding UTF8
"--- Amiya 相关行 ---" | Add-Content $report -Encoding UTF8
$log | Where-Object { $_ -match 'Amiya|amiya' } | Select-Object -Last 30 | ForEach-Object { $_.Trim() } | Add-Content $report -Encoding UTF8
"--- 关键异常 ---" | Add-Content $report -Encoding UTF8
$log | Where-Object { $_ -match 'DuplicateModel|CanonicalModel|PoolAttribute|Unhandled|Missing sprite.*amiya|System.Exception' } | Select-Object -Last 12 | ForEach-Object { $_.Trim() } | Add-Content $report -Encoding UTF8

# 仅关闭我们启动的冒烟实例
Stop-Process -Id $smoke.Id -Force -ErrorAction SilentlyContinue
"[$(Get-Date)] 冒烟实例已关闭，报告完成" | Add-Content $report -Encoding UTF8
