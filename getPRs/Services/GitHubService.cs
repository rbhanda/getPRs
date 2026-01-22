using GetPRFromCommitsTool.Models;
using Microsoft.Extensions.Logging;
using Octokit;

namespace GetPRFromCommitsTool.Services
{
    public interface IGitHubService
    {
        Task<RepositoryResult> GetPRsBetweenCommitsAsync(string repositoryName, string oldCommit, string newCommit);
    }

    public class GitHubService : IGitHubService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly ILogger<GitHubService> _logger;
        private readonly AppSettings _appSettings;

        public GitHubService(string token, AppSettings appSettings, ILogger<GitHubService> logger)
        {
            _gitHubClient = new GitHubClient(new ProductHeaderValue("GetPRFromCommitsTool"))
            {
                Credentials = new Credentials(token)
            };
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<RepositoryResult> GetPRsBetweenCommitsAsync(string repositoryName, string oldCommit, string newCommit)
        {
            var result = new RepositoryResult
            {
                RepositoryName = repositoryName,
                OldCommit = oldCommit,
                NewCommit = newCommit
            };

            try
            {
                var parts = repositoryName.Split('/');
                if (parts.Length != 2)
                {
                    result.ErrorMessage = $"Invalid repository name format: {repositoryName}. Expected format: owner/repo";
                    return result;
                }

                var owner = parts[0];
                var repo = parts[1];

                _logger.LogInformation($"Processing repository: {repositoryName}");
                
                // Get commits between the two commit SHAs
                var commits = await GetCommitsBetweenAsync(owner, repo, oldCommit, newCommit);
                result.Commits = commits;

                // Get PRs for each commit
                var allPRs = new Dictionary<int, PullRequestInfo>();
                
                foreach (var commit in commits)
                {
                    await Task.Delay(_appSettings.RateLimitDelay); // Rate limiting
                    
                    var prs = await GetPRsForCommitAsync(owner, repo, commit.Sha);
                    foreach (var pr in prs)
                    {
                        if (!allPRs.ContainsKey(pr.Number))
                        {
                            allPRs[pr.Number] = pr;
                        }
                        commit.AssociatedPRs.Add(pr);
                    }
                }

                result.PullRequests = allPRs.Values.ToList();
                result.Success = true;

                _logger.LogInformation($"Found {commits.Count} commits and {allPRs.Count} unique PRs for {repositoryName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing repository {repositoryName}");
                result.ErrorMessage = ex.Message;
                result.Success = false;
            }

            return result;
        }

        private async Task<List<CommitInfo>> GetCommitsBetweenAsync(string owner, string repo, string oldCommit, string newCommit)
        {
            try
            {
                // Get the comparison between old and new commit
                var comparison = await _gitHubClient.Repository.Commit.Compare(owner, repo, oldCommit, newCommit);
                
                var commits = new List<CommitInfo>();
                
                foreach (var commit in comparison.Commits)
                {
                    commits.Add(new CommitInfo
                    {
                        Sha = commit.Sha,
                        Message = commit.Commit.Message,
                        Author = commit.Commit.Author.Name,
                        Date = commit.Commit.Author.Date.DateTime
                    });
                }

                return commits;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting commits between {oldCommit} and {newCommit} for {owner}/{repo}");
                throw;
            }
        }

        private async Task<List<PullRequestInfo>> GetPRsForCommitAsync(string owner, string repo, string commitSha)
        {
            try
            {
                _logger.LogInformation($"Looking for PRs associated with commit {commitSha[..7]}...");
                
                // Use GitHub's search API to find PRs more efficiently
                // First, try to use the commit API to get associated pull requests
                try
                {
                    var commit = await _gitHubClient.Repository.Commit.Get(owner, repo, commitSha);
                    var matchingPRs = new List<PullRequestInfo>();
                    
                    // Check if this is a merge commit (usually from a PR)
                    if (commit.Parents.Count > 1)
                    {
                        _logger.LogInformation($"Commit {commitSha[..7]} appears to be a merge commit, searching for associated PR...");
                        
                        // Try to extract PR number from commit message
                        var prNumberMatch = System.Text.RegularExpressions.Regex.Match(
                            commit.Commit.Message, @"Merge pull request #(\d+)");
                        
                        if (prNumberMatch.Success && int.TryParse(prNumberMatch.Groups[1].Value, out int prNumber))
                        {
                            try
                            {
                                var pr = await _gitHubClient.PullRequest.Get(owner, repo, prNumber);
                                matchingPRs.Add(new PullRequestInfo
                                {
                                    Number = pr.Number,
                                    Title = pr.Title,
                                    Url = pr.HtmlUrl,
                                    Author = pr.User.Login,
                                    CreatedAt = pr.CreatedAt.DateTime,
                                    MergedAt = pr.MergedAt?.DateTime,
                                    MergeCommitSha = pr.MergeCommitSha ?? string.Empty
                                });
                                
                                _logger.LogInformation($"Found PR #{prNumber} for commit {commitSha[..7]}");
                                return matchingPRs;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Could not retrieve PR #{prNumber}");
                            }
                        }
                    }
                    
                    // Fallback: Search recent merged PRs (limited scope to avoid hanging)
                    _logger.LogInformation($"Searching recent merged PRs for commit {commitSha[..7]}...");
                    
                    var recentPRs = await _gitHubClient.PullRequest.GetAllForRepository(owner, repo, 
                        new PullRequestRequest 
                        { 
                            State = ItemStateFilter.Closed,
                            SortProperty = PullRequestSort.Updated,
                            SortDirection = SortDirection.Descending
                        }, 
                        new ApiOptions { PageSize = 50, PageCount = 1 }); // Limit to first 50 recent closed PRs
                    
                    var checkedCount = 0;
                    foreach (var pr in recentPRs.Where(p => p.MergedAt.HasValue).Take(20)) // Only check 20 recent merged PRs
                    {
                        checkedCount++;
                        _logger.LogInformation($"Checking PR #{pr.Number} ({checkedCount}/20)...");
                        
                        try
                        {
                            // Get commits for this PR
                            var prCommits = await _gitHubClient.PullRequest.Commits(owner, repo, pr.Number);
                            
                            if (prCommits.Any(c => c.Sha == commitSha))
                            {
                                matchingPRs.Add(new PullRequestInfo
                                {
                                    Number = pr.Number,
                                    Title = pr.Title,
                                    Url = pr.HtmlUrl,
                                    Author = pr.User.Login,
                                    CreatedAt = pr.CreatedAt.DateTime,
                                    MergedAt = pr.MergedAt?.DateTime,
                                    MergeCommitSha = pr.MergeCommitSha ?? string.Empty
                                });
                                
                                _logger.LogInformation($"Found PR #{pr.Number} containing commit {commitSha[..7]}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Error checking PR {pr.Number} for commit {commitSha}");
                        }

                        // Rate limiting between PR checks
                        await Task.Delay(200);
                    }

                    _logger.LogInformation($"Found {matchingPRs.Count} PRs for commit {commitSha[..7]}");
                    return matchingPRs;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error getting commit details for {commitSha}");
                    return new List<PullRequestInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PRs for commit {commitSha} in {owner}/{repo}");
                return new List<PullRequestInfo>();
            }
        }
    }
}