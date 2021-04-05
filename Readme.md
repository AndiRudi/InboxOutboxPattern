# Inbox Outbox Pattern Tests

This repository contains a console application simulating the inbox and outbox patterns using two services, a (faked) RabbitMQ and Hangfire. It was created to support the Inbox Outbox Article on my [blog](https://andirudi.github.io/).

## Setup

Make sure you have Postgres running (locally or via docker) and update the username and password in Service.cs files to match. Then execute migrations with the following commands

```bash
dotnet ef database update --context InboxOutboxPattern.Service1.ServiceDbContext
dotnet ef database update --context InboxOutboxPattern.Service2.ServiceDbContext
```

As a side note: This were the commands to create the migrations. You don't need them, but I saved them here anyway

```bash
dotnet ef migrations add Initial --context InboxOutboxPattern.Service1.ServiceDbContext --output-dir Migrations/Service1
dotnet ef migrations add Initial --context InboxOutboxPattern.Service2.ServiceDbContext --output-dir Migrations/Service2
```

## Running

I suggest to start the project in Visual Studio Code because to learn about the details a debugger is helpful.