# Bijdragen aan Shipment Tracking Platform

## Ontwikkelomgeving

1. Vereisten: .NET 8 SDK, Docker, Git
2. Kloon de repo en start de database: `docker compose up -d sql`
3. Run migraties: `dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI`
4. Start de API: `dotnet run --project src/WebAPI`
5. Open Swagger: https://localhost:5001/swagger

## Git workflow

- `main` — productie, alleen via PR
- `develop` — integratiebranch
- `feature/beschrijving` — nieuwe features
- `fix/beschrijving` — bugfixes

## Commit conventie (Conventional Commits)

```
feat: nieuwe feature
fix: bugfix
docs: documentatie
refactor: code herstructurering zonder gedragswijziging
test: tests toevoegen of aanpassen
chore: buildsysteem, dependencies
```

## Code review checklist

- [ ] Tests aanwezig voor nieuwe functionaliteit
- [ ] Geen publieke setters op entiteiten
- [ ] Interfaces in Application, implementaties in Infrastructure
- [ ] FluentValidation op alle commands
- [ ] XML-comments op publieke controller methodes
- [ ] Architectuurtests passeren (`dotnet test tests/Architecture`)

## Teststandaarden

- **Unit tests**: xUnit + Moq + FluentAssertions, geen database
- **Integratietests**: WebApplicationFactory + in-memory database
- **Architectuurtests**: NetArchTest — controleer dependency-regels
