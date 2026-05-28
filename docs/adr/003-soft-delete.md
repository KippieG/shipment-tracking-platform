# ADR-003: Soft Delete voor zendingen

**Status:** Geaccepteerd

## Beslissing
Zendingen worden nooit fysiek verwijderd. Een `IsDeleted` boolean met een EF Core Global Query Filter zorgt ervoor dat verwijderde zendingen transparant worden gefilterd.

## Gevolgen
✅ Volledige audit trail voor logistieke compliance.  
✅ Herstel van per ongeluk verwijderde zendingen is mogelijk.  
⚠️ Database groeit — periodieke archivering nodig op lange termijn.
