#!/usr/bin/env bash
# Выкладка MyApp на сервер: аналог publish.ps1 для Linux.
#
# Использование:
#   ./publish.sh                       # сервер по умолчанию (см. SERVER ниже)
#   ./publish.sh user@78.17.9.99       # указать сервер аргументом
#   SKIP_TESTS=1 ./publish.sh          # пропустить тесты (только в крайнем случае)
#
# Что делает: тесты -> публикация -> копирование на сервер ->
# серверный скрипт (остановка, замена файлов, проверка, откат при неудаче) ->
# итоговая проверка сайта.

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

SERVER="${1:-${SERVER:-user@sshch.online}}"
SITE_URL="${SITE_URL:-https://sshch.online}"
REMOTE_TEMP="/tmp/myapp-new"
REMOTE_DEPLOY="/usr/local/bin/myapp-deploy.sh"

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }
fail()  { red "$*"; exit 1; }

command -v dotnet >/dev/null 2>&1 || fail "Не найден dotnet: установите .NET SDK"
command -v ssh    >/dev/null 2>&1 || fail "Не найден ssh"
command -v curl   >/dev/null 2>&1 || fail "Не найден curl"

if [ "${SKIP_TESTS:-0}" != "1" ]; then
    echo "==> Тесты"
    dotnet test --nologo || fail "Тесты не прошли, выкладка отменена"
else
    echo "==> Тесты пропущены (SKIP_TESTS=1)"
fi

echo "==> Публикация"
rm -rf out
dotnet publish src/MyApp.Web -c Release -o out || fail "Публикация не удалась"

echo "==> Копирование на $SERVER:$REMOTE_TEMP"
ssh "$SERVER" "rm -rf '$REMOTE_TEMP' && mkdir -p '$REMOTE_TEMP'" \
    || fail "Не удалось подготовить каталог на сервере"

if command -v rsync >/dev/null 2>&1; then
    rsync -az --delete out/ "$SERVER:$REMOTE_TEMP/" || fail "Копирование (rsync) не удалось"
else
    scp -r out/. "$SERVER:$REMOTE_TEMP/" || fail "Копирование (scp) не удалось"
fi

echo "==> Выкладка на сервере (может потребоваться пароль sudo)"
ssh -t "$SERVER" "sudo $REMOTE_DEPLOY" \
    || fail "Выкладка на сервере завершилась ошибкой (возможно, сработал откат)"

echo "==> Проверка сайта"
body=$(curl -fsS --max-time 15 "$SITE_URL/health/ready") \
    || fail "Сайт не отвечает на $SITE_URL/health/ready"
green "Сайт отвечает: $body"
