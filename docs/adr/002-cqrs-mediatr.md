# ADR-002: CQRS via MediatR

**Status:** Geaccepteerd

## Beslissing
Elke use case is een aparte Command of Query class, verstuurd via MediatR. Pipeline behaviours verzorgen cross-cutting concerns (logging, validatie).

## Gevolgen
✅ Elke use case is onafhankelijk testbaar.  
✅ Nieuwe features toevoegen = nieuwe Command/Query klasse, geen wijzigingen aan bestaande code.  
✅ Logging en validatie zijn transparant voor handlers.
