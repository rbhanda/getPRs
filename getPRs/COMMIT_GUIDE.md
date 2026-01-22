# How to Find Commit SHAs for Testing

This guide shows you how to find actual commit SHAs from GitHub repositories for testing the application.

## Method 1: Using GitHub Web Interface

1. Navigate to any GitHub repository (e.g., https://github.com/dotnet/installer)
2. Click on the "Commits" link or go to `https://github.com/{owner}/{repo}/commits`
3. You'll see a list of recent commits with their SHA hashes
4. Copy any two commit SHAs - use an older one as `OldCommit` and a newer one as `NewCommit`

## Method 2: Using Git Command Line

If you have the repository cloned locally:

```bash
# Get the last 10 commit SHAs
git log --oneline -10

# Get commits from a specific date range
git log --oneline --since="2024-01-01" --until="2024-01-31"

# Get the SHA of a specific commit
git rev-parse HEAD~5  # 5 commits ago
```

## Example Test Configuration

Here are some real examples you can use for testing:

### Microsoft VSCode Repository
```json
{
  "RepositoryName": "microsoft/vscode",
  "OldCommit": "b58957e67",
  "NewCommit": "1a5e9a267", 
  "UseGitHub": true
}
```

### .NET Core Repository  
```json
{
  "RepositoryName": "dotnet/core",
  "OldCommit": "8b532c812",
  "NewCommit": "f2b8a1c45",
  "UseGitHub": true
}
```

### ASP.NET Core Repository
```json
{
  "RepositoryName": "dotnet/aspnetcore", 
  "OldCommit": "c4d7f2a11",
  "NewCommit": "e8b9c3f22",
  "UseGitHub": true
}
```

## Tips for Choosing Commits

1. **Choose commits that are not too far apart** - GitHub API has limits on the number of commits it will return
2. **Use recent commits** - they're more likely to have associated PRs
3. **Ensure the old commit is actually older** - the tool compares from old to new
4. **Test with active repositories** - they'll have more PR activity

## Finding Commits with PRs

To find commits that are likely to have associated PRs:

1. Look for commits with messages like "Merge pull request #123"
2. Look at the "Pull requests" tab of the repository
3. Check recently merged PRs and note their commit SHAs
4. Use commits from the main/master branch as they're more likely to be from merged PRs

## Commit SHA Formats

- **Full SHA**: `1a5e9a267b8c3d4e5f6789012345678901234567` (40 characters)
- **Short SHA**: `1a5e9a2` (7+ characters) - GitHub typically shows 7-character short SHAs
- Both formats work with the GitHub API