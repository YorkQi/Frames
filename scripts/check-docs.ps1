#Requires -Version 5.1
<#
.SYNOPSIS
    文档一致性检查（D1–D10）。规则说明见 docs/ops/doc-consistency.md。

.DESCRIPTION
    默认检查链接、锚点、front matter、manifest、程序集与 TFM、用例数声明、
    裸数字、表格形状、代码围栏、ADR 索引。
    加 -VerifyTests 时额外执行 dotnet test 并核对用例总数。

.PARAMETER VerifyTests
    运行 dotnet test Frames.sln，用 TRX 中的用例总数核对 manifest.facts.totalTestCases。

.PARAMETER Quiet
    只输出失败项。

.EXAMPLE
    powershell -NoProfile -File scripts/check-docs.ps1
.EXAMPLE
    powershell -NoProfile -File scripts/check-docs.ps1 -VerifyTests
#>
[CmdletBinding()]
param(
    [switch]$VerifyTests,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
$script:Problems = New-Object System.Collections.Generic.List[string]
$script:Root = Split-Path -Parent $PSScriptRoot

function Add-Problem {
    param([string]$Code, [string]$Message)
    $script:Problems.Add("$Code $Message")
}

function Get-RelPath {
    param([string]$FullPath)
    $base = $script:Root.TrimEnd('\', '/')
    $full = [IO.Path]::GetFullPath($FullPath)
    if (-not $full.StartsWith($base, [StringComparison]::OrdinalIgnoreCase)) {
        return ($full -replace '\\', '/')
    }
    return ($full.Substring($base.Length).TrimStart('\', '/') -replace '\\', '/')
}

function Get-Slug {
    param([string]$Heading)
    $s = $Heading
    $s = $s -replace '\[([^\]]+)\]\([^\)]*\)', '$1'   # 链接取文字
    $s = $s -replace '[`*_~]', ''
    $s = $s.Trim()
    $s = $s.ToLowerInvariant()
    $s = $s -replace '[^\w\s-]', ''
    $s = $s -replace '\s+', '-'
    return $s.Trim('-')
}

# 读取文件（UTF-8，去 BOM）
function Read-Text {
    param([string]$Path)
    return [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8)
}

function Get-Lines {
    param([string]$Path)
    return (Read-Text $Path) -split "`r?`n"
}

# 收集文档：docs 下全部 md + 根目录的 md
function Get-DocFiles {
    $list = New-Object System.Collections.Generic.List[string]
    $docsDir = Join-Path $script:Root 'docs'
    Get-ChildItem -Path $docsDir -Recurse -Filter '*.md' -File | ForEach-Object { $list.Add($_.FullName) }
    foreach ($name in @('README.md', 'AGENTS.md', 'CONTRIBUTING.md', 'CHANGELOG.md')) {
        $p = Join-Path $script:Root $name
        if (Test-Path -LiteralPath $p) { $list.Add($p) }
    }
    return $list
}

# 解析 front matter，返回 title/audience/status/updated/sources
function Get-FrontMatter {
    param([string[]]$Lines)
    $fm = @{ title = $null; audience = $null; status = $null; updated = $null; sources = @() }
    if ($Lines.Count -lt 3 -or $Lines[0].Trim() -ne '---') { return $null }
    $end = -1
    for ($i = 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i].Trim() -eq '---') { $end = $i; break }
    }
    if ($end -lt 0) { return $null }
    $inSources = $false
    for ($i = 1; $i -lt $end; $i++) {
        $line = $Lines[$i]
        if ($line -match '^sources:\s*$') { $inSources = $true; continue }
        if ($inSources) {
            if ($line -match '^\s+-\s*(.+?)\s*$') {
                $fm.sources += $Matches[1].Trim('"', "'")
                continue
            }
            $inSources = $false
        }
        if ($line -match '^(title|audience|status|updated):\s*(.+?)\s*$') {
            $fm[$Matches[1]] = $Matches[2].Trim('"', "'")
        }
    }
    return $fm
}

# 收集标题锚点
function Get-Headings {
    param([string[]]$Lines)
    $result = New-Object System.Collections.Generic.List[string]
    $inFence = $false
    foreach ($line in $Lines) {
        if ($line -match '^\s*```') { $inFence = -not $inFence; continue }
        if ($inFence) { continue }
        if ($line -match '^#{1,6}\s+(.+?)\s*#*\s*$') {
            $result.Add((Get-Slug $Matches[1]))
        }
    }
    return $result
}

# ---------------- 读取 manifest ----------------
$manifestPath = Join-Path $script:Root 'docs/manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath)) {
    Add-Problem 'D1' 'docs/manifest.json 不存在'
    Write-Host 'D1 docs/manifest.json 不存在'
    exit 1
}
$manifest = Read-Text $manifestPath | ConvertFrom-Json
$facts = $manifest.facts
$pages = @($manifest.pages)

$docFiles = Get-DocFiles
$headingCache = @{}

# ---------------- D1 manifest 覆盖 ----------------
$pageByPath = @{}
foreach ($p in $pages) {
    $pageByPath[$p.path] = $p
    $full = Join-Path $script:Root $p.path
    if (-not (Test-Path -LiteralPath $full)) {
        Add-Problem 'D1' "manifest 登记的页面不存在：$($p.path)"
    }
}
foreach ($file in $docFiles) {
    $rel = Get-RelPath $file
    if (-not $rel.StartsWith('docs/')) { continue }
    if (-not $pageByPath.ContainsKey($rel)) {
        Add-Problem 'D1' "docs 页面未在 manifest 登记：$rel"
    }
}

# ---------------- 逐文件检查 ----------------
foreach ($file in $docFiles) {
    $rel = Get-RelPath $file
    $lines = Get-Lines $file
    $headings = Get-Headings $lines
    $headingCache[$file] = $headings

    # D10 同文件锚点唯一
    $seen = @{}
    foreach ($h in $headings) {
        if ([string]::IsNullOrEmpty($h)) { continue }
        if ($seen.ContainsKey($h)) {
            Add-Problem 'D10' "${rel} 锚点重复：#$h"
        } else { $seen[$h] = $true }
    }

    # D4 front matter
    $fm = Get-FrontMatter $lines
    if ($rel.StartsWith('docs/')) {
        if ($null -eq $fm) {
            Add-Problem 'D4' "${rel} 缺少 front matter"
        } else {
            foreach ($key in @('title', 'audience', 'status', 'updated')) {
                if ([string]::IsNullOrWhiteSpace($fm[$key])) {
                    Add-Problem 'D4' "${rel} front matter 缺少 $key"
                }
            }
            foreach ($src in $fm.sources) {
                $srcPath = Join-Path $script:Root $src
                if (-not (Test-Path -LiteralPath $srcPath)) {
                    Add-Problem 'D4' "${rel} 的 sources 不存在：$src"
                }
            }
            if ($pageByPath.ContainsKey($rel)) {
                $page = $pageByPath[$rel]
                foreach ($key in @('title', 'status', 'audience')) {
                    if ($fm[$key] -ne $page.$key) {
                        Add-Problem 'D4' "${rel} 的 $key（$($fm[$key])）与 manifest（$($page.$key)）不一致"
                    }
                }
            }
        }
    }

    # D2/D3 链接与锚点；D7 裸数字；D8 围栏与表格
    $inFence = $false
    $tableWidth = -1
    $tableStart = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNo = $i + 1
        if ($line -match '^\s*```') {
            $inFence = -not $inFence
            $tableWidth = -1
            continue
        }
        if ($inFence) { continue }

        # D8 表格列数
        if ($line.TrimStart().StartsWith('|')) {
            $cols = ([regex]::Matches($line, '\|')).Count
            if ($tableWidth -lt 0) {
                $tableWidth = $cols
                $tableStart = $lineNo
            } elseif ($cols -ne $tableWidth) {
                Add-Problem 'D8' "${rel}:${lineNo} 表格列数 $cols 与表头（第 $tableStart 行，$tableWidth 个 |）不一致"
            }
        } else {
            $tableWidth = -1
        }

        # D7 裸数字
        foreach ($m in [regex]::Matches($line, '(\d+)\s*个用例')) {
            $n = [int]$m.Groups[1].Value
            $allowed = @([int]$facts.frameworkTestCases, [int]$facts.sampleTestCases, [int]$facts.totalTestCases)
            if ($allowed -notcontains $n) {
                Add-Problem 'D7' "${rel}:${lineNo} 出现未知用例数 $n（只允许 facts 中的数字）"
            }
        }
        foreach ($m in [regex]::Matches($line, '(\d+)\s*个程序集')) {
            $n = [int]$m.Groups[1].Value
            if ($n -ne [int]$facts.assemblies) {
                Add-Problem 'D7' "${rel}:${lineNo} 程序集数 $n 与 facts.assemblies（$($facts.assemblies)）不一致"
            }
        }

        # D2/D3 链接
        foreach ($m in [regex]::Matches($line, '\]\(([^)\s]+)\)')) {
            $target = $m.Groups[1].Value
            if ($target -match '^(https?|mailto):' -or $target.StartsWith('#') -or $target -match '^[A-Za-z]:') { continue }
            $anchor = ''
            $pathPart = $target
            $hashIdx = $target.IndexOf('#')
            if ($hashIdx -ge 0) {
                $pathPart = $target.Substring(0, $hashIdx)
                $anchor = $target.Substring($hashIdx + 1)
            }
            $targetFull = ''
            if ($pathPart -eq '') {
                $targetFull = $file
            } else {
                $targetFull = Join-Path (Split-Path -Parent $file) $pathPart
            }
            if ($pathPart -ne '' -and -not (Test-Path -LiteralPath $targetFull)) {
                Add-Problem 'D2' "${rel}:${lineNo} -> 链接目标不存在：$pathPart"
                continue
            }
            if ($anchor -ne '' -and $targetFull.EndsWith('.md', [StringComparison]::OrdinalIgnoreCase)) {
                if (-not $headingCache.ContainsKey($targetFull)) {
                    $headingCache[$targetFull] = Get-Headings (Get-Lines $targetFull)
                }
                if ($headingCache[$targetFull] -notcontains $anchor) {
                    Add-Problem 'D3' "${rel}:${lineNo} -> 锚点不存在：$pathPart#$anchor"
                }
            }
        }
    }
    if ($inFence) {
        Add-Problem 'D8' "${rel} 代码围栏未闭合"
    }
}

