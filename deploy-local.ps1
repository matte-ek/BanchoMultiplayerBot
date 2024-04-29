# Script to upgrade bot on remote server

Push-Location

$IpAddress = $Env:OsuBot

Set-Location ".\BanchoMultiplayerBot.Host.Web"

if (!$IpAddress)
{
    Write-Host "Failed to find 'OsuBot' environment variable."
    Exit
}

Write-Host "Deploying to $IpAddress"

Write-Host -NoNewline "Building project ... "

dotnet publish -c Release | Out-Null

if ($LastExitCode -ne 0)
{
    Write-Host "ERROR"
    Write-Host "Failed to compile project binaries"
    Exit
}

Write-Host "OK"

Write-Host -NoNewline "Preparing package ... "

Set-Location ".\bin\Release\net8.0"

Remove-Item ".\publish\config.json" 2>$null
Remove-Item ".\publish\appsettings.json" 2>$null
Remove-Item ".\publish\lobby_states.json" 2>$null
Remove-Item ".\publish\bot.db" 2>$null
Remove-Item ".\publish\release.zip" 2>$null
Remove-Item ".\publish.zip" 2>$null

7z a publish.zip .\publish\* | Out-Null

if (!(Test-Path publish.zip -PathType Leaf))
{
    Write-Host "ERROR"
    Write-Host "Failed to create package"
    Exit
}

Write-Host "OK"

Write-Host -NoNewline "Uploading package ... "

scp .\publish.zip root@$($IpAddress):/home/osu/release.zip | Out-Null

if ($LastExitCode -ne 0)
{
    Write-Host "ERROR"
    Write-Host "Failed to upload package"
    Exit
}

Write-Host "OK"

ssh root@$($IpAddress) "su - osu -c /home/osu/startup.sh"

Pop-Location