# GitFlow Workflow Guide for Idevs.Foundation

A practical guide for using GitFlow branching strategy in the Idevs.Foundation repository with complete workflows and examples.

## Quick Start

```bash
# Initial setup (run once)
./scripts/setup-git-aliases.sh

# Daily workflow
git sync                                    # Get latest changes
git feature-start my-new-feature           # Start feature
# ... develop your feature ...
git add . && git commit -m "feat: add new feature +semver: minor"
git push origin feature/my-new-feature     # Push changes
git feature-finish my-new-feature          # Finish feature
gh pr create --base develop --title "feat: add new feature"  # Create PR
```

## Table of Contents

- [Branch Structure](#branch-structure)
- [Feature Development](#feature-development)
- [Release Management](#release-management)
- [Hotfixes](#hotfixes)
- [Commands Reference](#commands-reference)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Branch Structure

### Main Branches (Permanent)

| Branch | Purpose | Deployment | Protection |
|--------|---------|------------|------------|
| `main` | Production-ready code | Stable NuGet packages | Protected, no direct commits |
| `develop` | Integration branch | Preview packages (-alpha) | Protected, accepts feature PRs |

### Supporting Branches (Temporary)

| Branch Type | Pattern | Purpose | Created From | Merged To |
|-------------|---------|---------|--------------|-----------|
| Feature | `feature/*` | New features/enhancements | `develop` | `develop` |
| Release | `release/*` | Prepare production release | `develop` | `main` + `develop` |
| Hotfix | `hotfix/*` | Emergency production fixes | `main` | `main` + `develop` |

## Feature Development

### Complete Feature Workflow

#### 1. Start a Feature

```bash
# Ensure you have latest changes
git sync

# Start new feature (creates branch and pushes to remote)
git feature-start user-authentication

# Alternative manual approach
git checkout develop
git pull origin develop
git checkout -b feature/user-authentication
git push -u origin feature/user-authentication
```

#### 2. Develop Your Feature

```bash
# Make changes to your code
# Add new files, modify existing ones

# Stage and commit changes with semantic versioning
git add src/Authentication/
git commit -m "feat: add user authentication service +semver: minor"

# Add tests
git add tests/Authentication/
git commit -m "test: add authentication tests +semver: none"

# Add documentation
git add docs/authentication.md
git commit -m "docs: add authentication documentation +semver: none"

# Push changes regularly
git push origin feature/user-authentication
```

#### 3. Keep Feature Updated

```bash
# Periodically sync with develop to avoid conflicts
git checkout develop
git pull origin develop
git checkout feature/user-authentication
git merge develop
# Resolve any conflicts if needed
git push origin feature/user-authentication
```

#### 4. Finish Feature & Create PR

```bash
# Final testing
dotnet build --configuration Release
dotnet test --configuration Release

# Push final changes
git push origin feature/user-authentication

# Finish feature (merges latest develop)
git feature-finish user-authentication

# Create Pull Request
gh pr create \
  --base develop \
  --head feature/user-authentication \
  --title "feat: add user authentication service" \
  --body "Implements user authentication with JWT tokens.

## Changes
- Added AuthenticationService class
- Implemented JWT token generation
- Added comprehensive unit tests
- Updated documentation

## Testing
- All existing tests pass
- New tests added with 90% coverage
- Manual testing completed

Closes #123"
```

#### 5. After PR is Merged

```bash
# Clean up local branch
git checkout develop
git pull origin develop
git branch -d feature/user-authentication

# Prune remote tracking branches
git remote prune origin
```

## Release Management

### Complete Release Workflow

#### 1. Start Release

```bash
# Sync and start release branch
git sync
git release-start 1.3.0

# Alternative manual approach
git checkout develop
git pull origin develop
git checkout -b release/1.3.0
git push -u origin release/1.3.0
```

#### 2. Prepare Release

```bash
# Update version in project files
# Edit Directory.Build.props
vim Directory.Build.props  # Update <VersionPrefix>1.3.0</VersionPrefix>

# Update changelog
vim CHANGELOG.md  # Add new section for v1.3.0

# Commit preparation changes
git add Directory.Build.props CHANGELOG.md
git commit -m "chore: prepare release 1.3.0 +semver: none"
git push origin release/1.3.0
```

#### 3. Test Release

```bash
# Build and test in release mode
dotnet build --configuration Release
dotnet test --configuration Release

# Build packages
./build-consolidated-package.sh Release
./build-individual-packages.sh Release

# Verify packages
ls -la artifacts/
```

#### 4. Fix Release Issues (if needed)

```bash
# If bugs found during release testing
git checkout release/1.3.0
# Make fixes...
git add .
git commit -m "fix: resolve issue in release testing +semver: patch"
git push origin release/1.3.0
# Continue testing until stable
```

#### 5. Complete Release

```bash
# Finish release (merges to main, tags, merges back to develop)
git release-finish 1.3.0

# This automatically does:
# - Merges release/1.3.0 to main
# - Creates tag v1.3.0
# - Merges release/1.3.0 back to develop
# - Deletes release branch
# - Pushes everything
```

#### 6. Create GitHub Release

```bash
# Create GitHub release with auto-generated notes
gh release create v1.3.0 \
  --title "Idevs.Foundation v1.3.0" \
  --generate-notes

# Or create draft for review first
gh release create v1.3.0 \
  --title "Idevs.Foundation v1.3.0" \
  --generate-notes \
  --draft
```

## Hotfixes

### Emergency Hotfix Workflow

#### 1. Start Hotfix (from main)

```bash
# Create hotfix branch from current production
git sync
git hotfix-start 1.2.1

# Manual approach
git checkout main
git pull origin main
git checkout -b hotfix/1.2.1
git push -u origin hotfix/1.2.1
```

#### 2. Implement Fix

```bash
# Make minimal changes to fix critical issue
git add src/ProblemArea/
git commit -m "fix: resolve critical null reference in authentication

Fixes production issue where null context causes app crash.
Added proper null checks and error handling.

Fixes #456
+semver: patch"

git push origin hotfix/1.2.1
```

#### 3. Test Hotfix

```bash
# Thorough testing
dotnet build --configuration Release
dotnet test --configuration Release

# Test specific scenario that was broken
# Regression testing
```

#### 4. Complete Hotfix

```bash
# Finish hotfix (merges to main and develop, creates tag)
git hotfix-finish 1.2.1

# Create GitHub release for hotfix
gh release create v1.2.1 \
  --title "Hotfix v1.2.1" \
  --notes "Critical bugfix for authentication issue"
```

## Commands Reference

### Git Aliases (Recommended)

Setup once:
```bash
./scripts/setup-git-aliases.sh
```

Available aliases:
```bash
git sync                           # Sync main and develop branches
git feature-start <name>           # Start feature branch
git feature-finish <name>          # Finish feature branch
git release-start <version>        # Start release branch  
git release-finish <version>       # Complete release
git hotfix-start <version>         # Start hotfix branch
git hotfix-finish <version>        # Complete hotfix
git status-all                     # Show comprehensive status
```

### Manual Commands

#### Feature Development
```bash
# Start
git checkout develop && git pull origin develop
git checkout -b feature/my-feature
git push -u origin feature/my-feature

# Finish
git checkout feature/my-feature
git push origin feature/my-feature
# Then create PR: feature/my-feature → develop
```

#### Release Process
```bash
# Start
git checkout develop && git pull origin develop
git checkout -b release/1.2.0
git push -u origin release/1.2.0

# Finish  
git checkout main && git pull origin main
git merge --no-ff release/1.2.0 -m "release: version 1.2.0"
git tag -a v1.2.0 -m "Release version 1.2.0"
git push origin main --tags

git checkout develop && git pull origin develop
git merge --no-ff release/1.2.0 -m "chore: merge release back to develop"
git push origin develop

git branch -d release/1.2.0
git push origin --delete release/1.2.0
```

#### Hotfix Process
```bash
# Start
git checkout main && git pull origin main
git checkout -b hotfix/1.2.1
git push -u origin hotfix/1.2.1

# Finish
git checkout main && git pull origin main
git merge --no-ff hotfix/1.2.1 -m "fix: hotfix 1.2.1"
git tag -a v1.2.1 -m "Hotfix version 1.2.1"
git push origin main --tags

git checkout develop && git pull origin develop
git merge --no-ff hotfix/1.2.1 -m "chore: merge hotfix back to develop"
git push origin develop

git branch -d hotfix/1.2.1
git push origin --delete hotfix/1.2.1
```

### GitHub CLI Commands

```bash
# Pull Requests
gh pr create --base develop --title "feat: my feature"
gh pr create --base develop --draft
gh pr list
gh pr view <number>
gh pr merge <number> --merge

# Releases
gh release create v1.2.0 --generate-notes
gh release create v1.2.0 --draft --generate-notes
gh release list
gh release view v1.2.0
```

## Best Practices

### Commit Messages

Use conventional commits with semantic versioning:

```bash
# Types and version impact
feat: new feature                    # +semver: minor
fix: bug fix                         # +semver: patch  
docs: documentation only             # +semver: none
style: formatting, no code change    # +semver: none
refactor: code refactoring           # +semver: patch
test: adding tests                   # +semver: none
chore: build process/tools           # +semver: none

# Examples
git commit -m "feat: add user authentication service +semver: minor"
git commit -m "fix: resolve null reference in login +semver: patch"
git commit -m "docs: update authentication guide +semver: none"

# Breaking changes
git commit -m "feat!: change authentication API signature +semver: major"
```

### Branch Naming

```bash
# Good names
feature/user-authentication
feature/json-query-support  
feature/issue-123-database-fix
release/1.2.0
hotfix/1.2.1-security-fix

# Bad names  
feature/auth                # Too abbreviated
feature/fix                 # Not descriptive
feature/temp-branch         # Temporary reference
```

### Version Semantics - One Feature = One Version Impact

**✅ Correct: Single version bump per feature**
```bash
git commit -m "feat: add caching service +semver: minor"          # Version impact
git commit -m "feat: implement Redis provider +semver: none"      # Same feature
git commit -m "feat: add DI integration +semver: none"            # Same feature  
git commit -m "test: add caching tests +semver: none"             # Same feature
# Result: One minor version increase
```

**❌ Wrong: Multiple bumps for same feature**
```bash
git commit -m "feat: add caching interfaces +semver: minor"       # Bump 1
git commit -m "feat: implement caching service +semver: minor"    # Bump 2 (Wrong!)
git commit -m "feat: add Redis support +semver: minor"           # Bump 3 (Wrong!)
# Result: Three unnecessary version bumps
```

### Development Workflow

```bash
# 1. Always start from develop
git sync
git feature-start my-feature

# 2. Commit frequently with clear messages  
git add . && git commit -m "feat: implement core feature +semver: minor"
git push origin feature/my-feature

# 3. Keep updated with develop
git checkout develop && git pull origin develop
git checkout feature/my-feature && git merge develop
git push origin feature/my-feature

# 4. Test before finishing
dotnet build --configuration Release
dotnet test --configuration Release

# 5. Complete workflow
git feature-finish my-feature
gh pr create --base develop --title "feat: my feature"
```

### Testing Requirements

- Unit tests for all new functionality
- Integration tests for complex features  
- All existing tests must pass
- Minimum 80% code coverage for new code
- Performance tests for critical paths

## Troubleshooting

### Common Issues

#### "Branch conflicts during merge"
```bash
git checkout feature/my-feature
git fetch origin
git merge origin/develop
# Resolve conflicts in editor
git add . && git commit -m "resolve: merge conflicts"
git push origin feature/my-feature
```

#### "Cannot push to protected branch"
```bash
# Don't push directly to main/develop
# Use feature branches and PRs instead
git checkout -b feature/my-changes
git push -u origin feature/my-changes
# Then create PR
```

#### "Develop branch behind main after release"
```bash
# This is normal after releases - use sync command
git sync-develop

# Or manually:
git checkout develop
git pull origin main  # Pull main changes into develop
git push origin develop
```

#### "Git aliases not working"
```bash
# Re-run setup script
./scripts/setup-git-aliases.sh

# Check existing aliases
git config --global --list | grep alias
```

#### "Build failing on CI but passing locally"
```bash
# Clean and rebuild
git clean -fdx
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release

# Check for case sensitivity issues (Linux CI)
# Ensure all file references match exact case
```

### Emergency Procedures

#### Rollback a Release
```bash
git checkout main
git checkout -b hotfix/1.2.1-rollback
git revert <commit-hash-of-release>
git commit -m "fix: rollback problematic release +semver: patch"
git hotfix-finish 1.2.1-rollback
```

#### Fix Corrupted Repository
```bash
# Fresh clone
cd ..
git clone https://github.com/Idevswork/idevs-foundation.git fresh-repo
cd fresh-repo
./scripts/setup-git-aliases.sh
# Copy your work from old repository
```

### Getting Help

```bash
# GitFlow script help
./scripts/gitflow.sh help

# Git help
git help
git help <command>

# GitHub CLI help  
gh help
gh <command> --help
```

## Complete Example: Adding User Authentication

Here's a complete workflow example:

```bash
# 1. Start feature
git sync
git feature-start user-authentication

# 2. Implement feature
echo "// User authentication code" > src/Auth/AuthService.cs
git add src/Auth/
git commit -m "feat: add user authentication service +semver: minor"

echo "// Authentication tests" > tests/Auth/AuthServiceTests.cs  
git add tests/Auth/
git commit -m "test: add authentication tests +semver: none"

echo "# Authentication docs" > docs/auth.md
git add docs/auth.md
git commit -m "docs: add authentication documentation +semver: none"

# 3. Push and test
git push origin feature/user-authentication
dotnet build --configuration Release
dotnet test --configuration Release

# 4. Finish feature
git feature-finish user-authentication

# 5. Create PR
gh pr create \
  --base develop \
  --title "feat: add user authentication service" \
  --body "Implements JWT-based user authentication with comprehensive tests and documentation."

# 6. After PR merged, create release
git sync
git release-start 1.3.0

# Update version and changelog
echo "<VersionPrefix>1.3.0</VersionPrefix>" >> Directory.Build.props
echo "## v1.3.0 - Added user authentication" >> CHANGELOG.md
git add . && git commit -m "chore: prepare release 1.3.0 +semver: none"
git push origin release/1.3.0

# Test and finish release
dotnet build --configuration Release
dotnet test --configuration Release
git release-finish 1.3.0

# Create GitHub release
gh release create v1.3.0 --generate-notes
```

---

**Need help?** Check the [repository issues](https://github.com/Idevswork/idevs-foundation/issues) or contact maintainers.
