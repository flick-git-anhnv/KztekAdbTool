<#
.SYNOPSIS
    Cài đặt hệ thống Gemini Agent toàn cầu (Global Setup) cho máy tính.
.DESCRIPTION
    Script này sẽ tự động tạo các Junction Links (thư mục ảo) bên trong C:\Users\<user>\.gemini
    trỏ về thư mục cấu hình chuẩn (.gemini) trong kho lưu trữ Git này.
    
    Việc này giúp đồng bộ toàn bộ luồng làm việc, agents, và rules cho mọi project trên máy tính
    mà không làm khóa (lock) thư mục cấu hình cục bộ của Gemini Agent.
#>

$ErrorActionPreference = "Stop"

# Lấy đường dẫn của kho lưu trữ hiện tại (Nơi chứa script này)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoGemini = Join-Path $ScriptDir ".gemini"
$RepoScripts = Join-Path $ScriptDir "scripts"

# Thư mục gốc của Gemini trên máy tính user
$UserGeminiHome = Join-Path $env:USERPROFILE ".gemini"

Write-Host "=== CÀI ĐẶT GEMINI AGENT (GLOBAL) ===" -ForegroundColor Cyan
Write-Host "Repo nguồn: $ScriptDir" -ForegroundColor Gray
Write-Host "Thư mục đích: $UserGeminiHome`n" -ForegroundColor Gray

if (-not (Test-Path $RepoGemini)) {
    Write-Error "Không tìm thấy thư mục cấu hình chuẩn tại: $RepoGemini"
    exit 1
}

if (-not (Test-Path $UserGeminiHome)) {
    Write-Host "Đang tạo thư mục gốc $UserGeminiHome..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $UserGeminiHome | Out-Null
}

$folders = @("agents", "commands", "evals", "hooks-kztek", "lessons", "templates", "shared")

Write-Host "Đang thiết lập các Junction Links..."
foreach ($f in $folders) {
    $targetPath = Join-Path $UserGeminiHome $f
    $sourcePath = Join-Path $RepoGemini $f
    
    if (Test-Path $sourcePath) {
        if (Test-Path $targetPath) {
            Write-Host "  [-] Xóa thư mục/link cũ: $f" -ForegroundColor DarkGray
            Remove-Item $targetPath -Recurse -Force
        }
        New-Item -ItemType Junction -Path $targetPath -Value $sourcePath | Out-Null
        Write-Host "  [+] Đã liên kết: $f" -ForegroundColor Green
    } else {
        Write-Host "  [!] Bỏ qua $f (không tồn tại trong Repo)" -ForegroundColor Yellow
    }
}

# Xử lý riêng cho thư mục scripts (nằm ngoài thư mục .gemini ở Repo)
$scriptsTarget = Join-Path $UserGeminiHome "scripts"
if (Test-Path $RepoScripts) {
    if (Test-Path $scriptsTarget) {
        Remove-Item $scriptsTarget -Recurse -Force
    }
    New-Item -ItemType Junction -Path $scriptsTarget -Value $RepoScripts | Out-Null
    Write-Host "  [+] Đã liên kết: scripts" -ForegroundColor Green
}

Write-Host "`n=== HOÀN TẤT ===" -ForegroundColor Cyan
Write-Host "Hệ thống Gemini Agent của bạn đã sẵn sàng!"
Write-Host "Bây giờ bạn có thể mở các project làm việc và copy bộ 3 file (GEMINI.md, RULES.md, WORKFLOW.md) sang đó để bắt đầu." -ForegroundColor Yellow
