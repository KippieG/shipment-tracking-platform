#!/bin/bash
# Voer EF Core migraties uit
set -e

echo "Migraties uitvoeren..."
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/WebAPI \
  --verbose

echo "Klaar."
