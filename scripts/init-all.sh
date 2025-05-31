#!/usr/bin/env bash

echo "🚀 Starting full environment bootstrap..."

./init-dotnet-tools.sh
./init-terraform.sh
./init-kubectl.sh

echo "🎉 All systems initialized."
