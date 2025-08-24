#!/bin/bash

# GitFlow Helper Script for Idevs.Foundation
# This script helps manage GitFlow branches and follows semantic versioning

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in a git repository
check_git_repo() {
    if ! git rev-parse --git-dir > /dev/null 2>&1; then
        print_error "Not in a git repository"
        exit 1
    fi
}

# Ensure we have the latest changes
sync_branches() {
    print_status "Syncing with remote..."
    git fetch origin
    
    # Ensure main and develop exist locally
    if ! git show-ref --verify --quiet refs/heads/main; then
        git checkout -b main origin/main
    fi
    
    if ! git show-ref --verify --quiet refs/heads/develop; then
        git checkout -b develop origin/develop
    fi
}

# Start a new feature branch
start_feature() {
    if [ -z "$1" ]; then
        print_error "Please provide a feature name"
        echo "Usage: $0 feature start <feature-name>"
        exit 1
    fi
    
    local feature_name="feature/$1"
    sync_branches
    
    print_status "Starting feature branch: $feature_name"
    git checkout develop
    git pull origin develop
    git checkout -b "$feature_name"
    git push -u origin "$feature_name"
    
    print_success "Feature branch '$feature_name' created and pushed to remote"
    print_status "You can now start working on your feature"
}

# Finish a feature branch
finish_feature() {
    if [ -z "$1" ]; then
        print_error "Please provide a feature name"
        echo "Usage: $0 feature finish <feature-name>"
        exit 1
    fi
    
    local feature_name="feature/$1"
    sync_branches
    
    print_status "Finishing feature branch: $feature_name"
    
    # Switch to feature branch and push latest changes
    git checkout "$feature_name"
    git push origin "$feature_name"
    
    # Create PR or merge (you can modify this based on your workflow)
    print_status "Creating pull request..."
    print_warning "Please create a PR from '$feature_name' to 'develop' on GitHub"
    print_status "Or run: gh pr create --base develop --head $feature_name --title 'feat: $1' --body 'Feature: $1'"
}

# Start a release branch
start_release() {
    if [ -z "$1" ]; then
        print_error "Please provide a version number"
        echo "Usage: $0 release start <version> (e.g., 1.2.0)"
        exit 1
    fi
    
    local version="$1"
    local release_name="release/$version"
    sync_branches
    
    print_status "Starting release branch: $release_name"
    git checkout develop
    git pull origin develop
    git checkout -b "$release_name"
    git push -u origin "$release_name"
    
    print_success "Release branch '$release_name' created and pushed to remote"
    print_status "You can now prepare the release (update changelogs, version numbers, etc.)"
}

# Start a release candidate sprint branch
start_rc_sprint() {
    if [ -z "$1" ] || [ -z "$2" ]; then
        print_error "Please provide a version number and sprint identifier"
        echo "Usage: $0 rc-sprint start <version> <sprint> (e.g., 1.2.0 rc1)"
        exit 1
    fi
    
    local version="$1"
    local sprint="$2"
    local release_name="release/$version"
    local rc_sprint_name="rc-sprint/$version-$sprint"
    sync_branches
    
    # Check if release branch exists
    if ! git show-ref --verify --quiet refs/heads/$release_name; then
        print_error "Release branch '$release_name' does not exist. Please start the release first."
        echo "Run: $0 release start $version"
        exit 1
    fi
    
    print_status "Starting release candidate sprint: $rc_sprint_name from $release_name"
    git checkout "$release_name"
    git pull origin "$release_name"
    git checkout -b "$rc_sprint_name"
    git push -u origin "$rc_sprint_name"
    
    print_success "RC Sprint branch '$rc_sprint_name' created and pushed to remote"
    print_status "You can now work on release candidate fixes and improvements"
    print_status "When ready, use 'finish' to merge back to the release branch"
}

# Finish a release candidate sprint branch
finish_rc_sprint() {
    if [ -z "$1" ] || [ -z "$2" ]; then
        print_error "Please provide a version number and sprint identifier"
        echo "Usage: $0 rc-sprint finish <version> <sprint>"
        exit 1
    fi
    
    local version="$1"
    local sprint="$2"
    local release_name="release/$version"
    local rc_sprint_name="rc-sprint/$version-$sprint"
    sync_branches
    
    print_status "Finishing RC Sprint: $rc_sprint_name"
    
    # Switch to rc-sprint branch and push latest changes
    git checkout "$rc_sprint_name"
    git push origin "$rc_sprint_name"
    
    # Merge back to release branch
    git checkout "$release_name"
    git pull origin "$release_name"
    git merge --no-ff "$rc_sprint_name" -m "feat: integrate RC Sprint $sprint changes for version $version"
    git push origin "$release_name"
    
    # Delete rc-sprint branch
    git branch -d "$rc_sprint_name"
    git push origin --delete "$rc_sprint_name"
    
    print_success "RC Sprint $sprint for version $version completed and merged to release branch"
    print_status "Release branch '$release_name' is now updated with RC Sprint changes"
    print_status "You can start another RC Sprint or finish the release when ready"
}

