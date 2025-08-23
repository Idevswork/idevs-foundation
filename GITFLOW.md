# GitFlow Workflow Guide for Idevs.Foundation

This comprehensive guide explains the GitFlow branching model used in the Idevs.Foundation repository, providing detailed workflows, best practices, and automation tools to streamline development and release processes.

## Table of Contents

- [Understanding GitFlow](#understanding-gitflow)
- [Branch Structure](#branch-structure)
- [Setup and Configuration](#setup-and-configuration)
- [Development Workflows](#development-workflows)
- [Release Management](#release-management)
- [Emergency Hotfixes](#emergency-hotfixes)
- [CI/CD Integration](#cicd-integration)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Understanding GitFlow

GitFlow is a branching model that defines a strict branching structure designed around project releases. It provides a robust framework for managing larger projects by:

- **Isolating feature development** from the main codebase
- **Supporting parallel development** of multiple features
- **Enabling controlled releases** with proper testing phases
- **Handling emergency fixes** without disrupting ongoing development
- **Maintaining release history** with clear version tagging

### Why GitFlow for Idevs.Foundation?

1. **NuGet Package Distribution**: We publish both preview and stable packages
2. **Multiple Feature Development**: Teams can work on different features simultaneously
3. **Quality Assurance**: Release branches allow final testing before production
4. **Semantic Versioning**: Automatic version calculation based on branch and commits
5. **CI/CD Automation**: Different branches trigger different deployment pipelines

## Branch Structure

### Main Branches (Permanent)

#### **`main` Branch**
- **Purpose**: Production-ready code that represents the current release
- **Deployment**: Automatically publishes stable packages to NuGet.org
- **Protection**: Direct commits forbidden, only accepts merges from release/hotfix branches
- **Versioning**: Each merge creates a new version tag (e.g., v1.2.0)
- **Quality Gate**: All tests must pass, code coverage requirements met

#### **`develop` Branch**
- **Purpose**: Integration branch where features come together
- **Deployment**: Publishes preview packages with `-alpha` suffix
- **State**: May be unstable as features are integrated
- **Source**: Receives merges from completed feature branches
- **Destination**: Source for release branches when ready for production

### Supporting Branches (Temporary)

#### **`feature/*` Branches**
- **Purpose**: Develop individual features or enhancements
- **Naming**: `feature/description` (e.g., `feature/user-authentication`)
- **Lifespan**: Created from `develop`, merged back to `develop`, then deleted
- **Scope**: Should focus on a single feature or closely related functionality
- **Testing**: Must include unit tests and pass all existing tests

#### **`release/*` Branches**
- **Purpose**: Prepare a specific version for production release
- **Naming**: `release/version` (e.g., `release/1.2.0`)
- **Activities**: Bug fixes, documentation updates, version bumping
- **No New Features**: Only bug fixes and release preparation allowed
- **Final Testing**: QA testing, integration testing, performance testing

#### **`hotfix/*` Branches**
- **Purpose**: Quickly fix critical issues in production
- **Naming**: `hotfix/version` (e.g., `hotfix/1.2.1`)
- **Urgency**: For critical bugs that can't wait for next regular release
- **Source**: Created from `main` branch
- **Destinations**: Merged to both `main` and `develop`

## Setup and Configuration

### Initial Repository Setup

If you're setting up the repository for the first time:

```bash
# Clone the repository
git clone https://github.com/Idevswork/idevs-foundation.git
cd idevs-foundation

# Ensure you have both main branches
git checkout main
git checkout develop  # or: git checkout -b develop origin/develop

# Set up Git aliases for convenience
./scripts/setup-git-aliases.sh
```

### Git Aliases Setup

The repository includes a setup script that creates convenient Git aliases:

```bash
# Run this once to set up aliases
./scripts/setup-git-aliases.sh
```

This creates the following aliases:
- `git feature-start <name>` - Start a new feature branch
- `git feature-finish <name>` - Finish a feature branch
- `git release-start <version>` - Start a release branch
- `git release-finish <version>` - Complete a release
- `git hotfix-start <version>` - Start a hotfix
- `git hotfix-finish <version>` - Complete a hotfix
- `git sync` - Sync main and develop branches
- `git status-all` - Show comprehensive repository status

### Development Environment

Ensure your development environment is properly configured:

```bash
# Install .NET 8.0 SDK
# Install GitHub CLI (gh) for PR management
brew install gh  # macOS
# or: winget install GitHub.cli  # Windows

# Authenticate with GitHub
gh auth login

# Set up your git identity
git config user.name "Your Name"
git config user.email "your.email@domain.com"
```

## Development Workflows

### Feature Development Workflow (Detailed)

#### 1. Planning Phase

Before starting a feature:
- **Review requirements** and acceptance criteria
- **Check existing features** to avoid duplicating work
- **Plan the implementation** approach
- **Consider dependencies** on other features

#### 2. Starting a Feature

```bash
# Option 1: Using Git alias (recommended)
git sync  # Ensure you have latest changes
git feature-start enhanced-repository-queries

# Option 2: Manual approach
git checkout develop
git pull origin develop
git checkout -b feature/enhanced-repository-queries
git push -u origin feature/enhanced-repository-queries
```

**Naming Conventions:**
- Use descriptive names: `user-authentication`, `json-query-support`
- Use hyphens, not underscores: `enhanced-repository` not `enhanced_repository`
- Avoid abbreviations: `authentication` not `auth`
- Keep it concise but clear: `caching-layer` not `implement-advanced-caching-layer-with-redis`

#### 3. Development Phase

**Best Practices During Development:**

```bash
# Make frequent, focused commits
git add src/NewFeature.cs
git commit -m "feat: add core NewFeature class +semver: minor"

git add tests/NewFeatureTests.cs
git commit -m "test: add unit tests for NewFeature +semver: none"

git add docs/NewFeature.md
git commit -m "docs: add NewFeature documentation +semver: none"
```

**Development Checklist:**
- [ ] Write unit tests for new functionality
- [ ] Update existing tests if needed
- [ ] Add integration tests for complex features
- [ ] Update documentation (README, code comments, XML docs)
- [ ] Follow existing code style and patterns
- [ ] Build and test locally: `dotnet build && dotnet test`

#### 4. Keeping Feature Branch Updated

```bash
# Regularly sync with develop to avoid large conflicts
git checkout develop
git pull origin develop
git checkout feature/enhanced-repository-queries
git merge develop  # or: git rebase develop

# Push updates
git push origin feature/enhanced-repository-queries
```

#### 5. Finishing a Feature

```bash
# Final testing
dotnet build --configuration Release
dotnet test --configuration Release

# Push final changes
git push origin feature/enhanced-repository-queries

# Finish the feature
git feature-finish enhanced-repository-queries

# Create Pull Request
gh pr create --base develop \
              --head feature/enhanced-repository-queries \
              --title "feat: enhanced repository query capabilities" \
              --body "Implements advanced querying features including JSON support and GraphQL integration.
              
              ### Changes
              - Added JSON query methods
              - Implemented GraphQL query parsing
              - Enhanced repository base class
              - Added comprehensive tests
              
              ### Testing
              - All existing tests pass
              - New unit tests added
              - Integration tests included
              
              Closes #123"
```

### Code Review Process

**For Pull Request Authors:**
1. **Self-review** your code before requesting review
2. **Write descriptive PR description** with context and changes
3. **Reference related issues** using "Closes #123" syntax
4. **Include screenshots** for UI changes
5. **Ensure CI passes** before requesting review

**For Reviewers:**
1. **Review code logic** and architecture
2. **Check test coverage** and quality
3. **Verify documentation** is updated
4. **Test locally** if needed
5. **Provide constructive feedback**

### Branch Maintenance

**Cleaning Up Local Branches:**

```bash
# List all branches
git branch -a

# Delete merged feature branches
git branch -d feature/completed-feature

# Force delete unmerged branches (use with caution)
git branch -D feature/abandoned-feature

# Prune remote tracking branches
git remote prune origin

# Clean up everything
git gc --prune=now
```

## Release Management (Comprehensive)

### Release Planning

Before starting a release:
1. **Feature freeze** - No new features after this point
2. **Review milestone** - Ensure all planned features are complete
3. **Version planning** - Determine version number based on changes
4. **Documentation review** - Update changelogs and documentation

### Release Preparation Workflow

#### 1. Create Release Branch

```bash
# Start release preparation
git sync
git release-start 1.3.0

# Alternative manual approach
git checkout develop
git pull origin develop
git checkout -b release/1.3.0
git push -u origin release/1.3.0
```

#### 2. Release Preparation Tasks

On the release branch, perform these tasks:

```bash
# Update version numbers in project files
# Edit Directory.Build.props
vim Directory.Build.props
# Update <VersionPrefix>1.3.0</VersionPrefix>

# Update CHANGELOG.md
vim CHANGELOG.md
# Add new section for v1.3.0 with all changes

# Update documentation if needed
vim README.md

# Commit preparation changes
git add .
git commit -m "chore: prepare release 1.3.0 +semver: none"
git push origin release/1.3.0
```

#### 3. Release Testing

**Comprehensive Testing Checklist:**

```bash
# Build and test in release mode
dotnet build --configuration Release
dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Package testing
./build-consolidated-package.sh Release
./build-individual-packages.sh Release

# Integration testing
# Test with sample projects
# Performance testing
# Security scanning
```

#### 4. Bug Fixes During Release

If bugs are found during release testing:

```bash
# Fix on release branch
git checkout release/1.3.0
# Make fixes...
git commit -m "fix: resolve issue with connection pooling +semver: patch"
git push origin release/1.3.0

# Continue testing until stable
```

#### 5. Complete the Release

```bash
# Final release
git release-finish 1.3.0

# This automatically:
# 1. Merges release branch to main
# 2. Tags the release (v1.3.0)
# 3. Merges release branch back to develop
# 4. Deletes the release branch
# 5. Pushes everything to remote
```

#### 6. Create GitHub Release

```bash
# Create GitHub release with auto-generated notes
gh release create v1.3.0 \
  --title "Idevs.Foundation v1.3.0" \
  --notes "Major update with enhanced repository capabilities and improved performance." \
  --generate-notes

# Or create draft release for review
gh release create v1.3.0 \
  --title "Idevs.Foundation v1.3.0" \
  --generate-notes \
  --draft
```

### Post-Release Activities

1. **Monitor deployment** - Ensure packages are published correctly
2. **Update project boards** - Close completed issues and milestones
3. **Communicate release** - Notify team and users about new version
4. **Plan next iteration** - Start planning next release features

## Emergency Hotfixes (Detailed)

### When to Use Hotfixes

Hotfixes are for **critical issues** that need immediate attention:
- **Security vulnerabilities** that expose user data
- **Critical bugs** that break core functionality
- **Performance issues** that severely impact users
- **Data corruption** or loss scenarios

**Not for hotfixes:**
- Minor bugs that can wait for next release
- New features or enhancements
- Non-critical performance improvements
- Documentation updates

### Hotfix Workflow

#### 1. Assess the Issue

```bash
# First, verify the issue in production
# Check error logs, user reports, monitoring alerts
# Determine impact and urgency
# Plan the minimal fix required
```

#### 2. Create Hotfix Branch

```bash
# Create hotfix from main (current production)
git sync
git hotfix-start 1.2.1

# Manual approach
git checkout main
git pull origin main
git checkout -b hotfix/1.2.1
git push -u origin hotfix/1.2.1
```

#### 3. Implement the Fix

```bash
# Make minimal changes to fix the issue
# Edit only what's necessary
vim src/ProblemArea/BuggyClass.cs

# Add/update tests to verify fix
vim tests/ProblemArea/BuggyClassTests.cs

# Commit the fix
git add .
git commit -m "fix: resolve critical null reference exception in user authentication

Fixes issue where null user context causes application crash
during login process. Added null checks and proper error handling.

Fixes #456
+semver: patch"

git push origin hotfix/1.2.1
```

#### 4. Test the Hotfix

```bash
# Thorough testing of the fix
dotnet build --configuration Release
dotnet test --configuration Release

# Test the specific scenario that was broken
# Regression testing to ensure no new issues
# Performance testing if applicable
```

#### 5. Complete the Hotfix

```bash
# Deploy the hotfix
git hotfix-finish 1.2.1

# This merges to both main and develop,
# creates tag v1.2.1, and cleans up
```

#### 6. Emergency Deployment

```bash
# Monitor CI/CD pipeline
gh run list --workflow=ci-cd.yml

# Verify package publication
# Check NuGet.org for new version
# Test deployment in staging if available
# Monitor production after deployment
```

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

### Critical Rule: One Feature = One Version Impact

**Important**: When implementing a feature across multiple commits, only the **first commit** that introduces the feature should have a version impact. Subsequent commits that build on the same feature should use `+semver: none`.

#### ✅ **Correct Approach - Single Feature Implementation**
```bash
# First commit introduces the feature (version impact)
git commit -m "feat(caching): add distributed caching service +semver: minor"

# Subsequent commits build on the same feature (no version impact)
git commit -m "feat(caching): implement Redis provider support +semver: none"
git commit -m "feat(caching): add dependency injection module +semver: none"
git commit -m "test: add caching service tests +semver: none"
git commit -m "docs: add caching documentation +semver: none"

# Result: Only ONE minor version bump for the entire feature
```

#### ❌ **Incorrect Approach - Multiple Version Impacts for Same Feature**
```bash
# Wrong: Each part of the same feature bumps version
git commit -m "feat(caching): add caching interfaces +semver: minor"     # Minor bump
git commit -m "feat(caching): implement caching service +semver: minor"   # Another minor bump (WRONG!)
git commit -m "feat(caching): add Redis support +semver: minor"        # Another minor bump (WRONG!)

# Result: Three unnecessary minor version bumps for one feature
```

#### ✅ **When Multiple Minor Bumps Are Correct**
```bash
# These are genuinely SEPARATE features:
git commit -m "feat: add caching service +semver: minor"           # Feature 1
git commit -m "feat: add logging service +semver: minor"          # Feature 2
git commit -m "feat: add validation service +semver: minor"       # Feature 3

# Each provides independent value to users
```

## CI/CD Integration (Comprehensive)

### Pipeline Overview

The Idevs.Foundation repository uses GitHub Actions for comprehensive CI/CD automation. Different branches trigger different pipeline behaviors:

#### **Feature Branches** (`feature/*`)
```yaml
Triggers:
  - Pull requests to develop
  - Pushes to feature branches

Actions:
  - ✅ Build verification (Debug + Release)
  - ✅ Run unit tests with coverage
  - ✅ Code quality analysis
  - ✅ Security scanning (CodeQL)
  - ❌ No package publishing
```

#### **Develop Branch**
```yaml
Triggers:
  - Direct pushes to develop
  - Merged pull requests

Actions:
  - ✅ Full build and test suite
  - ✅ Generate coverage reports
  - ✅ Publish preview packages to GitHub Packages
  - 📦 Package naming: Idevs.Foundation.1.2.0-alpha.123
  - 🏷️ No version tagging
```

#### **Main Branch**
```yaml
Triggers:
  - Release merges
  - Hotfix merges

Actions:
  - ✅ Complete build and test validation
  - ✅ Package generation (consolidated + individual)
  - 📦 Publish to NuGet.org (stable packages)
  - 📦 Publish to GitHub Packages (backup)
  - 🏷️ Automatic version tagging
  - 📋 Update GitHub release notes
```

#### **Release Branches** (`release/*`)
```yaml
Triggers:
  - Pushes to release branches

Actions:
  - ✅ Build and test validation
  - ✅ Pre-release package generation
  - 📦 Publish beta packages (optional)
  - ❌ No production deployment
```

### Package Publishing Strategy

#### **Version Calculation**
Versions are automatically calculated using GitVersion:

```bash
# Branch-based versioning
main branch:     1.2.0
develop branch:  1.3.0-alpha.42
feature branch:  1.3.0-feature.auth.123
release branch:  1.2.0-beta.1
hotfix branch:   1.2.1-hotfix.1
```

#### **Publishing Destinations**

**NuGet.org (Production)**
- Source: `main` branch only
- Package types: Stable releases (1.2.0)
- Audience: Public consumers
- Quality gate: Full test suite + manual approval

**GitHub Packages (Preview)**
- Source: `develop` branch
- Package types: Preview releases (1.3.0-alpha.42)
- Audience: Early adopters, internal testing
- Quality gate: Automated tests only

#### **Package Types**

**Consolidated Package** (`Idevs.Foundation`)
- Contains: All foundation components
- Use case: Most consumers (recommended)
- Dependencies: Self-contained

**Individual Packages**
- `Idevs.Foundation.Abstractions`
- `Idevs.Foundation.EntityFramework`
- `Idevs.Foundation.Cqrs`
- `Idevs.Foundation.Mediator`
- `Idevs.Foundation.Services`
- `Idevs.Foundation.Autofac`
- `Idevs.Foundation.Serilog`
- Use case: Granular dependency management

### Environment Configuration

#### **Required Secrets**
Configure these in GitHub repository settings:

```bash
# Required for NuGet.org publishing
NUGET_API_KEY=your-nuget-api-key

# Optional for code coverage
CODECOV_TOKEN=your-codecov-token

# GitHub token (automatically provided)
GITHUB_TOKEN=automatic
```

#### **Environment Protection**

**Production Environment** (main branch):
- Required reviewers: 1 (maintainers only)
- Deployment approval: Manual
- Branch protection: Enabled
- Status checks: Required

**Preview Environment** (develop branch):
- Auto-deployment: Enabled
- Branch protection: Enabled
- Status checks: Required

### Monitoring and Notifications

#### **Build Status**
```bash
# Check workflow status
gh run list --workflow=ci-cd.yml

# View specific run
gh run view 123456 --log

# Check current status
gh run watch
```

#### **Package Status**
```bash
# Check NuGet.org packages
nuget list Idevs.Foundation

# Check GitHub packages
gh api /user/packages/nuget/Idevs.Foundation/versions
```

#### **Quality Metrics**
- **Code Coverage**: Tracked via Codecov
- **Security**: Weekly CodeQL scans
- **Dependencies**: Dependabot auto-updates
- **Performance**: Build time monitoring

### Integration with Development Workflow

#### **Pre-commit Hooks** (Optional)
Set up local validation:

```bash
# Install pre-commit hooks
npm install -g @commitlint/cli @commitlint/config-conventional

# Add to .git/hooks/commit-msg
echo 'npx commitlint --edit $1' > .git/hooks/commit-msg
chmod +x .git/hooks/commit-msg
```

#### **Local Testing**
Before pushing, run the same checks as CI:

```bash
# Full validation (matches CI)
dotnet build --configuration Release
dotnet test --configuration Release --collect:"XPlat Code Coverage"
dotnet format --verify-no-changes

# Package validation
./build-consolidated-package.sh Release
./build-individual-packages.sh Release
```

## Best Practices (Detailed)

### Commit Message Guidelines

#### **Conventional Commits Format**
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

#### **Commit Types**
- `feat`: New feature (semver: minor)
- `fix`: Bug fix (semver: patch)
- `docs`: Documentation only changes (semver: none)
- `style`: Code style changes (semver: none)
- `refactor`: Code refactoring (semver: patch)
- `test`: Adding missing tests (semver: none)
- `chore`: Build process or auxiliary tool changes (semver: none)
- `perf`: Performance improvements (semver: patch)
- `ci`: CI configuration changes (semver: none)

#### **Breaking Changes**
```bash
# Breaking change (semver: major)
git commit -m "feat!: change authentication API signature

BREAKING CHANGE: The authenticate() method now requires
a second parameter for multi-factor authentication.

+semver: major"
```

#### **Examples**
```bash
# Good commit messages
git commit -m "feat(auth): add OAuth2 integration +semver: minor"
git commit -m "fix(cache): resolve memory leak in Redis provider +semver: patch"
git commit -m "docs(readme): update installation instructions +semver: none"

# Bad commit messages
git commit -m "fix stuff"  # Too vague
git commit -m "WIP"        # Not descriptive
git commit -m "Quick fix"  # No context
```

### Branch Naming Conventions

#### **Feature Branches**
```bash
# Good names
feature/user-authentication
feature/json-query-support
feature/performance-improvements
feature/issue-123-database-connection

# Bad names
feature/auth          # Too abbreviated
feature/fix           # Not descriptive
feature/john-work     # Personal reference
feature/temp-branch   # Temporary nature
```

#### **Release Branches**
```bash
# Standard format
release/1.2.0
release/2.0.0-beta.1

# Emergency releases
release/1.2.1-security-fix
```

#### **Hotfix Branches**
```bash
# Version-based naming
hotfix/1.2.1
hotfix/1.2.2-security

# Issue-based naming (alternative)
hotfix/critical-null-reference
hotfix/security-vulnerability-cve-2023-1234
```

### Code Quality Standards

#### **Code Style**
```bash
# Apply consistent formatting
dotnet format

# Check for style violations
dotnet format --verify-no-changes
```

#### **Documentation Requirements**
- XML documentation for all public APIs
- README updates for new features
- CHANGELOG.md entries for all releases
- Code comments for complex algorithms

#### **Testing Requirements**
- Unit tests for all new functionality
- Integration tests for complex features
- Minimum 80% code coverage
- Performance tests for critical paths

### Security Considerations

#### **Dependency Management**
```bash
# Check for vulnerable packages
dotnet list package --vulnerable

# Update packages
dotnet add package PackageName --version latest
```

#### **Secret Management**
- Never commit secrets or API keys
- Use GitHub secrets for CI/CD
- Use Azure Key Vault for production secrets
- Rotate keys regularly

#### **Code Analysis**
```bash
# Static code analysis
dotnet build --configuration Release --verbosity normal

# Security analysis (if tools available)
# Run CodeQL analysis via GitHub Actions
```

### Performance Guidelines

#### **Build Performance**
- Use incremental builds when possible
- Cache NuGet packages in CI
- Parallelize test execution
- Optimize Docker layer caching

#### **Code Performance**
- Profile critical paths
- Use async/await properly
- Implement proper disposal patterns
- Monitor memory usage

## Troubleshooting (Comprehensive)

### Common Issues and Solutions

#### **Branch-Related Issues**

**Problem**: "Branch conflicts during merge"
```bash
# Solution: Update and resolve conflicts
git checkout feature/my-feature
git fetch origin
git merge origin/develop

# Resolve conflicts in your editor
# Then continue
git add .
git commit -m "resolve: merge conflicts with develop"
git push origin feature/my-feature
```

**Problem**: "Cannot push to protected branch"
```bash
# Solution: Use proper GitFlow process
# Don't push directly to main/develop
# Create feature branch and PR instead

git checkout -b feature/my-changes
git push -u origin feature/my-changes
# Then create PR on GitHub
```

**Problem**: "Git aliases not working"
```bash
# Solution: Re-run setup script
./scripts/setup-git-aliases.sh

# Or check if aliases exist
git config --global --list | grep alias

# Manual setup if needed
git config --global alias.feature-start '!f() { git checkout develop && git pull origin develop && git checkout -b "feature/$1" && git push -u origin "feature/$1"; }; f'
```

#### **CI/CD Issues**

**Problem**: "Build failing on CI but passing locally"
```bash
# Solution: Check environment differences

# 1. Check .NET version
dotnet --version

# 2. Clean and rebuild
git clean -fdx
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release

# 3. Check for case sensitivity (Linux CI)
find . -name "*.cs" | grep -v bin | grep -v obj
```

**Problem**: "Package publishing fails"
```bash
# Solution: Check NuGet API key and permissions

# 1. Verify secrets are set in GitHub
# 2. Check package version conflicts
nuget list Idevs.Foundation -Source https://api.nuget.org/v3/index.json

# 3. Test local package generation
./build-consolidated-package.sh Release
ls -la artifacts/
```

**Problem**: "Tests timeout in CI"
```bash
# Solution: Optimize test execution

# 1. Run tests with timeout
dotnet test --logger "console;verbosity=detailed" -- RunConfiguration.TestSessionTimeout=600000

# 2. Run specific failing test
dotnet test --filter "FullyQualifiedName~YourTestClass.YourTestMethod"

# 3. Check for deadlocks or infinite loops
```

#### **Version and Release Issues**

**Problem**: "Wrong version number generated"
```bash
# Solution: Check GitVersion configuration

# 1. Install GitVersion tool
dotnet tool install --global GitVersion.Tool

# 2. Check version calculation
gitversion

# 3. Verify branch naming
git branch --show-current

# 4. Check commit history
git log --oneline -10
```

**Problem**: "Release branch merge conflicts"
```bash
# Solution: Resolve conflicts carefully

# 1. Update release branch with latest main
git checkout release/1.2.0
git fetch origin
git merge origin/main

# 2. Resolve conflicts
# Edit conflicted files
git add .
git commit -m "resolve: merge conflicts with main"

# 3. Complete release
git release-finish 1.2.0
```

**Problem**: "Hotfix not appearing in develop"
```bash
# Solution: Ensure hotfix was merged to both branches

# 1. Check merge history
git log --graph --oneline main develop

# 2. If missing, manually merge to develop
git checkout develop
git merge hotfix/1.2.1
git push origin develop
```

### Emergency Procedures

#### **Rollback a Release**
```bash
# If a release needs to be rolled back

# 1. Create hotfix with revert
git checkout main
git checkout -b hotfix/1.2.1-rollback
git revert <commit-hash-of-problematic-release>
git push origin hotfix/1.2.1-rollback

# 2. Complete hotfix process
git hotfix-finish 1.2.1-rollback

# 3. Unpublish package if needed (contact NuGet.org)
```

#### **Fix Broken Main Branch**
```bash
# If main branch is in broken state

# 1. Create emergency hotfix
git checkout main
git checkout -b hotfix/emergency-fix
# Make minimal fixes
git commit -m "fix: emergency repair of main branch"

# 2. Test thoroughly
dotnet build --configuration Release
dotnet test --configuration Release

# 3. Complete hotfix
git hotfix-finish emergency-fix
```

#### **Corrupted Repository**
```bash
# If local repository becomes corrupted

# 1. Fresh clone
cd ..
git clone https://github.com/Idevswork/idevs-foundation.git idevs-foundation-fresh
cd idevs-foundation-fresh

# 2. Apply your changes
# Copy your work from the old repository

# 3. Re-setup environment
./scripts/setup-git-aliases.sh
```

### Getting Help

#### **Internal Resources**
- Check repository README.md
- Review GitHub Issues for similar problems
- Check GitHub Discussions for Q&A
- Review GitHub Actions logs for CI issues

#### **Commands for Help**
```bash
# GitFlow script help
./scripts/gitflow.sh help

# Git help
git help
git help <command>

# GitHub CLI help
gh help
gh <command> --help

# .NET help
dotnet --help
dotnet <command> --help
```

#### **External Resources**
- [GitFlow Original Documentation](https://nvie.com/posts/a-successful-git-branching-model/)
- [Conventional Commits Specification](https://conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [GitHub Flow Documentation](https://guides.github.com/introduction/flow/)

---

**Need immediate help?** Check the repository Issues or Discussions, or contact the maintainers directly.

## Complete Feature Development Example: Distributed Caching Service

Here's a complete example of adding a significant feature to the Idevs.Foundation library, showing proper GitFlow and semantic versioning practices:

### Step 1: Start the Feature Branch

```bash
# Ensure we're on develop and up-to-date
git checkout develop
git pull origin develop

# Start the feature using GitFlow alias
git feature-start distributed-caching

# This creates and switches to: feature/distributed-caching
```

### Step 2: Implement the Feature (with proper versioning)

#### 2.1: Create Core Abstractions

```bash
# Create the cache abstraction interface and base types
git add src/Idevs.Foundation.Abstractions/Caching/
git commit -m "feat(caching): add distributed caching service abstractions

Introduces core caching abstractions:
- ICacheService for basic cache operations
- ICacheKeyGenerator for consistent key generation
- CacheOptions for configuration
- CacheEntry<T> for typed cache entries

+semver: minor"
```

#### 2.2: Implement Core Services

```bash
# Create concrete implementations
git add src/Idevs.Foundation.Services/Caching/
git commit -m "feat(caching): implement distributed caching service providers

Adds implementation for multiple providers:
- Redis distributed cache provider
- In-memory cache provider
- SQL Server cache provider

Part of the distributed caching feature.

+semver: none"
```

#### 2.3: Add DI Support

```bash
# Add dependency injection support
git add src/Idevs.Foundation.Autofac/CachingModule.cs
git commit -m "feat(caching): add Autofac registration module

Provides dependency injection for caching services.
Completes the distributed caching feature implementation.

+semver: none"
```

#### 2.4: Add Tests & Documentation

```bash
# Add comprehensive test coverage
git add tests/Idevs.Foundation.Tests/Caching/
git commit -m "test: add distributed caching service tests

Comprehensive test suite for all caching scenarios.

+semver: none"

# Add documentation
git add docs/caching.md README.md
git commit -m "docs: add caching service documentation

Includes usage examples and configuration options.

+semver: none"
```

### Step 3: Keep Branch Updated and Run Tests

```bash
# Periodically sync with develop (during development)
git checkout develop
git pull origin develop
git checkout feature/distributed-caching
git merge develop
# Resolve any conflicts if they occur

# Before finishing, ensure everything is up-to-date and tested
git push origin feature/distributed-caching

# Run comprehensive tests
dotnet build --configuration Release
dotnet test --configuration Release

# Ensure code quality
dotnet format --verify-no-changes
```

### Step 4: Finish the Feature

```bash
# Push final changes
git push origin feature/distributed-caching

# Finish the feature
git feature-finish distributed-caching

# Create detailed PR
gh pr create --base develop --head feature/distributed-caching \
  --title "feat: add distributed caching service with multi-provider support" \
  --body "Implements comprehensive caching with Redis, Memory, and SQL Server providers.
  
  Key features:
  - Type-safe cache operations
  - Multiple provider support
  - Automatic serialization
  - Dependency injection support
  
  Closes #123"
```

### Step 5: Version Impact

**Version Calculation:**
- **Initial Version**: 1.2.0
- **After Feature Merge**: 1.3.0-alpha.5 (Minor bump due to new feature)
- **Final Release**: 1.3.0 (When released)

**Note**: Even though we made multiple commits, the version only increased once (minor) because all commits were part of the same feature and we properly used `+semver: none` for continuation commits.

## Typical Workflow Example (Simple Feature)

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
