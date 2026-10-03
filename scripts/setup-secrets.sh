#!/usr/bin/env bash
# Copies .env into the MoviesAdmin user secrets store (~/.microsoft/usersecrets/<UserSecretsId>/),
# so the app and `dotnet ef` read the connection string, seed admin, and API keys from there rather
# than from appsettings*.json. The connection string is built from the same MSSQL_PORT and
# MSSQL_SA_PASSWORD that docker-compose.yml uses, so the two can't drift apart.
#
# Usage (from the repository root): ./scripts/setup-secrets.sh
# Re-run it whenever .env changes.
set -euo pipefail

cd "$(dirname "$0")/.."

if [[ ! -f .env ]]; then
  echo "No .env found. Copy .env.example to .env and fill it in first." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1091
source .env
set +a

: "${MSSQL_PORT:?MSSQL_PORT is not set in .env}"
: "${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is not set in .env}"
: "${SEED_ADMIN_EMAIL:?SEED_ADMIN_EMAIL is not set in .env}"
: "${SEED_ADMIN_PASSWORD:?SEED_ADMIN_PASSWORD is not set in .env}"

project=MoviesAdmin
secret() { dotnet user-secrets set "$1" "$2" --project "$project" >/dev/null; echo "  set $1"; }

echo "Writing user secrets for $project:"
secret "ConnectionStrings:DefaultConnection" \
  "Server=localhost,${MSSQL_PORT};Database=MoviesAdmin;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;MultipleActiveResultSets=true"
secret "SeedUsers:0:Email" "$SEED_ADMIN_EMAIL"
secret "SeedUsers:0:Password" "$SEED_ADMIN_PASSWORD"
secret "SeedUsers:0:DisplayName" "${SEED_ADMIN_DISPLAY_NAME:-Administrator}"
secret "SeedUsers:0:Role" "Admin"

if [[ -n "${TMDB_API_KEY:-}" ]]; then secret "MovieLookup:TmdbApiKey" "$TMDB_API_KEY"; fi
if [[ -n "${OMDB_API_KEY:-}" ]]; then secret "MovieLookup:OmdbApiKey" "$OMDB_API_KEY"; fi

echo "Done. List them with: dotnet user-secrets list --project $project"
