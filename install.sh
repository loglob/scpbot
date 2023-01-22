DEST="/srv/scpbot"

serviceStatus=$(systemctl is-active scpbot)
mkdir -p "$DEST"
dotnet publish --self-contained -c Release -r linux-x64 -p:PublishSingleFile=true

if [ "$serviceStatus" = "active" ]; then systemctl stop scpbot; fi;

cp -r bin/Release/net7.0/linux-x64/publish/* "$DEST"
cp -n config.json token "$DEST"

if [ "$serviceStatus" = "active" ]; then systemctl start scpbot; fi;