$ErrorActionPreference = 'Stop'
$cardDir = "C:\Users\33761\Desktop\mod2\AmiyaMod\src\Cards"
$design = (Get-ChildItem "C:\Users\33761\Desktop\mod2\Amiya" -Filter *.txt | Select-Object -First 1).FullName
$outJson = "C:\Users\33761\Desktop\mod2\tools\card_export.json"

$numMap = @{}
foreach ($line in Get-Content $design -Encoding UTF8) {
  if ($line -match '^(\d+)\t([^\t]+)') {
    $n = [int]$matches[1]; $nm = $matches[2].Trim()
    if ($nm -ne '' -and -not $numMap.ContainsKey($nm)) { $numMap[$nm] = $n }
  }
}

$files = Get-ChildItem $cardDir -Filter *.cs | Sort-Object Name
$results = @()
foreach ($f in $files) {
  $text = Get-Content $f.FullName -Raw -Encoding UTF8
  if ($text -notmatch ': BaseAmiyaCard') { continue }
  $cls = [regex]::Match($text, 'public sealed class (\w+)\s*:\s*BaseAmiyaCard')
  if (-not $cls.Success) { continue }
  $name = $cls.Groups[1].Value

  $title = ([regex]::Match($text, '\("title",\s*"([^"]*)"\)')).Groups[1].Value
  $ctor = [regex]::Match($text, 'base\(\s*(-?\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)\s*\)')
  if (-not $ctor.Success) { Write-Host "NO CTOR: $name"; continue }
  $cost = [int]$ctor.Groups[1].Value
  $ctype = $ctor.Groups[2].Value
  $rarity = $ctor.Groups[3].Value

  $desc = ([regex]::Match($text, '\("description",\s*"([^"]*)"\)')).Groups[1].Value
  $desc = $desc -replace '^#', ''

  $upCost = $cost
  $m = [regex]::Match($text, 'EnergyCost\.UpgradeBy\((-?\d+)\)')
  if ($m.Success) { $upCost = $cost + [int]$m.Groups[1].Value }

  $kws = [regex]::Matches($text, 'CardKeyword\.(\w+)') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -notin @('None') } | Select-Object -Unique
  $tags = [regex]::Matches($text, 'AmiyaTags => AmiyaTag\.(\w+)') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -notin @('None') } | Select-Object -Unique
  $kwNote = ''
  if ($text -match 'if \(IsUpgraded\)[\s\S]{0,150}?Array\.Empty<CardKeyword>') { $kwNote = 'upg-removes-ethereal' }
  if ($text -match 'public override int MaxUpgradeLevel') { if ($kwNote) { $kwNote += ' + multi-upgrade' } else { $kwNote = 'multi-upgrade' } }

  $baseDesc = $desc; $upDesc = $desc
  $um = [regex]::Match($desc, '\{IfUpgraded:show:([^}|]*)\|([^}]*)\}')
  if ($um.Success) {
    $baseDesc = $desc.Substring(0, $um.Index) + $um.Groups[2].Value + $desc.Substring($um.Index + $um.Length)
    $upDesc   = $desc.Substring(0, $um.Index) + $um.Groups[1].Value + $desc.Substring($um.Index + $um.Length)
  }

  $results += [ordered]@{
    File = $f.Name; Class = $name; Title = $title; Type = $ctype; Rarity = $rarity;
    Cost = $cost; UpCost = $upCost;
    Keywords = (($kws -join '/')); Tags = (($tags -join '/'));
    Base = $baseDesc; Up = $upDesc; KwNote = $kwNote;
    DesignNo = $numMap[$title];
  }
}
$results | ConvertTo-Json -Depth 4 | Set-Content $outJson -Encoding UTF8
Write-Host "exported $($results.Count) cards -> $outJson"
$results | Select-Object Title, Type, Rarity, Cost, UpCost, Keywords, Tags, KwNote | Format-Table -AutoSize | Out-String -Width 200
