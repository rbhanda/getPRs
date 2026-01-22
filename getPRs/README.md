# GetPRFromCommitsTool

A .NET application that finds Pull Requests between two commits in GitHub repositories.

## Features

- 🔍 Finds all commits between two commit SHAs
- 📋 Identifies Pull Requests associated with each commit
- 🚀 Parallel processing with rate limiting
- ⚙️ Configurable test scenarios
- 📊 Detailed JSON output with summary

## Setup

### 1. Configure GitHub Token

Edit `appsettings.json` and replace `YOUR_GITHUB_TOKEN_HERE` with your actual GitHub Personal Access Token:

```json
{
  "GitHubToken": "ghp_your_actual_token_here",
  "AzureDevOpsToken": "YOUR_AZDO_TOKEN_HERE",
  "RateLimitDelay": 1000,
  "MaxParallelRequests": 5
}
```

#### Creating a GitHub Token

1. Go to GitHub Settings > Developer settings > Personal access tokens > Tokens (classic)
2. Click "Generate new token (classic)"
3. Select scopes: `repo` (for private repos) or `public_repo` (for public repos only)
4. Copy the token and paste it in `appsettings.json`

### 2. Configure Test Scenarios

Edit `test_config.json` to specify which repositories and commit ranges to analyze:

```json
{
  "TestRepos": [
    {
      "RepositoryName": "dotnet/installer",
      "OldCommit": "abc123...",
      "NewCommit": "def456...",
      "UseGitHub": true
    }
  ]
}
```

Replace `abc123...` and `def456...` with actual commit SHAs from the repository.

#### Finding Commit SHAs

You can find commit SHAs by:
1. Going to the repository on GitHub
2. Click on "Commits" 
3. Copy the SHA hash (usually 7-40 characters) from any commit
4. Use an older commit as `OldCommit` and a newer commit as `NewCommit`

Example real commits from dotnet/installer:
- Old commit: `4b1ae8c0870d6d7924c14b3837e23e98a7c511cb`
- New commit: `8d55c73b7a9e468b2316e6b6ad7f6f7f0b8e8e8e`

## Running the Application

### Prerequisites

- .NET 8.0 SDK
- Internet connection for GitHub API calls

### Build and Run

```bash
# Restore packages
dotnet restore

# Build the application  
dotnet build

# Run the application
dotnet run
```

## Output

The application generates:

1. **Console output** with real-time progress and summary
2. **results.json** with detailed information about:
   - All commits found between the specified range
   - Pull Requests associated with each commit
   - PR details (title, author, creation date, merge date)
   - Error information for failed repositories

### Sample Output

```
✅ dotnet/installer: 15 commits, 8 PRs
   Pull Requests:
     - #1234: Fix build issue in Windows by johndoe
     - #1235: Update dependencies by janedoe
     - #1236: Add new feature XYZ by contributor
```

## Configuration Options

### appsettings.json

- `GitHubToken`: Your GitHub Personal Access Token
- `AzureDevOpsToken`: Your Azure DevOps Personal Access Token (for future AzDO support)
- `RateLimitDelay`: Delay between API calls in milliseconds (default: 1000)
- `MaxParallelRequests`: Maximum number of parallel requests (default: 5)

### Rate Limiting

The tool implements GitHub rate limiting best practices:
- Configurable delay between requests
- Limited parallel requests
- Automatic retry logic (planned)
- Respectful API usage

## Repository Support

Currently supports:
- ✅ GitHub repositories
- 🚧 Azure DevOps repositories (planned)

The application reads from `repo_list.json` which contains repository URLs for both GitHub and Azure DevOps, but currently only processes GitHub repositories.

## Troubleshooting

### Common Issues

1. **"Please configure your GitHub token"**
   - Make sure you've replaced `YOUR_GITHUB_TOKEN_HERE` in `appsettings.json`

2. **"No test configuration found"**
   - Update `test_config.json` with valid repository names and commit SHAs

3. **"Invalid repository name format"**
   - Repository names should be in format `owner/repo` (e.g., `dotnet/installer`)

4. **Rate limit errors**
   - Increase `RateLimitDelay` in `appsettings.json`
   - Decrease `MaxParallelRequests`

5. **Commit not found errors**
   - Verify commit SHAs exist in the repository
   - Ensure commits are in the correct order (old commit should be older than new commit)

## Project Structure

```
GetPRFromCommitsTool/
├── Models/
│   ├── AppModels.cs          # Data models for PRs, commits, results
│   └── Repository.cs         # Repository configuration models
├── Services/
│   └── GitHubService.cs      # GitHub API integration
├── Program.cs                # Main application entry point
├── appsettings.json          # Application configuration
├── test_config.json          # Test scenarios configuration
├── repo_list.json           # Repository list
└── GetPRFromCommitsTool.csproj # Project file
```

## Future Enhancements

- Azure DevOps support
- Enhanced error handling and retry logic
- Additional output formats (CSV, Excel)
- Web UI interface
- Caching for improved performance
- Support for branch comparisons