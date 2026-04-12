set -e

PROJECT="./VideoAnonymizer.AppHost"

echo "Initializing user secrets..."

dotnet user-secrets init --project "$PROJECT"

generate_password() {
  length=${1:-24}
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 32 | tr -dc 'A-Za-z0-9!@#$%^&*()' | head -c "$length"
  else
    tr -dc 'A-Za-z0-9!@#$%^&*()' </dev/urandom | head -c "$length"
  fi
}

RABBIT_USER="rabbituser"
RABBIT_PASSWORD=$(generate_password)
POSTGRES_PASSWORD=$(generate_password)

echo "Setting development secrets..."

dotnet user-secrets set "Parameters:rabbit-user" "$RABBIT_USER" --project "$PROJECT"
dotnet user-secrets set "Parameters:rabbit-password" "$RABBIT_PASSWORD" --project "$PROJECT"
dotnet user-secrets set "Parameters:postgres-password" "$POSTGRES_PASSWORD" --project "$PROJECT"

echo "Development secrets configured successfully!"