# Finish a release branch
finish_release() {
    if [ -z "$1" ]; then
        print_error "Please provide a version number"
        echo "Usage: $0 release finish <version>"
        exit 1
    fi
    
    local version="$1"
    local release_name="release/$version"
    sync_branches
    
    print_status "Finishing release: $release_name"
    
    # Merge to main
    git checkout main
    git pull origin main
    git merge --no-ff "$release_name" -m "release: version $version"
    git tag -a "v$version" -m "Release version $version"
    git push origin main --tags
    
    # Merge back to develop
    git checkout develop
    git pull origin develop
    git merge --no-ff "$release_name" -m "chore: merge release $version back to develop"
    git push origin develop
    
    # Delete release branch
    git branch -d "$release_name"
    git push origin --delete "$release_name"
    
    print_success "Release $version completed and tagged"
    print_status "Create a GitHub release at: https://github.com/$(git remote get-url origin | sed 's/.*github.com[:/]\([^.]*\).*/\1/')/releases/new?tag=v$version"
}

# Start a hotfix branch
start_hotfix() {
    if [ -z "$1" ]; then
        print_error "Please provide a version number"
        echo "Usage: $0 hotfix start <version> (e.g., 1.2.1)"
        exit 1
    fi
    
    local version="$1"
    local hotfix_name="hotfix/$version"
    sync_branches
    
    print_status "Starting hotfix branch: $hotfix_name"
    git checkout main
    git pull origin main
    git checkout -b "$hotfix_name"
    git push -u origin "$hotfix_name"
    
    print_success "Hotfix branch '$hotfix_name' created and pushed to remote"
    print_status "You can now fix the critical issue"
}

# Finish a hotfix branch
finish_hotfix() {
    if [ -z "$1" ]; then
        print_error "Please provide a version number"
        echo "Usage: $0 hotfix finish <version>"
        exit 1
    fi
    
    local version="$1"
    local hotfix_name="hotfix/$version"
    sync_branches
    
    print_status "Finishing hotfix: $hotfix_name"
    
    # Merge to main
    git checkout main
    git pull origin main
    git merge --no-ff "$hotfix_name" -m "fix: hotfix $version"
    git tag -a "v$version" -m "Hotfix version $version"
    git push origin main --tags
    
    # Merge back to develop
    git checkout develop
    git pull origin develop
    git merge --no-ff "$hotfix_name" -m "chore: merge hotfix $version back to develop"
    git push origin develop
    
    # Delete hotfix branch
    git branch -d "$hotfix_name"
    git push origin --delete "$hotfix_name"
    
    print_success "Hotfix $version completed and tagged"
    print_status "Create a GitHub release at: https://github.com/$(git remote get-url origin | sed 's/.*github.com[:/]\([^.]*\).*/\1/')/releases/new?tag=v$version"
}

# Show current status
show_status() {
    print_status "Current Git Status:"
    echo ""
    git status --short --branch
    echo ""
    
    print_status "Available branches:"
    git branch -a
    echo ""
    
    print_status "Recent commits:"
    git log --oneline -5
}

# Show help
show_help() {
    echo "GitFlow Helper Script for Idevs.Foundation"
    echo ""
    echo "Usage: $0 <command> [options]"
    echo ""
    echo "Commands:"
    echo "  feature start <name>        Start a new feature branch"
    echo "  feature finish <name>       Finish a feature branch (creates PR)"
    echo "  release start <version>     Start a new release branch"
    echo "  release finish <version>    Finish a release branch and create tag"
    echo "  rc-sprint start <ver> <id>  Start a release candidate sprint branch"
    echo "  rc-sprint finish <ver> <id> Finish a release candidate sprint branch"
    echo "  hotfix start <version>      Start a new hotfix branch"
    echo "  hotfix finish <version>     Finish a hotfix branch and create tag"
    echo "  status                      Show current git status and branches"
    echo "  help                        Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 feature start user-authentication"
    echo "  $0 feature finish user-authentication"
    echo "  $0 release start 1.2.0"
    echo "  $0 rc-sprint start 1.2.0 rc1"
    echo "  $0 rc-sprint finish 1.2.0 rc1"
    echo "  $0 release finish 1.2.0"
    echo "  $0 hotfix start 1.2.1"
    echo "  $0 hotfix finish 1.2.1"
    echo ""
    echo "Semantic Versioning in Commits:"
    echo "  feat: new feature (+semver: minor)"
    echo "  fix: bug fix (+semver: patch)"
    echo "  feat!: breaking change (+semver: major)"
    echo "  docs: documentation (+semver: none)"
}

# Main script logic
check_git_repo

case "$1" in
    "feature")
        case "$2" in
            "start") start_feature "$3" ;;
            "finish") finish_feature "$3" ;;
            *) echo "Usage: $0 feature [start|finish] <name>"; exit 1 ;;
        esac
        ;;
    "release")
        case "$2" in
            "start") start_release "$3" ;;
            "finish") finish_release "$3" ;;
            *) echo "Usage: $0 release [start|finish] <version>"; exit 1 ;;
        esac
        ;;
    "rc-sprint")
        case "$2" in
            "start") start_rc_sprint "$3" "$4" ;;
            "finish") finish_rc_sprint "$3" "$4" ;;
            *) echo "Usage: $0 rc-sprint [start|finish] <version> <sprint>"; exit 1 ;;
        esac
        ;;
    "hotfix")
        case "$2" in
            "start") start_hotfix "$3" ;;
            "finish") finish_hotfix "$3" ;;
            *) echo "Usage: $0 hotfix [start|finish] <version>"; exit 1 ;;
        esac
        ;;
    "status") show_status ;;
    "help"|"-h"|"--help") show_help ;;
    *) show_help; exit 1 ;;
esac
