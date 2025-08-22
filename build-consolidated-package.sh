#!/bin/bash

# Script to build the consolidated Foundation package
# Usage: ./build-consolidated-package.sh [Release|Debug]

CONFIGURATION=${1:-Release}

echo "Building consolidated Foundation package in $CONFIGURATION configuration..."

# Clean artifacts
rm -rf artifacts/*.nupkg

# Build the consolidated package
dotnet build src/IdevsWork.Foundation/IdevsWork.Foundation.csproj --configuration $CONFIGURATION

echo ""
echo "Consolidated package generated:"
ls -la artifacts/*.nupkg

if [ -f artifacts/IdevsWork.Foundation.*.nupkg ]; then
    echo ""
    echo "Package contents:"
    unzip -l artifacts/IdevsWork.Foundation.*.nupkg | grep "\.dll"
fi
