# Что добавлено

- Задачи: срок = дата и время, исполнитель, проекты (владелец + ответственные), меню «⋯» (Изменить / Удалить) у задач и проектов.
- Notes: записи с тегами task/faq, текст с цветом и картинками, всё в PostgreSQL.
- Поиск в шапке: задачи, проекты, Notes, названия чатов.
- Долгая сессия: токен доступа 2 часа (Auth:AccessTokenMinutes), refresh-токен 30 дней (Auth:RefreshTokenDays), клиент обновляет токен сам.

# Миграция БД (обязательно)

Папки Migrations в архиве не было, поэтому миграцию нужно создать у себя:

    dotnet ef migrations add TasksProjectsNotes --project src/MyApp.Infrastructure --startup-project src/MyApp.Web

Затем откройте созданный файл и в методе Up() убедитесь в таком порядке (EF по умолчанию теряет старые сроки):

1. `AddColumn DueAt` у Todos (nullable: true, временно)
2. `migrationBuilder.Sql("UPDATE \"Todos\" SET \"DueAt\" = COALESCE(\"DueDate\"::timestamp AT TIME ZONE 'UTC', \"CreatedAt\");");`
3. `AlterColumn DueAt` -> nullable: false
4. `DropColumn DueDate`

То есть: сначала добавить DueAt как NULL, заполнить из DueDate, потом сделать NOT NULL и удалить DueDate.
Остальное (Projects, ProjectMembers, Notes, AssigneeId, ProjectId, индексы) EF сгенерирует сам.
Для выкладки: `dotnet ef migrations script --idempotent` как у вас в migrate.sql.

# Не проверено сборкой

В среде, где писался код, не было .NET SDK и пакетов npm: C# и React-код не собирались и тесты не запускались.
Запустите: `dotnet build`, `dotnet test`, `cd client && npm run build`.
