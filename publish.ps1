$Server = "user@78.17.9.99"
$RemoteTemp = "/tmp/myapp-new"

Remove-Item out -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish src\MyApp.Web -c Release -o out
if ($LASTEXITCODE -ne 0) { Pause; exit 1 }

ssh $Server "rm -rf $RemoteTemp && mkdir -p $RemoteTemp"
if ($LASTEXITCODE -ne 0) { Pause; exit 1 }

scp -r out\* "${Server}:${RemoteTemp}/"
if ($LASTEXITCODE -ne 0) { Pause; exit 1 }

ssh $Server "~/deploy-myapp.sh"

Pause