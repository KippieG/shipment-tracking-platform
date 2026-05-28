#!/bin/bash
# Nieuwe migratie aanmaken
# Gebruik: ./scripts/add-migration.sh "MigratieNaam"
set -e

if [ -z "$1" ]; then
  echo "Gebruik: $0 <MigratieNaam>"
  exit 1
fi

dotnet ef migrations add "$1" \
  --project src/Infrastructure \
  --startup-project src/WebAPI \
  --output-dir Persistence/Migrations

echo "Migratie '$1' aangemaakt."