# ---------------- D5 程序集与 TFM ----------------
$srcDir = Join-Path $script:Root 'src'
$csprojs = @(Get-ChildItem -Path $srcDir -Recurse -Filter 'Frame.*.csproj' -File | Where-Object { $_.Directory.Name -ne 'obj' })
if ($csprojs.Count -ne [int]$facts.assemblies) {
    Add-Problem 'D5' "src 下实际有 $($csprojs.Count) 个 Frame.*.csproj，与 facts.assemblies（$($facts.assemblies)）不一致"
}
$readmeText = Read-Text (Join-Path $script:Root 'README.md')
$agentsText = Read-Text (Join-Path $script:Root 'AGENTS.md')
foreach ($proj in $csprojs) {
    $name = $proj.BaseName
    $xml = Read-Text $proj.FullName
    $tfm = ''
    if ($xml -match '<TargetFramework>\s*([^<]+?)\s*</TargetFramework>') { $tfm = $Matches[1] }
    if ($readmeText -notmatch [regex]::Escape($name)) {
        Add-Problem 'D5' "README.md 未提及程序集 $name"
    }
    if ($agentsText -notmatch [regex]::Escape($name)) {
        Add-Problem 'D5' "AGENTS.md 未提及程序集 $name"
    }
    if ($tfm -ne '' -and $readmeText -notmatch [regex]::Escape($tfm)) {
        Add-Problem 'D5' "README.md 未出现 $name 的目标框架 $tfm"
    }
}

