# Notification System

API-only ASP.NET Core solution for scheduling annual notification messages, such as birthdays and anniversaries, at 9:00 AM in each user's local time zone.

## Stack

- ASP.NET Core 9 controllers
- SQL Server / LocalDB
- EF Core 9 migrations
- Hosted background worker for durable notification dispatch
- Typed `HttpClient` for the external email service

## Run Locally

```powershell
dotnet tool restore
dotnet restore
dotnet tool run dotnet-ef database update --project BirthdayNotificationSystem.Api\BirthdayNotificationSystem.Api.csproj --startup-project BirthdayNotificationSystem.Api\BirthdayNotificationSystem.Api.csproj
dotnet run --project BirthdayNotificationSystem.Api\BirthdayNotificationSystem.Api.csproj --launch-profile http
```

The default local URL is:

```text
http://localhost:5036
```

Swagger UI is available at:

```text
http://localhost:5036/swagger
```

## Configuration

`BirthdayNotificationSystem.Api/appsettings.json` contains:

- `ConnectionStrings:DefaultConnection`
- `EmailService:BaseUrl`
- `EmailService:SendMailPath`
- `EmailService:ApiKey`
- `EmailService:FromEmail`
- `NotificationWorker` batch, polling, and lock settings

Set `EmailService:ApiKey` before sending real requests to the email service.

## Endpoints

```http
POST /user
DELETE /user/{id}
DELETE /user?id={id}
PUT /user/{id}
GET /health
```

Example create request:

```json
{
  "firstName": "Ava",
  "lastName": "Taylor",
  "email": "ava.taylor@example.com",
  "birthday": "1992-05-19",
  "anniversaryDate": "2018-09-10",
  "timeZoneId": "Australia/Melbourne",
  "locationText": "Melbourne, Australia"
}
```

## Reliability Notes

The system stores scheduled messages as durable `Notifications` records. Every polling cycle, the worker recovers expired locks, backfills missing upcoming schedules, atomically claims due rows, sends them, retries transient email failures, and creates next year's notification of the same type after a successful send.

Duplicate active notifications are blocked by a filtered unique SQL index on `UserId`, `NotificationType`, and `EventYear`.

February 29 annual events are scheduled on February 28 in non-leap years.
