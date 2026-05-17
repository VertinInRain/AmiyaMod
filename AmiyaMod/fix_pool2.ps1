$cardDir = "c:\Users\33761\Desktop\mod2\AmiyaMod\Cards"
$utf8 = New-Object System.Text.UTF8Encoding($false)

Get-ChildItem $cardDir -Filter "*.cs" | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName, $utf8)
    
    # Fix: Remove all duplicated 'using BaseLib.Utils;' lines (keep only first one)
    $lines = $content -split "`r`n"
    $newLines = @()
    $seenBaseLib = $false
    foreach ($line in $lines) {
        if ($line -eq 'using BaseLib.Utils;') {
            if (-not $seenBaseLib) {
                $newLines += $line
                $seenBaseLib = $true
            }
            # else skip duplicate
        } else {
            $newLines += $line
        }
    }
    $content = [string]::Join("`r`n", $newLines)
    [System.IO.File]::WriteAllText($_.FullName, $content, $utf8)
}
Write-Host "Deduped!"
