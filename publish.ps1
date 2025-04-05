New-Item -ItemType Directory -Path .\publish -Force
if (Test-Path -Path ".\publish\anti-sb.cipx") {
    Remove-Item -Path ".\publish\anti-sb.cipx" -Force
}
Remove-Item ".\bin\Release\net8.0-windows\*.pdb"
Compress-Archive -Path ".\bin\Release\net8.0-windows\*" -DestinationPath "./publish/Anti-SB.cipx"


$path="publish"

Write-Output $path
Remove-Item $path/*.md5sum
Remove-Item $path/*.md
$files = Get-ChildItem $path
$hashes = [ordered]@{}
$summary = "
> [!important]
> 下载时请注意核对文件MD5是否正确。

| 文件名 | MD5 |
| --- | --- |
"

foreach ($i in $files) {
    $name = $i.Name
    $hash = Get-FileHash $i -Algorithm MD5
    $hashString = $hash.Hash
    $hashes.Add($name, $hashString)
    Write-Output $hash.Hash > "${i}.md5sum"
    $summary +=  "| $name | ``${hashString}`` |`n"
}

echo $hashes

$json = ConvertTo-Json $hashes -Compress

$summary +=  "`n<!-- CLASSISLAND_PKG_MD5 ${json} -->" 
echo $summary > "$path/checksums.md"
Write-Host "MD5 Summary:" -ForegroundColor Gray
Write-Host $summary -ForegroundColor Gray
Write-Host "----------" -ForegroundColor Gray

#if (-not $GITHUB_ACTION -eq $null) {
#    'MD5_SUMMARY=' + $summary.Replace("`n", "<<") | Out-File -FilePath $env:GITHUB_ENV -Append
#}
