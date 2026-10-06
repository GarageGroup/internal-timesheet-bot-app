# Инфраструктура и CI/CD Timesheet Telegram Bot

## Что управляется этим проектом

CI/CD бота создаёт или обновляет:

- Linux Function App с .NET 10 isolated;
- System Assigned Managed Identity Function App;
- привязку существующей User Assigned Managed Identity для доступа к Dataverse;
- привязку отдельной User Assigned Managed Identity для вызова Agent API;
- APIM бота в Resource Group приложения, маршруты `/bot/message` и `/telegram/api`, named values и подписки;
- Telegram webhook на `/bot/message` после публикации кода;
- App Settings Function App;
- сборку, публикацию ZIP в Blob Storage и развёртывание кода при первом `install`.

CI/CD бота не создаёт и не изменяет:

- App Service Plan, Storage Account, Application Insights и UAMI Dataverse — это общие зависимости,
  создаваемые инфраструктурой Timesheet API;
- App Registration агента, роль `Timesheet.Agent.Invoke` и её назначение агентской UAMI бота —
  всё это настраивает CI/CD API до создания Function App бота;
- Azure Bot resource, Mini App и Static Web App; Telegram webhook направлен прямо в APIM;
- общий Storage Account релизных артефактов.

В Test есть дополнительные старые операции APIM (`provide-claims`, `oauth2/callback`,
`telegram/message/send`). Эти два приложения их не вызывают; `install.yml` настраивает
маршруты, которые реально используют текущий код и Telegram webhook. Существующие старые
операции при повторном `install` не удаляются.

## Workflow

- `build.yml` проверяет Bicep и shell-скрипты, собирает .NET 10 решение и запускает тесты.
- `install.yml` создаёт/обновляет Function App и APIM, публикует bootstrap ZIP через Blob Storage,
  проверяет health и устанавливает Telegram webhook.
  Если у новой Function App ключ ещё не сформирован, APIM сначала получает временное значение;
  после публикации кода `finalize-apim.sh` заменяет его реальным ключом и проверяет маршрут через APIM.
- `publish.yml` создаёт ZIP GitHub Release, публикует его в Blob Storage, скачивает тот же ZIP,
  разворачивает Test и проверяет health endpoint.
- `deploy.yml` вручную разворачивает существующий ZIP на Test или Prod без повторной сборки.
- `delete.yml` удаляет ZIP удалённого Release из Blob Storage и удаляет Git-тег.

## Что должно существовать заранее

Для каждой среды до первого `install.yml` должны существовать:

1. Resource Group приложения либо право администратора создать её.
2. Успешно выполненный API `install.yml`: он создаёт App Service Plan, Storage Account, Application Insights,
   обе User Assigned MI, API Web App и настройки Agent API.
3. Azure deployment App Registration с Service Principal.
4. Telegram-бот и его token в настройках API Web App; Mini App URL и welcome image URL.
5. Общий artifact Storage Account; контейнер релизов создаст `install.yml`.
6. GitHub Environments с точными именами `Test` и `Prod`.

Client Secret для deployment App Registration не используется: GitHub входит через OIDC.

## Shell-скрипт для администратора

Скрипт выполняется администратором один раз для каждой среды. Администратору нужны права создать
Resource Group и назначить `Contributor`.

Ниже приведён полный скрипт для Test с подтверждённым Client ID deployment-приложения.

