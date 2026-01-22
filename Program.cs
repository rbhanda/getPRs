using GetPRFromCommitsTool.Models;
using GetPRFromCommitsTool.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GetPRFromCommitsTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var appSettings = configuration.Get<AppSettings>() ?? new AppSettings();

            // Setup logging
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting PR Analysis Tool");

                // Validate tokens
                if (string.IsNullOrEmpty(appSettings.GitHubToken) || appSettings.GitHubToken == "YOUR_GITHUB_TOKEN_HERE")
                {
                    logger.LogError("Please configure your GitHub token in appsettings.json");
                    return;
                }

                // Load repositories
                var repoConfig = await LoadRepositoryConfigAsync();
                if (repoConfig == null)
                {
                    logger.LogError("Failed to load repository configuration");
                    return;
                }

                // Load test configuration
                var testConfig = await LoadTestConfigAsync();
                if (testConfig == null)
                {
                    logger.LogWarning("No test configuration found. Please update test_config.json with commit hashes for testing.");
                    return;
                }

                // Create GitHub service
                var githubLogger = serviceProvider.GetRequiredService<ILogger<GitHubService>>();
                var githubService = new GitHubService(appSettings.GitHubToken, appSettings, githubLogger);

                // Process repositories
                var results = new List<RepositoryResult>();
                var semaphore = new SemaphoreSlim(appSettings.MaxParallelRequests, appSettings.MaxParallelRequests);

                var tasks = testConfig.TestRepos.Where(tr => tr.UseGitHub).Select(async testRepo =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        logger.LogInformation($"Processing {testRepo.RepositoryName}...");
                        var result = await githubService.GetPRsBetweenCommitsAsync(
                            testRepo.RepositoryName, 
                            testRepo.OldCommit, 
                            testRepo.NewCommit);
                        return result;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                results.AddRange(await Task.WhenAll(tasks));

                // Output results
                await OutputResultsAsync(results, logger);

                logger.LogInformation("Analysis completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during execution");
            }
        }

        private static async Task<RepositoryConfiguration?> LoadRepositoryConfigAsync()
        {
            try
            {
                var json = await File.ReadAllTextAsync("repo_list.json");
                return JsonConvert.DeserializeObject<RepositoryConfiguration>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading repository configuration: {ex.Message}");
                return null;
            }
        }

        private static async Task<TestConfiguration?> LoadTestConfigAsync()
        {
            try
            {
                var json = await File.ReadAllTextAsync("test_config.json");
                return JsonConvert.DeserializeObject<TestConfiguration>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading test configuration: {ex.Message}");
                return null;
            }
        }

        private static async Task OutputResultsAsync(List<RepositoryResult> results, ILogger logger)
        {
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "results.json");
            
            // Create summary
            var summary = new
            {
                ProcessedAt = DateTime.UtcNow,
                TotalRepositories = results.Count,
                SuccessfulRepositories = results.Count(r => r.Success),
                FailedRepositories = results.Count(r => !r.Success),
                TotalCommits = results.Where(r => r.Success).Sum(r => r.Commits.Count),
                TotalPRs = results.Where(r => r.Success).Sum(r => r.PullRequests.Count),
                Results = results
            };

            var json = JsonConvert.SerializeObject(summary, Formatting.Indented);
            await File.WriteAllTextAsync(outputPath, json);

            // Console output
            logger.LogInformation("=== SUMMARY ===");
            logger.LogInformation($"Total Repositories: {summary.TotalRepositories}");
            logger.LogInformation($"Successful: {summary.SuccessfulRepositories}");
            logger.LogInformation($"Failed: {summary.FailedRepositories}");
            logger.LogInformation($"Total Commits Found: {summary.TotalCommits}");
            logger.LogInformation($"Total PRs Found: {summary.TotalPRs}");
            logger.LogInformation($"Detailed results saved to: {outputPath}");

            foreach (var result in results)
            {
                if (result.Success)
                {
                    logger.LogInformation($"✅ {result.RepositoryName}: {result.Commits.Count} commits, {result.PullRequests.Count} PRs");
                    
                    if (result.PullRequests.Any())
                    {
                        logger.LogInformation("   Pull Requests:");
                        foreach (var pr in result.PullRequests.OrderBy(p => p.Number))
                        {
                            logger.LogInformation($"     - #{pr.Number}: {pr.Title} by {pr.Author}");
                        }
                    }
                }
                else
                {
                    logger.LogError($"❌ {result.RepositoryName}: {result.ErrorMessage}");
                }
                logger.LogInformation("");
            }
        }
    }
}