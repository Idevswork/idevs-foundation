#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 GitFlow Release Process${NC}"
echo "================================"

# Get current branch
CURRENT_BRANCH=$(git branch --show-current)
echo -e "Current branch: ${YELLOW}$CURRENT_BRANCH${NC}"

# Ensure we're on develop branch
if [ "$CURRENT_BRANCH" != "develop" ]; then
    echo -e "${YELLOW}⚠️  Switching to develop branch...${NC}"
    git checkout develop
fi

# Pull latest develop
echo -e "${BLUE}📥 Pulling latest develop...${NC}"
git pull origin develop

# Check for uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    echo -e "${RED}❌ Error: You have uncommitted changes!${NC}"
    echo "Please commit or stash your changes before releasing."
    exit 1
fi

# Get version for release message
VERSION="${1:-$(date +v%Y.%m.%d)}"
echo -e "${BLUE}📦 Preparing release: ${GREEN}$VERSION${NC}"

# Switch to main and pull latest
echo -e "${BLUE}🔄 Switching to main and pulling latest...${NC}"
git checkout main
git pull origin main

# Merge develop to main with no-ff
echo -e "${BLUE}🔀 Merging develop to main...${NC}"
git merge develop --no-ff -m "Release $VERSION

$(git log main..develop --oneline --format="- %s")"

# Push main
echo -e "${BLUE}📤 Pushing main...${NC}"
git push origin main

# Switch back to develop and sync with main
echo -e "${BLUE}🔄 Syncing develop with main...${NC}"
git checkout develop
git merge main --ff-only
git push origin develop

echo ""
echo -e "${GREEN}✅ Release $VERSION completed successfully!${NC}"
echo -e "${GREEN}📦 NuGet packages will be published automatically${NC}"
echo -e "${GREEN}🎯 Both main and develop are now synchronized${NC}"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "1. Check GitHub Actions for package publishing status"
echo "2. Monitor NuGet.org for your published packages"
echo "3. Continue development on develop branch"

# Check if release workflow is running
echo ""
echo -e "${BLUE}🔍 Checking GitHub Actions status...${NC}"
if command -v gh &> /dev/null; then
    gh run list --workflow="CI/CD Pipeline" --limit 3
else
    echo -e "${YELLOW}💡 Install 'gh' CLI to see workflow status: brew install gh${NC}"
fi
