#!/usr/bin/env bash

echo "🛠️ Installing local .NET tools..."

# Ensure dotnet-tools manifest exists
if [ ! -f "./.config/dotnet-tools.json" ]; then
    dotnet new tool-manifest
fi

# Install commonly used tools
dotnet tool install dotnet-ef
dotnet tool install dotnet-outdated-tool
dotnet tool install dotnet-format

# Restore tools (if someone cloned the repo)
dotnet tool restore

echo "✅ .NET tools are installed and ready."
