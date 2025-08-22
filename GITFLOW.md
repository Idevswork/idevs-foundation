# GitFlow Quick Reference

This document provides quick commands for managing branches in the IdevsWork.Foundation repository using GitFlow methodology.

## Branch Structure

- **`main`** - Production-ready releases (publishes to NuGet.org)
- **`develop`** - Development integration branch (publishes preview packages)
- **`feature/*`** - Feature development branches
- **`release/*`** - Release preparation branches  
- **`hotfix/*`** - Emergency fixes

## Quick Commands (Using Git Aliases)

### Feature Development

```bash
# Start a new feature
git feature-start user-authentication

# Finish feature (pushes and suggests PR command)
git feature-finish user-authentication

# Create PR using GitHub CLI
gh pr create --base develop --head feature/user-authentication --title "feat: user authentication"
```

### Release Management

```bash
# Start a release
git release-start 1.2.0

# Finish release (merges, tags, and pushes)
git release-finish 1.2.0

# Create GitHub release
gh release create v1.2.0 --generate-notes
```

### Hotfixes

```bash
# Start a hotfix
git hotfix-start 1.2.1

# Finish hotfix (merges, tags, and pushes)
git hotfix-finish 1.2.1
```

### Utility Commands

```bash
# Sync all branches with remote
git sync

# Show comprehensive status
git status-all
```

## Manual Commands (If You Prefer Full Control)

### Feature Branches

```bash
# Start feature
git checkout develop
git pull origin develop  
git checkout -b feature/my-feature
git push -u origin feature/my-feature

# Finish feature  
git checkout feature/my-feature
git push origin feature/my-feature
# Create PR on GitHub: feature/my-feature → develop
```

### Release Branches

```bash
# Start release
git checkout develop
git pull origin develop
git checkout -b release/1.2.0
git push -u origin release/1.2.0

# Finish release
git checkout main
git pull origin main
git merge --no-ff release/1.2.0 -m "release: version 1.2.0"
git tag -a v1.2.0 -m "Release version 1.2.0"
git push origin main --tags

git checkout develop  
git pull origin develop
git merge --no-ff release/1.2.0 -m "chore: merge release 1.2.0 back to develop"
git push origin develop

git branch -d release/1.2.0
git push origin --delete release/1.2.0
```

### Hotfix Branches

```bash
# Start hotfix
git checkout main
git pull origin main
git checkout -b hotfix/1.2.1
git push -u origin hotfix/1.2.1

# Finish hotfix
git checkout main
git pull origin main
git merge --no-ff hotfix/1.2.1 -m "fix: hotfix 1.2.1"
git tag -a v1.2.1 -m "Hotfix version 1.2.1"
git push origin main --tags

git checkout develop
git pull origin develop  
git merge --no-ff hotfix/1.2.1 -m "chore: merge hotfix 1.2.1 back to develop"
git push origin develop

git branch -d hotfix/1.2.1
git push origin --delete hotfix/1.2.1
```

## Advanced Script Usage

For more advanced operations, use the GitFlow helper script:

```bash
# Using the script
./scripts/gitflow.sh feature start user-authentication
./scripts/gitflow.sh feature finish user-authentication
./scripts/gitflow.sh release start 1.2.0
./scripts/gitflow.sh release finish 1.2.0
./scripts/gitflow.sh hotfix start 1.2.1
./scripts/gitflow.sh hotfix finish 1.2.1
./scripts/gitflow.sh status
```

## Semantic Versioning in Commits

Use conventional commit messages with semantic versioning hints:

```bash
git commit -m "feat: add user authentication +semver: minor"
git commit -m "fix: resolve login issue +semver: patch"  
git commit -m "feat!: breaking API changes +semver: major"
git commit -m "docs: update README +semver: none"
```

## CI/CD Integration

The CI/CD pipeline will automatically:

- **Feature branches**: Run tests and build validation
- **Develop branch**: Publish preview packages with `-alpha` suffix
- **Main branch**: Publish release packages to NuGet.org
- **Release tags**: Create GitHub releases with attached packages

## Typical Workflow Example

1. **Start a feature:**
   ```bash
   git feature-start user-authentication
   ```

2. **Develop your feature** (make commits with semantic versioning):
   ```bash
   git commit -m "feat: add login endpoint +semver: minor"
   git commit -m "test: add authentication tests +semver: none"
   git commit -m "fix: handle edge case in login +semver: patch"
   ```

3. **Finish the feature:**
   ```bash
   git feature-finish user-authentication
   gh pr create --base develop --head feature/user-authentication --title "feat: user authentication"
   ```

4. **After PR is merged, prepare a release:**
   ```bash
   git release-start 1.2.0
   # Update CHANGELOG.md, version numbers, etc.
   git commit -m "chore: prepare release 1.2.0 +semver: none"
   git release-finish 1.2.0
   ```

5. **Create GitHub release:**
   ```bash
   gh release create v1.2.0 --generate-notes
   ```

## Tips

- Always sync before starting new branches: `git sync`
- Use descriptive branch names: `feature/user-authentication` not `feature/auth`
- Follow semantic versioning in commit messages
- Test locally before pushing: `dotnet build && dotnet test`
- Use the PR template for consistent documentation

## Troubleshooting

- **Branch conflicts**: Use `git sync` to get latest changes
- **Failed merges**: Resolve conflicts manually and complete the merge
- **Missing branches**: Ensure `develop` branch exists: `git checkout -b develop origin/develop`
- **Permission issues**: Ensure you have push access to the repository

For more help: `./scripts/gitflow.sh help`
