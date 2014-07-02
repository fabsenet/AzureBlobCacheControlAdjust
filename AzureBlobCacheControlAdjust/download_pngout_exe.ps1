param([string]$destinationFolder=".")
Set-PSDebug -Strict

#because the licence of pngout.exe prohibits redistribution, we download it once

# see http://advsys.net/ken/utils.htm for the pngout homepage and licence

$source = "http://advsys.net/ken/util/pngout.exe"
$destination = $destinationFolder + "\" + "pngout.exe"

if(!(Test-Path -Path $destination)){
    Write-Host "downloading pngout.exe to $destination"
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($source, $destination)
    Write-Host "downloading pngout.exe...done"
} else {
    Write-Host "skipping download of pngout.exe because the file exists"
}