```shell
az login --tenant 69879a40-68bb-4b30-941b-eb9692ddd9b4

export GITHUB_ORG='GarageGroup'
export GITHUB_REPO='internal-timesheet-bot-app'
export GITHUB_ENVIRONMENT='Test'

export AZURE_DEPLOY_APP_ID='a9205b63-de76-4b11-9e61-0e4c6be04991'

export AZURE_SUBSCRIPTION_ID='73a6f94e-bfb4-4926-a9dd-09228d53a2a5'
export AZURE_RESOURCE_GROUP_NAME='rg-garage-timesheet-test'
export AZURE_LOCATION='westeurope'

set -euo pipefail

required=(
  GITHUB_ORG
  GITHUB_REPO
  GITHUB_ENVIRONMENT
  AZURE_DEPLOY_APP_ID
  AZURE_SUBSCRIPTION_ID
  AZURE_RESOURCE_GROUP_NAME
  AZURE_LOCATION
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Не задана переменная $variable" >&2; exit 1; }
done

subject="repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${GITHUB_ENVIRONMENT}"
credential_name="github-${GITHUB_REPO}-${GITHUB_ENVIRONMENT,,}"

create_federated_credential() {
  local app_id="$1"
  local existing
  local parameters

  existing="$(az ad app federated-credential list \
    --id "$app_id" \
    --query "[?name=='$credential_name'].name | [0]" \
    --output tsv \
    --only-show-errors)"

  if [[ -z "$existing" ]]; then
    parameters="$(jq -n \
      --arg name "$credential_name" \
      --arg subject "$subject" \
      '{
        name: $name,
        issuer: "https://token.actions.githubusercontent.com",
        subject: $subject,
        audiences: ["api://AzureADTokenExchange"]
      }')"

    az ad app federated-credential create \
      --id "$app_id" \
      --parameters "$parameters" \
      --output none \
      --only-show-errors
  fi
}

create_federated_credential "$AZURE_DEPLOY_APP_ID"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

if [[ "$(az group exists \
  --name "$AZURE_RESOURCE_GROUP_NAME" \
  --output tsv \
  --only-show-errors)" == false ]]; then
  az group create \
    --name "$AZURE_RESOURCE_GROUP_NAME" \
    --location "$AZURE_LOCATION" \
    --tags \
      'application=garage-timesheet-bot' \
      "environment=${GITHUB_ENVIRONMENT,,}" \
      'managedBy=github-actions' \
    --output none \
    --only-show-errors
fi

application_scope="/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME"
az role assignment create \
  --assignee "$AZURE_DEPLOY_APP_ID" \
  --role 'Contributor' \
  --scope "$application_scope" \
  --output none \
  --only-show-errors

echo 'GitHub OIDC и Azure RBAC настроены.'
```

Для Prod скрипт повторяется с `GITHUB_ENVIRONMENT='Prod'`, production Client ID, subscription,
Resource Group и location. `GITHUB_ENVIRONMENT` сохраняет регистр имени GitHub Environment; нижний
регистр для имени OIDC credential и Azure tag формируется автоматически через `${GITHUB_ENVIRONMENT,,}`.

## Repository variables и secrets

Эти значения используются задачами без выбранного GitHub Environment.

| Имя | Тип | Назначение |
| --- | --- | --- |
| `GG_NUGET_SOURCE_URL` | Variable | URL приватного Garage Group NuGet source |
| `GG_NUGET_SOURCE_USER_NAME` | Secret | Пользователь NuGet source |
| `GG_NUGET_SOURCE_USER_PASSWORD` | Secret | Пароль NuGet source |
| `AZURE_ARTIFACT_NAME` | Variable | Базовое имя ZIP, например `internal-timesheet-bot` |
| `AZURE_ARTIFACT_ACCOUNT_NAME` | Variable | Общий artifact Storage Account |
| `AZURE_ARTIFACT_CONTAINER_NAME` | Variable | Blob Container релизов; будет создан при установке |
| `AZURE_ACCOUNT_KEY_ARTIFACT` | Secret | Ключ artifact Storage Account |

## GitHub Environment variables

### Azure

| Имя | Test-значение или назначение |
| --- | --- |
| `DEPLOY_CLIENT_ID` | Client ID Azure deployment App Registration |
| `DEPLOY_TENANT_ID` | `69879a40-68bb-4b30-941b-eb9692ddd9b4` |
| `DEPLOY_SUBSCRIPTION_ID` | `73a6f94e-bfb4-4926-a9dd-09228d53a2a5` |
| `AZURE_RESOURCE_GROUP_NAME` | `rg-garage-timesheet-test` |
| `AZURE_LOCATION` | `westeurope`; используется при создании Function App |
| `AZURE_NAME_POSTFIX` | `test` |
| `FUNCTION_APP_NAME` | `func-internal-gtimesheet-test` |
| `APP_SERVICE_PLAN_NAME` | `asp-garage-timesheet-test` |
| `STORAGE_ACCOUNT_NAME` | `stinternalgtimesheettest` |
| `APPLICATION_INSIGHTS_NAME` | `appi-internal-gtimesheet-test` |
| `DATAVERSE_MANAGED_IDENTITY_NAME` | `id-internal-gtimesheet-test` |
| `API_WEB_APP_NAME` | `app-garage-timesheet-service-test`; Web App API в той же Resource Group |
| `APIM_SERVICE_NAME` | `apim-integration-platform-test-01`; общий APIM для Agent API |
| `APIM_AGENT_API_PATH` | Путь Agent API в APIM; совпадает с переменной API pipeline |
| `BOT_APIM_SERVICE_NAME` | `apim-garage-timesheet-test`; APIM бота в Resource Group приложения |
| `BOT_APIM_LOCATION` | `northeurope` для нового Test; для существующего APIM регион сохраняется |
| `BOT_APIM_PUBLISHER_EMAIL` | `pm@garage-group.io` для Test; адрес publisher нового APIM бота |
| `BOT_APIM_PUBLISHER_NAME` | `Garage Group` для Test; имя publisher нового APIM бота |

