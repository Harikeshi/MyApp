# Новости (рынки и крипта) в MyApp

Перевод пока не включён. Всё остальное работает на вашей архитектуре: Domain / Application / Infrastructure / Web (Minimal API), `ICurrentUser`, `IUnitOfWork`, конфигурации EF через `ApplyConfigurationsFromAssembly`.

## Что нашёл в заготовке и исправил

- `NewsComment.UserId` был `Guid`, а `NewsCommentConfiguration` вешала внешний ключ на `ApplicationUser.Id` (строка): модель не строилась бы. Теперь `UserId` строковый, как у `TodoItem`.
- Миграция `AddNewsIndexes` создавала `UserId` как `uuid` и без внешнего ключа. Не меняю её: новая миграция `AddNewsFeed` пересоздаёт новостные таблицы (в них ничего нет: воркера и эндпоинтов не было).
- Имя автора комментария не хранится: берётся из `ApplicationUser.DisplayName`, поэтому смена имени в «Настройках» сразу видна в старых комментариях. Комментарии и жалобы удаляются каскадом вместе с аккаунтом (внешний ключ на `AspNetUsers`), поэтому доработок в `/account/delete` не нужно.

## Что добавлено и изменено

Новые файлы:
- `MyApp.Domain/News/`: `NewsTopic`, `NewsCommentReport` (остальные сущности переписаны).
- `MyApp.Application/News/`: `NewsService`, `NewsResponses`, `NewsCursor`, `CommentRules`; `Abstractions/INewsRepository`.
- `MyApp.Infrastructure/News/`: `NewsOptions`, `NewsPollingService`, `NewsCleanupService`, `NewsSeeder`, `NewsHealthCheck`, `TextCleaner`, `KeywordFilter`, `NewsServiceCollectionExtensions`.
- `MyApp.Infrastructure/Persistence/`: `NewsRepository`, миграция `20261121120000_AddNewsFeed` (+ Designer).
- `MyApp.Web/`: `Endpoints/NewsEndpoints`, `Models/NewsDtos`.
- `client/src/features/news/`: вкладка «Новости». `tests/MyApp.Tests/News/NewsRulesTests.cs`.

Изменённые файлы (небольшие правки):
- `MyApp.Domain/News/NewsSource|NewsItem|NewsComment.cs`: переписаны.
- `MyApp.Application/DependencyInjection.cs`: `AddScoped<NewsService>()`.
- `MyApp.Infrastructure/DependencyInjection.cs`: репозиторий и `AddNewsFeed(env)`.
- `MyApp.Infrastructure/MyApp.Infrastructure.csproj`: пакет `System.ServiceModel.Syndication`.
- `MyApp.Infrastructure/Persistence/AppDbContext.cs`: `NewsSources`, `NewsCommentReports`.
- `MyApp.Infrastructure/Persistence/Configurations/NewsConfiguration.cs`: переписан.
- `MyApp.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`: обновлена новостная часть.
- `MyApp.Web/Program.cs`: `app.MapNews()`.
- `MyApp.Web/Endpoints/HealthEndpoints.cs`: `/health/news`.
- `MyApp.Web/Extensions/ServiceCollectionExtensions.cs`: политика лимита `news-comments`.

## Порядок действий

1. Скопируйте файлы поверх проекта (структура архива повторяет вашу).
2. `dotnet build`. Если что-то не сойдётся, пришлите текст ошибки. Я не мог собрать проект в своей среде.
3. Проверьте, что миграция совпадает с моделью (я правил снимок модели вручную):
   ```
   dotnet ef migrations add Check --project MyApp.Infrastructure --startup-project MyApp.Web
   ```
   Миграция должна получиться пустой (`Up`/`Down` без операций). Тогда удалите её: `dotnet ef migrations remove ...`.
   Если в ней что-то есть, сделайте так: `migrations remove` для `Check` и для `AddNewsFeed`, затем `migrations add AddNewsFeed`. Модель уже в коде, EF сгенерирует правильный вариант сам.
4. Если `AddNewsIndexes` уже применялась к какой-то базе, ничего откатывать не надо: `AddNewsFeed` удалит её пустые таблицы и создаст новые.
5. Прод: идемпотентный скрипт от последней применённой миграции (`dotnet ef migrations script <последняя> --idempotent`) и обычная выкладка `publish`.
6. Источники добавятся при первом старте приложения (сидер); первые новости появятся в течение `News:PollIntervalMinutes`.

## Адреса (все требуют входа, как и /todos)

| Метод | Адрес | Что делает |
|---|---|---|
| GET | `/news?topic=crypto\|markets&cursor=...&limit=20` | лента `{ items, nextCursor }`, без `topic` все темы |
| GET | `/news/{id}/comments` | комментарии |
| POST | `/news/{id}/comments` | `{ "text": "..." }`, до 2000 символов, не больше одной https-ссылки |
| DELETE | `/news/comments/{id}` | удалить свой комментарий |
| POST | `/news/comments/{id}/report` | пожаловаться (3 разные жалобы скрывают комментарий) |
| GET | `/health/news` | 503, если ни один источник не обновлялся дольше `News:HealthMaxAgeMinutes` |

Адреса вне `/api`, потому что `/api/**` целиком уходит в AiChat через YARP.

## Настройки (необязательные, переменные окружения)

```
News__UserAgent=MyAppNewsBot/1.0 (+https://ваш-домен)   # желательно задать
News__PollIntervalMinutes=10
News__RetentionDays=60                                  # старые новости без комментариев удаляются
News__Enabled=false                                     # выключить опрос (локально без интернета)
RateLimit__NewsCommentsPerMinute=10                     # лимит комментариев и жалоб на IP
```

В среде `Testing` воркеры и проверка не регистрируются, тесты не ходят в интернет.

## Источники

Стартовые: Cointelegraph, CoinDesk, Decrypt (крипта); MarketWatch, CNBC (рынки, с фильтром по ключевым словам). Bloomberg Markets добавлен выключенным: проверьте адрес `curl`-ом и включите (`UPDATE "NewsSources" SET "IsEnabled" = true WHERE "Name" = 'Bloomberg Markets';`). Новый источник это просто строка в таблице `NewsSources`.

## Мониторинг

`/health/news` не входит в `/health/ready`, чтобы сбой ленты не делал приложение «неготовым». Если захотите, чтобы ваш Telegram-бот следил за новостями, добавьте в него проверку этого адреса.

## Клиент

Папку `client/src/features/news/` положите в ваш `client/src/features/` (или куда у вас складываются вкладки):
1. В `http.ts` подставьте ваш способ получения токена или вызов общего http-клиента.
2. Добавьте вкладку «Новости» рядом с Задачи / Статистика / Чаты и отрисуйте `<NewsTab />`.
3. Картинки грузятся с чужих доменов. Если у страницы или nginx есть CSP, разрешите `img-src https:`; без этого карточки просто останутся без картинок.
4. Темы приходят в `camelCase` (`crypto`, `markets`): так настроен JSON приложения.

## Перевод (когда понадобится)

Всё готово к добавлению без переделок: таблица переводов `(NewsItemId, Lang)`, воркер, который переводит свежие новости через Claude, и кнопка в карточке. Это отдельный шаг с новой миграцией, к нему можно вернуться в любой момент.
