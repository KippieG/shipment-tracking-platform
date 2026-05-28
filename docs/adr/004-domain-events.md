# ADR-004: Domain Events

**Status:** Geaccepteerd

## Beslissing
Entiteiten verzamelen domein-events in een interne lijst (`_domainEvents`). Na het opslaan van wijzigingen worden deze events gepubliceerd via een `IDomainEventDispatcher`. Events worden ook naar Azure Service Bus gepubliceerd voor externe integraties.

## Gevolgen
✅ Losse koppeling tussen domein-logica en side effects (e-mail, Service Bus).  
✅ Events zijn testbaar zonder externe systemen.
