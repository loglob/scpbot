serviceStatus=$(systemctl is-active scpbot)
mkdir -p /srv/scpbot
dotnet publish --self-contained -c Release -r linux-x64 -p:PublishSingleFile=true

if [ "$serviceStatus" = "active" ]; then systemctl stop scpbot; fi;

cp -r bin/Release/net6/linux-x64/publish/* /srv/scpbot
cp config.json token /srv/scpbot

if [ "$serviceStatus" = "active" ]; then systemctl start scpbot; fi;