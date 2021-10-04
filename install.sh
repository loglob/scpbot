serviceStatus=$(systemctl is-active scpbot)
mkdir -p /srv/scpbot
dotnet publish --self-contained -c Release -r linux-x64 -p:PublishSingleFile=true

if [ "$serviceStatus" = "active" ]; then systemctl stop scpbot; fi;

cp -r bin/Release/netcoreapp3.1/linux-x64/publish/* /srv/scpbot
cp config.json token scpbot.service /srv/scpbot

if [ "$serviceStatus" = "active" ]; then systemctl start scpbot; fi;