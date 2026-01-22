namespace GetPRFromCommitsTool.Models
{
    public class Repository
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string GitHubRepoUrl { get; set; } = string.Empty;
        public string AzDoRepoUrl { get; set; } = string.Empty;
    }

    public class RepositoryConfiguration
    {
        public List<Repository> Repos { get; set; } = new();
    }
}