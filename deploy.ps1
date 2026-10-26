param([string]$Server = "user@sshch.online")
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

dotnet test --nologo
if ($LASTEXITCODE -ne 0) { throw "Тесты не прошли, выкладка отменена" }

Remove-Item out -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish src\MyApp.Web -c Release -o out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish завершился с ошибкой" }

ssh $Server "rm -rf /tmp/myapp-new && mkdir -p /tmp/myapp-new"
scp -r out\* "${Server}:/tmp/myapp-new/"
ssh -t $Server "sudo /usr/local/bin/myapp-deploy.sh"