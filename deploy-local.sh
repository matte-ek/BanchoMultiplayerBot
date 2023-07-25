#!/bin/bash

cd BanchoMultiplayerBot.Host.Web

source ~/.deploy-configuration

echo -n "Building project ... "

if ! dotnet publish -c Release > /dev/null; then
    echo "FAILED"
    echo "Failed to publish web binaries, exiting."
    exit 1
fi

echo "OK"

echo -n "Preparing package ... "

cd bin/Release/net6.0

rm -f publish/config.json > /dev/null
rm -f publish/appsettings.json > /dev/null
rm -f publish/lobby_states.json > /dev/null
rm -f publish/bot.db > /dev/null
rm -f publish/release.zip > /dev/null
rm -f publish.zip > /dev/null

cd publish;
zip -r ../publish.zip * > /dev/null
cd ..

if ! [ -f "publish.zip" ]; then
    echo "FAILED"
    echo "Missing archive, zip probably failed, exiting..."
    exit 2
fi

echo "OK"
echo -n "Uploading package ... "

if ! scp publish.zip root@${IpAddress}:/home/osu/release.zip > /dev/null; then
    echo "FAILED"
    exit 3
fi

echo "OK"

# We're done here, hand over deployment to the bash script on the server
ssh root@${IpAddress} "su - osu -c /home/osu/startup.sh"
