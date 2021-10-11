using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using static System.Console;
using static System.String;

namespace GitClearBranches
{
    internal static class Program
    {
        private static Task<int> Main(string[] whitelisted)
        {
            if (whitelisted.Length == 0)
            {
                whitelisted = new[] { "master", "main", "dev", "develop", "prod" };
            }
            
            var repositoryPath = ResolveRepositoryPath();
            if (IsNullOrEmpty(repositoryPath))
            {
                WriteLine("fatal: Not a git repository");
                return Task.FromResult(-1);
            }

            var pathToGit = ResolveGitPath();
            if (IsNullOrEmpty(pathToGit))
            {
                WriteLine("fatal: git is not in your path");
                return Task.FromResult(-2);
            }

            using var repository = new Repository(repositoryPath);
            
            var headRef = repository.Refs.Where(r => r.CanonicalName == repository.Head.CanonicalName);

            var notTrackedMergedLocalBranches = repository.Branches.Where(
                b=>
                    !b.IsTracking && 
                    !b.IsRemote &&
                    b.FriendlyName != repository.Head.FriendlyName &&
                    repository.Refs.ReachableFrom(headRef, new[] { b.Tip }).Any() && // merged
                    !whitelisted.Contains(b.FriendlyName) 
            ).ToList();
            
            foreach (var branch in notTrackedMergedLocalBranches)
            {
                repository.Branches.Remove(branch);
                WriteLine(branch.FriendlyName);
            }
            
            return Task.FromResult(0);
        }

        private static string ResolveRepositoryPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var repositoryPath = Repository.Discover(currentDirectory);

            if (IsNullOrEmpty(repositoryPath))
                return Empty;

            return Repository.IsValid(repositoryPath) 
                ? repositoryPath[..repositoryPath.LastIndexOf("/.git/", StringComparison.Ordinal)]
                : Empty;
        }

        private static string ResolveGitPath()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (IsNullOrEmpty(path))
                return Empty;

            var paths = path.Split(Path.PathSeparator);
            
            var fileNames = new[] { "git.exe", "git" };
            var searchPaths = paths.SelectMany(p => fileNames.Select(f => Path.Combine(p, f)));

            return searchPaths.FirstOrDefault(File.Exists) ?? Empty;
        }
    }
}
