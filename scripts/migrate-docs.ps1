#Requires -Version 5.1
<#
.SYNOPSIS
    One-shot migration: move docs into guide/concepts/design/ops, rewrite links,
    and inject YAML front matter from docs/manifest.json.

.DESCRIPTION
    This script is temporary tooling for the documentation re-plan. It is safe to
    run once; it refuses to move a file that does not exist and leaves already
    migrated files untouched.

    Run from anywhere:  powershell -NoProfile -File scripts/migrate-docs.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$map = [ordered]@{
    'docs/getting-started.md' = 'docs/guide/getting-started.md'
    'docs/samples.md'         = 'docs/guide/samples.md'
    'docs/architecture.md'    = 'docs/concepts/architecture.md'
    'docs/modules.md'         = 'docs/concepts/modules.md'
    'docs/core.md'            = 'docs/concepts/core.md'
    'docs/unit-of-work.md'    = 'docs/concepts/unit-of-work.md'
    'docs/domain-events.md'   = 'docs/concepts/domain-events.md'
    'docs/auditing.md'        = 'docs/concepts/auditing.md'
    'docs/databases.md'       = 'docs/concepts/databases.md'
    'docs/locks.md'           = 'docs/concepts/locks.md'
    'docs/eventbus.md'        = 'docs/concepts/eventbus.md'
    'docs/scheduler.md'       = 'docs/concepts/scheduler.md'
    'docs/redis.md'           = 'docs/concepts/redis.md'
    'docs/design-notes.md'    = 'docs/design/design-notes.md'
    'docs/testing.md'         = 'docs/ops/testing.md'
}

$reverse = @{}
foreach ($k in $map.Keys) { $reverse[$map[$k]] = $k }

# ---------------------------------------------------------------- directories
$dirs = @(
    'docs/guide', 'docs/concepts', 'docs/design', 'docs/design/adr', 'docs/ops',
    'scripts', '.githooks', '.github/workflows'
)
foreach ($d in $dirs) {
    if (-not (Test-Path (Join-Path $root $d))) {
        New-Item -ItemType Directory -Path (Join-Path $root $d) -Force | Out-Null
    }
}

# --------------------------------------------------------------------- moves
foreach ($k in $map.Keys) {
    $from = Join-Path $root $k
    $to   = Join-Path $root $map[$k]
    if (-not (Test-Path $from)) {
        Write-Host "skip (already moved): $k"
        continue
    }
    if (Test-Path $to) {
        throw "target already exists: $($map[$k])"
    }
    & git mv $k $map[$k]
    if ($LASTEXITCODE -ne 0) { throw "git mv failed: $k -> $($map[$k])" }
}

# ------------------------------------------------------------ path utilities
function Get-RelPath {
    param([string]$FromDir, [string]$ToPath)
    $fromUri = New-Object System.Uri((($FromDir.TrimEnd('\')) + '\'))
    $toUri   = New-Object System.Uri($ToPath)
    $rel = $fromUri.MakeRelativeUri($toUri).ToString()
    return [System.Uri]::UnescapeDataString($rel).Replace('\', '/')
}

function Get-RootRel {
    param([string]$FullPath)
    $p = [System.IO.Path]::GetFullPath($FullPath)
    if ($p.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $p.Substring($root.Length).TrimStart('\', '/').Replace('\', '/')
    }
    return $null
}

# --------------------------------------------------------------- link rewrite
$files = @()
$files += Get-ChildItem -Path $root -Filter *.md -File
$files += Get-ChildItem -Path (Join-Path $root 'docs') -Filter *.md -Recurse -File
$memoryDir = Join-Path $root '.claude/memory'
if (Test-Path $memoryDir) {
    $files += Get-ChildItem -Path $memoryDir -Filter *.md -File
}

$linkPattern = '\]\((?<t>[^)\s]+)\)'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$rewritten = 0

foreach ($file in ($files | Sort-Object FullName -Unique)) {
    $newFull = $file.FullName
    $newRel  = Get-RootRel $newFull
    $oldRel  = if ($reverse.ContainsKey($newRel)) { $reverse[$newRel] } else { $newRel }
    $oldFull = Join-Path $root $oldRel
    $oldDir  = [System.IO.Path]::GetDirectoryName($oldFull)
    $newDir  = [System.IO.Path]::GetDirectoryName($newFull)

    $text = [System.IO.File]::ReadAllText($newFull, [System.Text.Encoding]::UTF8)
    $lines = $text -split "`r?`n"
    $inFence = $false
    $changed = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line.TrimStart().StartsWith('```')) { $inFence = -not $inFence; continue }
        if ($inFence) { continue }

        $newLine = [regex]::Replace($line, $linkPattern, {
            param($m)
            $t = $m.Groups['t'].Value
            if ($t -match '^(https?:|mailto:|#)') { return $m.Value }
            $anchor = ''
            $hash = $t.IndexOf('#')
            if ($hash -ge 0) {
                $anchor = $t.Substring($hash)
                $t = $t.Substring(0, $hash)
            }
            if ([string]::IsNullOrWhiteSpace($t)) { return $m.Value }
            if ($t -match '^[A-Za-z]:[\\/]') { return $m.Value }

            $oldTarget = [System.IO.Path]::GetFullPath((Join-Path $oldDir $t))
            $targetRel = Get-RootRel $oldTarget
            $newTarget = $oldTarget
            if ($targetRel -and $map.Contains($targetRel)) {
                $newTarget = Join-Path $root $map[$targetRel]
            }

            $relNew = Get-RelPath $newDir $newTarget
            return "]($relNew$anchor)"
        })

        if ($newLine -ne $line) {
            $lines[$i] = $newLine
            $changed = $true
        }
    }

    if ($changed) {
        [System.IO.File]::WriteAllText($newFull, ($lines -join "`r`n"), $utf8)
        $rewritten++
        Write-Host "links rewritten: $newRel"
    }
}

Write-Host "files with rewritten links: $rewritten"

# ------------------------------------------------------------- front matter
$manifestPath = Join-Path $root 'docs/manifest.json'
if (-not (Test-Path $manifestPath)) { throw 'docs/manifest.json not found' }
$manifest = [System.IO.File]::ReadAllText($manifestPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json

$injected = 0
foreach ($page in $manifest.pages) {
    $full = Join-Path $root $page.path
    if (-not (Test-Path $full)) { Write-Host "missing page (skip): $($page.path)"; continue }
    $text = [System.IO.File]::ReadAllText($full, [System.Text.Encoding]::UTF8)
    if ($text -match '^---\r?\n') { continue }

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("---`n")
    [void]$sb.Append("title: $($page.title)`n")
    [void]$sb.Append("audience: $($page.audience)`n")
    [void]$sb.Append("status: $($page.status)`n")
    [void]$sb.Append("updated: $($page.updated)`n")
    [void]$sb.Append("sources:`n")
    foreach ($s in $page.sources) { [void]$sb.Append("  - $s`n") }
    [void]$sb.Append("---`n`n")

    [System.IO.File]::WriteAllText($full, $sb.ToString() + $text, $utf8)
    $injected++
}
Write-Host "front matter injected: $injected"
