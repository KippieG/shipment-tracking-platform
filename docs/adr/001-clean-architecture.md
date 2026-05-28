# ADR-001: Clean Architecture

**Status:** Geaccepteerd  
**Datum:** 2025-01-01  
**Auteur:** Development Team

## Context
We bouwen een logistiek platform dat in de loop der jaren zal groeien. We moeten kunnen wisselen van database, externe services vervangen, en nieuwe features toevoegen zonder bestaande functionaliteit te breken.

## Beslissing
We passen Clean Architecture toe met vier lagen:
- **Domain** — entiteiten, value objects, domein-logica. Geen externe dependencies.
- **Application** — use cases via CQRS/MediatR. Afhankelijk van Domain via interfaces.
- **Infrastructure** — concrete implementaties (EF Core, Azure, Redis). Afhankelijk van Application-interfaces.
- **WebAPI** — HTTP-laag. Afhankelijk van Application en Infrastructure.

## Gevolgen
✅ Domein en business-logica zijn volledig testbaar zonder database of HTTP.  
✅ Infrastructure kan worden vervangen (SQL → NoSQL) zonder Application aan te raken.  
✅ Architectuurtests (NetArchTest) controleren automatisch of regels worden gevolgd.  
⚠️ Meer initiële boilerplate dan een monolithische aanpak.
