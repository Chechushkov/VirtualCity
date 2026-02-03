# Setting Up Remote Git Repository for Excursion GPT API

This guide provides instructions for setting up a remote Git repository for the Excursion GPT API project.

## Option 1: GitHub

### Step 1: Create New Repository on GitHub
1. Go to [GitHub](https://github.com)
2. Click the "+" icon in the top-right corner and select "New repository"
3. Configure repository:
   - **Repository name**: `excursion-gpt-api`
   - **Description**: "ASP.NET Core API for 3D building excursions with Docker support"
   - **Visibility**: Private (recommended) or Public
   - **Initialize with README**: ❌ Uncheck (we already have README.md)
   - **Add .gitignore**: ❌ Uncheck (we already have .gitignore)
   - **Choose a license**: Select appropriate license or skip

### Step 2: Connect Local Repository to GitHub
```bash
# Navigate to project directory
cd "C:\Users\DELL G15\Documents\Experiments\GPT"

# Add remote repository (replace with your GitHub URL)
git remote add origin https://github.com/YOUR_USERNAME/excursion-gpt-api.git

# Verify remote was added
git remote -v

# Push to GitHub
git push -u origin master

# Push tags
git push --tags
```

## Option 2: GitLab

### Step 1: Create New Project on GitLab
1. Go to [GitLab](https://gitlab.com)
2. Click "New project"
3. Choose "Create blank project"
4. Configure project:
   - **Project name**: `excursion-gpt-api`
   - **Project slug**: `excursion-gpt-api`
   - **Visibility level**: Private or Public
   - **Initialize with README**: ❌ Uncheck

### Step 2: Connect Local Repository to GitLab
```bash
# Add remote repository (replace with your GitLab URL)
git remote add origin https://gitlab.com/YOUR_USERNAME/excursion-gpt-api.git

# Push to GitLab
git push -u origin --all
git push --tags
```

## Option 3: Azure DevOps

### Step 1: Create New Repository in Azure DevOps
1. Go to your Azure DevOps organization
2. Create a new project:
   - **Project name**: `ExcursionGPT`
   - **Visibility**: Private
   - **Version control**: Git
   - **Work item process**: Choose appropriate template

### Step 2: Connect Local Repository
```bash
# Add remote repository (replace with your Azure DevOps URL)
git remote add origin https://dev.azure.com/YOUR_ORG/YOUR_PROJECT/_git/excursion-gpt-api

# Push to Azure DevOps
git push -u origin --all
git push --tags
```

## Option 4: Bitbucket

### Step 1: Create New Repository on Bitbucket
1. Go to [Bitbucket](https://bitbucket.org)
2. Click "Create repository"
3. Configure repository:
   - **Repository name**: `excursion-gpt-api`
   - **Access level**: Private or Public
   - **Include README**: ❌ No
   - **Git ignore**: None (we have our own)

### Step 2: Connect Local Repository
```bash
# Add remote repository (replace with your Bitbucket URL)
git remote add origin https://YOUR_USERNAME@bitbucket.org/YOUR_USERNAME/excursion-gpt-api.git

# Push to Bitbucket
git push -u origin master
git push --tags
```

## Common Commands After Setup

### Verify Remote Configuration
```bash
# Check remote URLs
git remote -v

# Show remote branches
git branch -r
```

### Push Updates
```bash
# Push commits
git push

# Push specific branch
git push origin master

# Push all branches
git push --all origin

# Push tags
git push --tags
```

### Pull Updates
```bash
# Pull latest changes
git pull origin master

# Fetch without merging
git fetch origin
```

### Clone Repository (for other developers)
```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/excursion-gpt-api.git

# Clone with specific branch
git clone -b master https://github.com/YOUR_USERNAME/excursion-gpt-api.git
```

## SSH Key Setup (Recommended for Security)

### Generate SSH Key
```bash
# Generate new SSH key (if you don't have one)
ssh-keygen -t ed25519 -C "your_email@example.com"

# Or use RSA
ssh-keygen -t rsa -b 4096 -C "your_email@example.com"
```

### Add SSH Key to Git Provider
1. Copy your public key:
   ```bash
   # Windows
   cat ~/.ssh/id_ed25519.pub
   
   # Or
   clip < ~/.ssh/id_ed25519.pub
   ```

2. Add to your Git provider:
   - **GitHub**: Settings → SSH and GPG keys → New SSH key
   - **GitLab**: Preferences → SSH Keys
   - **Azure DevOps**: User settings → SSH public keys
   - **Bitbucket**: Personal settings → SSH keys

### Use SSH URL for Remote
```bash
# Change remote to SSH (GitHub example)
git remote set-url origin git@github.com:YOUR_USERNAME/excursion-gpt-api.git
```

## Branch Protection Rules (Recommended)

For production repositories, set up branch protection:

### GitHub
1. Go to repository Settings → Branches
2. Add branch protection rule for `master` branch:
   - ✅ Require pull request reviews before merging
   - ✅ Require status checks to pass
   - ✅ Include administrators
   - ✅ Require linear history

### GitLab
1. Settings → Repository → Protected branches
2. Protect `master` branch:
   - Allowed to merge: Maintainers
   - Allowed to push: No one

## CI/CD Integration

### GitHub Actions (.github/workflows/ci.yml)
```yaml
name: CI

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
```

### GitLab CI (.gitlab-ci.yml)
```yaml
image: mcr.microsoft.com/dotnet/sdk:9.0

stages:
  - build
  - test

build:
  stage: build
  script:
    - dotnet restore
    - dotnet build --no-restore

test:
  stage: test
  script:
    - dotnet test --no-build --verbosity normal
```

## Troubleshooting

### Authentication Issues
```bash
# Cache credentials (Windows)
git config --global credential.helper wincred

# Cache credentials (macOS/Linux)
git config --global credential.helper cache
```

### Permission Denied
```bash
# Check SSH connection
ssh -T git@github.com

# Verify SSH key is added
ssh-add -l
```

### Push Rejected
```bash
# Pull latest changes first
git pull origin master --rebase

# Force push (use with caution)
git push -f origin master
```

### Wrong Remote URL
```bash
# Change remote URL
git remote set-url origin NEW_URL

# Remove and add remote
git remote remove origin
git remote add origin NEW_URL
```

## Best Practices

1. **Regular Commits**: Commit frequently with descriptive messages
2. **Branch Strategy**: Use feature branches for new development
3. **Pull Requests**: Use PRs for code review before merging to master
4. **Tags**: Tag releases for version tracking
5. **.gitignore**: Keep .gitignore updated to exclude build artifacts
6. **Backup**: Regular backups of the repository

## Next Steps

1. Set up CI/CD pipeline
2. Configure deployment to staging/production
3. Add issue templates
4. Set up code review process
5. Configure automated testing
6. Set up monitoring and alerts

## Support

For issues with Git setup:
- Consult [Git documentation](https://git-scm.com/doc)
- Check your Git provider's help documentation
- Review error messages for specific guidance

---
**Repository Ready!** 🎉

Your Excursion GPT API is now version controlled and ready for collaborative development.