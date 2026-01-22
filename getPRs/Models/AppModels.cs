namespace GetPRFromCommitsTool.Models
{
    public class AppSettings
    {
        public string GitHubToken { get; set; } = string.Empty;
        public string AzureDevOpsToken { get; set; } = string.Empty;
        public int RateLimitDelay { get; set; } = 1000; // milliseconds
        public int MaxParallelRequests { get; set; } = 5;
    }

    public class TestConfiguration
    {
        public List<TestRepositoryCommits> TestRepos { get; set; } = new();
    }

    public class TestRepositoryCommits
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string OldCommit { get; set; } = string.Empty;
        public string NewCommit { get; set; } = string.Empty;
        public bool UseGitHub { get; set; } = true; // true for GitHub, false for Azure DevOps
    }

    public class PullRequestInfo
    {
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? MergedAt { get; set; }
        public string MergeCommitSha { get; set; } = string.Empty;
    }

    public class CommitInfo
    {
        public string Sha { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<PullRequestInfo> AssociatedPRs { get; set; } = new();
    }

    public class RepositoryResult
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string OldCommit { get; set; } = string.Empty;
        public string NewCommit { get; set; } = string.Empty;
        public List<CommitInfo> Commits { get; set; } = new();
        public List<PullRequestInfo> PullRequests { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}