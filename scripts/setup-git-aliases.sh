#!/bin/bash

# Setup Git aliases for GitFlow workflow
echo "Setting up Git aliases for GitFlow workflow..."

# Feature branch aliases
git config alias.feature-start '!f() { git checkout develop && git pull origin develop && git checkout -b "feature/$1" && git push -u origin "feature/$1"; }; f'
git config alias.feature-finish '!f() { git checkout "feature/$1" && git push origin "feature/$1" && echo "Create PR: gh pr create --base develop --head feature/$1 --title \"feat: $1\""; }; f'

# Release branch aliases  
git config alias.release-start '!f() { git checkout develop && git pull origin develop && git checkout -b "release/$1" && git push -u origin "release/$1"; }; f'
git config alias.release-finish '!f() { git checkout main && git pull origin main && git merge --no-ff "release/$1" -m "release: version $1" && git tag -a "v$1" -m "Release version $1" && git push origin main --tags && git checkout develop && git pull origin develop && git merge --no-ff "release/$1" -m "chore: merge release $1 back to develop" && git push origin develop && git branch -d "release/$1" && git push origin --delete "release/$1"; }; f'

# RC Sprint branch aliases
git config alias.rc-sprint-start '!f() { git checkout "release/$1" && git pull origin "release/$1" && git checkout -b "rc-sprint/$1-$2" && git push -u origin "rc-sprint/$1-$2"; }; f'
git config alias.rc-sprint-finish '!f() { git checkout "rc-sprint/$1-$2" && git push origin "rc-sprint/$1-$2" && git checkout "release/$1" && git pull origin "release/$1" && git merge --no-ff "rc-sprint/$1-$2" -m "feat: integrate RC Sprint $2 changes for version $1" && git push origin "release/$1" && git branch -d "rc-sprint/$1-$2" && git push origin --delete "rc-sprint/$1-$2"; }; f'

# Hotfix branch aliases
git config alias.hotfix-start '!f() { git checkout main && git pull origin main && git checkout -b "hotfix/$1" && git push -u origin "hotfix/$1"; }; f'
git config alias.hotfix-finish '!f() { git checkout main && git pull origin main && git merge --no-ff "hotfix/$1" -m "fix: hotfix $1" && git tag -a "v$1" -m "Hotfix version $1" && git push origin main --tags && git checkout develop && git pull origin develop && git merge --no-ff "hotfix/$1" -m "chore: merge hotfix $1 back to develop" && git push origin develop && git branch -d "hotfix/$1" && git push origin --delete "hotfix/$1"; }; f'

# Utility aliases
git config alias.sync '!f() { git fetch origin && git checkout main && git pull origin main && git checkout develop && git pull origin develop; }; f'
git config alias.sync-develop '!f() { git checkout develop && git pull origin main && git push origin develop; }; f'
git config alias.status-all '!f() { echo "=== Git Status ==="; git status -s; echo ""; echo "=== Branches ==="; git branch -a; echo ""; echo "=== Recent Commits ==="; git log --oneline -5; }; f'

echo "Git aliases configured successfully!"
echo ""
echo "Usage examples:"
echo "  git feature-start user-authentication"
echo "  git feature-finish user-authentication"
echo "  git release-start 1.2.0"
echo "  git rc-sprint-start 1.2.0 rc1"
echo "  git rc-sprint-finish 1.2.0 rc1"
echo "  git release-finish 1.2.0"
echo "  git hotfix-start 1.2.1"
echo "  git hotfix-finish 1.2.1"
echo "  git sync"
echo "  git sync-develop"
echo "  git status-all"
