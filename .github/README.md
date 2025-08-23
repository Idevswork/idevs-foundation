# GitHub Actions CI/CD Setup

This directory contains the GitHub Actions workflows and configuration for the Idevs.Foundation repository.

## Overview

The CI/CD pipeline consists of several workflows:

1. **Main CI/CD Pipeline** (`ci-cd.yml`) - Builds, tests, and publishes packages
2. **CodeQL Security Analysis** (`codeql.yml`) - Performs security and quality analysis
3. **Dependabot** (`dependabot.yml`) - Automated dependency updates

## Setup Instructions

### 1. Repository Secrets

You need to configure the following secrets in your GitHub repository:

#### Required Secrets:
- `NUGET_API_KEY` - Your NuGet.org API key for publishing packages
- `CODECOV_TOKEN` - (Optional) Your Codecov token for code coverage reports

#### To add secrets:
1. Go to your repository on GitHub
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with its corresponding value

### 2. Environment Configuration

The pipeline uses two environments for deployment protection:

#### Preview Environment:
- Used for publishing preview packages from `develop` branch
- Publishes to GitHub Packages with `-alpha` suffix

#### Production Environment:
- Used for publishing release packages from `main` branch and releases
- Publishes to both NuGet.org and GitHub Packages

#### To set up environments:
1. Go to **Settings** → **Environments**
2. Create `preview` and `production` environments
3. (Optional) Add protection rules and required reviewers

### 3. Branch Strategy

The pipeline follows GitFlow branching model:

- **`main`** - Production-ready releases
- **`develop`** - Development integration branch
- **`feature/*`** - Feature branches
- **`release/*`** - Release preparation branches
- **`hotfix/*`** - Emergency fixes

### 4. Semantic Versioning

The pipeline uses GitVersion for automatic semantic versioning:

- **Patch increment**: Default for `main` branch
- **Minor increment**: Default for `develop` branch
- **Manual control**: Use commit messages with `+semver:` tags

#### Commit Message Examples:
```
feat: add new repository pattern +semver: minor
fix: resolve null reference exception +semver: patch
feat!: breaking change to API +semver: major
docs: update README +semver: none
```

## Workflow Details

### CI/CD Pipeline (`ci-cd.yml`)

#### Build and Test Job:
- Runs on every push and pull request
- Sets up .NET 8.0
- Restores dependencies and builds solution
- Runs all tests with code coverage
- Uploads test results and coverage reports

#### Package Job:
- Runs after successful build/test (excludes PRs)
- Uses GitVersion for semantic versioning
- Builds consolidated package for all branches
- Builds individual packages for `main` and releases
- Uploads package artifacts

#### Publishing Jobs:
- **Preview** (develop branch): Publishes to GitHub Packages
- **Release** (main branch): Publishes to NuGet.org and GitHub Packages
- **GitHub Release**: Publishes on GitHub releases and attaches packages

### Security Analysis (`codeql.yml`)

- Runs CodeQL analysis on C# code
- Triggered on pushes to main/develop, PRs to main, and weekly schedule
- Uses security-extended and security-and-quality query suites

### Dependency Updates (`dependabot.yml`)

- Automatically creates PRs for dependency updates
- Groups related packages (Microsoft.Extensions, EF Core, Serilog, etc.)
- Runs weekly on Mondays
- Ignores major version updates for stable packages

## Package Publishing Strategy

### Consolidated Package (`Idevs.Foundation`)
- Contains all Foundation components
- Published on every build (develop/main)
- Recommended for most consumers

### Individual Packages
- Granular packages for specific functionality
- Only built for `main` branch and releases
- Allows selective dependency management

## Usage Examples

### Triggering a Release

1. **Create a release branch:**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b release/1.2.0
   git push -u origin release/1.2.0
   ```

2. **Merge to main and create release:**
   ```bash
   git checkout main
   git merge --no-ff release/1.2.0
   git tag v1.2.0
   git push origin main --tags
   ```

3. **Create GitHub Release:**
   - Go to GitHub → Releases → Create Release
   - Select the tag (v1.2.0)
   - This will trigger the release publishing workflow

### Publishing a Hotfix

1. **Create hotfix from main:**
   ```bash
   git checkout main
   git checkout -b hotfix/1.2.1
   # Make your fixes
   git commit -m "fix: critical bug fix +semver: patch"
   git push -u origin hotfix/1.2.1
   ```

2. **Merge to main:**
   ```bash
   git checkout main
   git merge --no-ff hotfix/1.2.1
   git push origin main
   ```

## Monitoring and Troubleshooting

### Build Status
- Check the Actions tab for build status
- All workflows must pass before merging PRs
- Failed builds will prevent package publishing

### Package Verification
- Verify packages are published to intended repositories
- Check version numbers match expectations
- Validate package contents in artifacts

### Common Issues

1. **NuGet API Key Issues:**
   - Verify the `NUGET_API_KEY` secret is set correctly
   - Ensure the key has permissions to publish packages

2. **Version Conflicts:**
   - Check GitVersion configuration
   - Verify commit messages follow semver conventions
   - Review branch naming conventions

3. **Test Failures:**
   - Check test output in the Actions logs
   - Ensure all dependencies are available in CI environment

## Security Considerations

- Secrets are never logged or exposed in workflows
- Package signing is handled by NuGet.org
- CodeQL analysis runs automatically for security scanning
- Dependabot keeps dependencies up to date

## Contributing

When contributing to the CI/CD configuration:

1. Test workflow changes in a fork first
2. Update this documentation for any configuration changes
3. Follow the established branching strategy
4. Ensure backward compatibility when possible

For more information about GitHub Actions, see the [GitHub Actions Documentation](https://docs.github.com/en/actions).
