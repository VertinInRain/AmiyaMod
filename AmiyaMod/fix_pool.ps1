$cardDir = "c:\Users\33761\Desktop\mod2\AmiyaMod\Cards"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

Get-ChildItem $cardDir -Filter "*.cs" | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName, $utf8NoBom)
    
    # Add using BaseLib.Utils if missing
    if ($content -notmatch 'using BaseLib\.Utils;') {
        $content = $content -replace 'using MegaCrit\.Sts2\.Core\.', "using BaseLib.Utils;`r`nusing MegaCrit.Sts2.Core."
    }
    
    # Add Pool after namespace line (if not already there)
    if ($content -notmatch '\[Pool\(') {
        $content = $content -replace '(namespace Amiya\.Cards;)', "`$1`r`n`r`n[Pool(typeof(AmiyaCardPool))]"
    }
    
    [System.IO.File]::WriteAllText($_.FullName, $content, $utf8NoBom)
}
Write-Host "All card files updated"
