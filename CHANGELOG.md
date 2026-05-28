# Changelog

Alle wijzigingen worden in dit bestand bijgehouden.
Formaat gebaseerd op [Keep a Changelog](https://keepachangelog.com/nl/1.0.0/).

## [Unreleased]

### Toegevoegd
- SignalR voor real-time statusupdates
- Background worker voor Service Bus inbound events
- .NET MAUI offline-first synchronisatie
- Rate limiting dashboard via health endpoint

## [1.0.0] - 2025-01-01

### Toegevoegd
- Clean Architecture structuur (Domain, Application, Infrastructure, WebAPI)
- CQRS via MediatR met logging en validatie pipeline behaviours
- Entity Framework Core 8 met Azure SQL en migraties
- Shipment aggregate root met statusmachine en domein-events
- Azure Blob Storage voor documentbeheer
- Azure Service Bus voor event-driven integraties
- JWT authenticatie met rol-gebaseerde autorisatie
- Serilog met file rotation en correlatie-ID middleware
- Rate limiting (100 req/min globaal, 10/min voor auth endpoints)
- Redis cache integratie (in-memory fallback voor dev)
- Health check endpoints (/health/live, /health/ready)
- Achtergrondservice voor verlopen zendingen
- SendGrid e-mail integraties
- xUnit unit- en integratietests
- NetArchTest architectuurtests
- GitHub Actions CI/CD pipeline met Docker build en push
- Kubernetes deployment manifesten
- Data seeder voor ontwikkelomgeving
- Volledige Swagger/OpenAPI documentatie
