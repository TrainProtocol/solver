#!/bin/sh
set -e  # Exit immediately if a command exits with non-zero status

# Env vars
VAULT_ADDR="http://127.0.0.1:$HASHICORP_KEYVAULT_PORT"
UNSEAL_THRESHOLD=3
KEYS_DIR="/vault/keys"
ENV_DIR="/vault/env"
READONLY_POLICY="train-readonly"

# Ensure directories exist
mkdir -p "$KEYS_DIR" "$ENV_DIR"

# Start Vault server in background
echo "Starting Vault server..."
vault server -config=/vault/config/local.json &
VAULT_PID=$!

# Wait for Vault to start to execute init and unseal on the first run only
sleep 10

# Export Vault address for CLI commands
export VAULT_ADDR

# Check if Vault is already unsealed
if vault status 2>/dev/null | grep -q 'Initialized.*true'; then
  echo "Vault is already initialized."
else
  # Install dependencies
  echo "Installing dependencies..."
  apk add --no-cache jq

  # Initialize Vault
  echo "Initializing Vault..."
  vault operator init -format=json | tee "$KEYS_DIR/vault_init_full.json" | \
    jq -r '. | "ROOT_TOKEN=\(.root_token)"' > "$ENV_DIR/.env"

  # Source the env file to get the root token
  . "$ENV_DIR/.env"
  export VAULT_TOKEN="$ROOT_TOKEN"

  # Unseal vault with threshold number of keys
  echo "Unsealing Vault with $UNSEAL_THRESHOLD keys..."
  for i in $(seq 0 $(($UNSEAL_THRESHOLD-1))); do
    KEY=$(jq -r ".unseal_keys_hex[$i]" "$KEYS_DIR/vault_init_full.json")
    vault operator unseal "$KEY"
  done

  # Verify vault is unsealed
  vault status | grep "Sealed" | grep "false" || {
    echo "Error: Failed to unseal Vault."
    exit 1
  }

  # Setup auth and secrets
  echo "Setting up auth methods and secret engines..."
  vault auth enable userpass || echo "Userpass already enabled"
  vault secrets enable -path=secret kv || echo "KV secrets engine already enabled"

  # Create readonly policy
  echo "Creating policies..."
  vault policy write "$READONLY_POLICY" - <<EOF
path "secret/data/*" {
  capabilities = ["read", "list"]
}
path "secret/metadata/*" {
  capabilities = ["list"]
}
EOF

  # Create users
  echo "Creating users..."
  vault write auth/userpass/users/$HASHICORP_KEYVAULT_USER password="$HASHICORP_KEYVAULT_PASSWORD" policies="$READONLY_POLICY"
  echo "Vault initialization complete!"
fi

# Wait for the Vault process
wait $VAULT_PID