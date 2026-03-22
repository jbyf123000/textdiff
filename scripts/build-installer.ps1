[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $repoRoot "TextDiff.Desktop\TextDiff.Desktop.csproj"
$installerScript = Join-Path $repoRoot "installer\textdiff.iss"

if (-not (Test-Path $projectPath)) {
    throw "找不到项目文件: $projectPath"
}

if (-not (Test-Path $installerScript)) {
    throw "找不到安装脚本: $installerScript"
}

[xml]$projectXml = Get-Content $projectPath
$propertyGroup = $projectXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1
$version = if ($propertyGroup -and $propertyGroup.Version) { $propertyGroup.Version } else { "1.0.0" }

$publishDir = Join-Path $repoRoot "TextDiff.Desktop\bin\$Configuration\net10.0-windows\$Runtime\publish"
$outputDir = Join-Path $repoRoot "installer\Output"

Write-Host "发布应用..."
dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

$isccCommand = Get-Command iscc -ErrorAction SilentlyContinue
$iscc = if ($isccCommand) { $isccCommand.Source } else { $null }
if (-not $iscc) {
    $defaultIscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $defaultIscc) {
        $iscc = $defaultIscc
    }
}

if (-not $iscc) {
    Write-Host ""
    Write-Host "应用已经发布到: $publishDir"
    throw "未检测到 Inno Setup。请先安装 Inno Setup 6，并确保 ISCC.exe 在 PATH 中，或者位于 C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}

Write-Host "生成安装包..."
& $iscc "/DMyAppVersion=$version" "/DMyPublishDir=$publishDir" $installerScript

Write-Host ""
Write-Host "安装包输出目录: $outputDir"
