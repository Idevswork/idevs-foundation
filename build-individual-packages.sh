#!/bin/bash

# Script to build individual Foundation packages
# Usage: ./build-individual-packages.sh [Release|Debug]

CONFIGURATION=${1:-Release}

echo "Building individual Foundation packages in $CONFIGURATION configuration..."

# Clean artifacts
rm -rf artifacts/*.nupkg

# Build all individual packages
dotnet build --configuration $CONFIGURATION -p:GenerateIndividualPackages=true

echo ""
echo "Individual packages generated:"
ls -la artifacts/*.nupkg