# ---------------- D6 用例数四处一致 ----------------
$total = [string]$facts.totalTestCases
foreach ($path in @('README.md', 'AGENTS.md', 'docs/ops/testing.md')) {
    $full = Join-Path $script:Root $path
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $text = Read-Text $full
    if ($text -notmatch [regex]::Escape($total)) {
        Add-Problem 'D6' "$path 未出现用例总数 $total"
    }
}

# ---------------- D9 ADR 索引覆盖 ----------------
$adrDir = Join-Path $script:Root 'docs/design/adr'
if (Test-Path -LiteralPath $adrDir) {
    $adrIndex = Read-Text (Join-Path $adrDir 'README.md')
    Get-ChildItem -Path $adrDir -Filter '*.md' -File | Where-Object { $_.Name -ne 'README.md' } | ForEach-Object {
        if ($adrIndex -notmatch [regex]::Escape($_.Name)) {
            Add-Problem 'D9' "ADR 索引未登记：$($_.Name)"
        }
    }
}

# ---------------- D6 -VerifyTests ----------------
if ($VerifyTests) {
    if (-not $Quiet) { Write-Host '正在执行 dotnet test ...' }
    $tmp = Join-Path ([IO.Path]::GetTempPath()) ("frames-docs-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp | Out-Null
    Push-Location $script:Root
    try {
        & dotnet test 'Frames.sln' --logger "trx" --results-directory $tmp 2>&1 | Out-Null
        $code = $LASTEXITCODE
        # 每个测试项目各写一个 TRX，必须求和
        $trxFiles = @(Get-ChildItem -Path $tmp -Filter '*.trx' -File)
        if ($code -ne 0) {
            Add-Problem 'D6' "dotnet test 失败（退出码 $code）"
        } elseif ($trxFiles.Count -gt 0) {
            $actual = 0
            foreach ($trxFile in $trxFiles) {
                [xml]$trxXml = Read-Text $trxFile.FullName
                $actual += [int]$trxXml.TestRun.ResultSummary.Counters.total
            }
            if ($actual -ne [int]$facts.totalTestCases) {
                Add-Problem 'D6' "实测用例总数 $actual（$($trxFiles.Count) 个测试项目）与 facts.totalTestCases（$($facts.totalTestCases)）不一致"
            }
        } else {
            Add-Problem 'D6' '未生成 TRX，无法核对用例数'
        }
    } finally {
        Pop-Location
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# ---------------- 输出 ----------------
if ($script:Problems.Count -eq 0) {
    Write-Host 'OK 文档一致性检查通过。'
    exit 0
}
foreach ($p in $script:Problems) { Write-Host $p }
Write-Host ("FAIL 共 {0} 个问题。" -f $script:Problems.Count)
exit 1
