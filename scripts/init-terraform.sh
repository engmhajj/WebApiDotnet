#!/usr/bin/env bash

# Initializes Terraform with optional backend config
echo "🔧 Initializing Terraform..."

TF_DIR="./infrastructure" # change if needed

cd "$TF_DIR" || exit 1

terraform init \
    -input=true \
    -upgrade

terraform validate

echo "✅ Terraform initialized."