App Service Plan, Storage, Application Insights и обе UAMI должны существовать. Bicep использует их как
shared dependencies. Для уже существующей Function App её фактическая локация сохраняется; совпадение
с `AZURE_LOCATION` не проверяется. Существующие tags Function App также сохраняются, включая
служебные Application Insights links; для новой Function App добавляются project/environment tags.

### Интеграции приложения

| Имя | Test-значение или назначение |
| --- | --- |
| `DATAVERSE_SERVICE_URL` | `https://garage-stage.crm4.dynamics.com/` |
| `BOT_WEB_APP_BASE_ADDRESS` | URL Test Mini App |
| `WELCOME_IMAGE_URL` | URL изображения приветствия |
| `TIMESHEET_LIMITATION_DAY_OF_MONTH` | `10` |

При запуске bot `install.yml` аудитория `AgentApi__Audience` читается из настроек API Web App,
`AgentApi__BaseAddress` строится из APIM service и path, а `AgentApi__ManagedIdentityClientId`
берётся из созданной API агентской UAMI. Бот использует её для токена Agent API;
`AZURE_CLIENT_ID` остаётся привязанным к отдельной Dataverse UAMI.
`TelegramBot__BaseAddress` и `Bot__FileUrlTemplate` рассчитываются из `BOT_APIM_SERVICE_NAME`.
`TelegramBot__ApiKey` берётся из подписки `TelegramApiSubscription` APIM; это не Telegram token.

## GitHub Environment secrets

Telegram token бот получает из настроек API Web App, куда его записывает API `install.yml`.
Отдельный secret с токеном в репозитории бота не нужен.

Publish profiles, Function keys и Azure Client Secret больше не хранятся в GitHub. Deployment идёт
через OIDC, а health key читается из Function App непосредственно перед проверкой и маскируется в логе.

## Порядок первого запуска

1. Создать Azure deployment App Registration бота и выполнить административный shell-скрипт для Test.
2. Заполнить repository variables/secrets и GitHub Environment `Test`.
3. Запустить API `install.yml` для `Test`; он создаст общие ресурсы, агентскую UAMI,
   настроит общий APIM и опубликует код API.
4. Запустить bot `install.yml` для `Test`; он создаст Function App и APIM бота, подключит обе UAMI,
   опубликует код, проверит health и установит webhook Telegram.
5. Проверить пользовательский сценарий в боте. Для последующих версий создавать GitHub Release:
   `publish.yml` развёртывает Test, а `deploy.yml` переносит тот же ZIP на Prod.

Read-only Azure `what-if` для нового APIM-шаблона на существующем Test не показал удаления или замены
ресурсов. Он показал обновление Timesheet API, операций и политик APIM с той же маршрутизацией.
Отдельно при установке ожидается изменение существующей Function App:

- runtime stack `DOTNET-ISOLATED|8.0` → `DOTNET-ISOLATED|10.0`;
- включение HTTP/2;
- отключение FTPS;
- явная фиксация TLS 1.2 и 64-bit worker.

Существующие System Assigned/UAMI identities и служебные Application Insights tags сохраняются.
Azure при подготовке файлов не изменялся.

Все изменения Azure, Entra ID и production-параметров должны фиксироваться в проектной документации
без публикации секретных значений.

## .NET 10 Function worker

Для этого проекта важно оставить в `AzureFunc.csproj` параметры
`FunctionsEnableWorkerIndexing=false`, `FunctionsEnableExecutorSourceGen=false` и
`FunctionsAutoRegisterGeneratedFunctionsExecutor=false`. Они повторяют конфигурацию
рабочих .NET 10 Function-проектов AMBY. Без них `worker.config.json` содержит
`workerIndexing: true`: локальный Azure Functions Core Tools 4.8.0 запускает worker,
но сообщает `No job functions found`, хотя `functions.metadata` содержит функции.
С параметрами выше `workerIndexing: false`, функции индексируются хостом.

При ручной ZIP-публикации проверьте, что пути внутри архива используют `/`, а
`host.json`, `functions.metadata` и `worker.config.json` лежат в корне. На Windows
`Compress-Archive` записывает вложенные пути с `\`; такой архив не следует
публиковать без проверки. Маршрут `/health` имеет `authLevel: Function`, поэтому
прямой запрос без `x-functions-key` корректно возвращает 401.

CI/CD запускает `.infra/scripts/validate-function-package.sh` после публикации и
перед развёртыванием готового ZIP. Скрипт проверяет индексирование, наличие
`HealthCheck`, `HandleBotHttp`, `HandleBotEntity`, корневые файлы и пути архива.
Обычный deploy также останавливается до публикации, если Function App не настроена
на `DOTNET-ISOLATED|10.0`. После публикации остаётся проверка `/health` с ключом.
