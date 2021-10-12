using System;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using LibGit2Sharp;
using static System.Console;
using static System.String;

namespace GitClearBranches
{
    internal static class Program
    {
        private static ConsoleColor _defaultConsoleColor;
        private static Task<int> Main(string[] whitelisted)
        {
            _defaultConsoleColor = ForegroundColor;

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
                b =>
                    // !b.IsTracking && 
                    !b.IsRemote &&
                    (!b.IsTracking || b.TrackingDetails.CommonAncestor is null) &&
                    b.FriendlyName != repository.Head.FriendlyName &&
                    repository.Refs.ReachableFrom(headRef, new[] { b.Tip }).Any() && // merged
                    !whitelisted.Contains(b.FriendlyName)
            ).ToList();

            if (!notTrackedMergedLocalBranches.Any())
                return Task.FromResult(0);

            if (whitelisted.Contains("--yes") || whitelisted.Contains("-y"))
            {
                notTrackedMergedLocalBranches.ForEach(repository.Branches.Remove);
                return Task.FromResult(0);
            }

            WriteLine("Branches to be removed: ");
            WriteLine();
            var indexer = 0;
            notTrackedMergedLocalBranches.ForEach((b) =>
            {
                Write(b.FriendlyName);
                Write("\t");
                if(indexer++ % 5 == 0) WriteLine();
            });
            WriteLine("--------------------------------------------");
            WriteLine();
            ForegroundColor = ConsoleColor.Cyan;
            Write("Are you sure you wanna delete those branches (Y/n) ?  ");
            var answer = ReadLine();
            ForegroundColor = _defaultConsoleColor;

            if (IsNullOrEmpty(answer) || answer == "Y")
                notTrackedMergedLocalBranches.ForEach(repository.Branches.Remove);

            return Task.FromResult(0);
        }

        private static string ResolveRepositoryPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var repositoryPath = Repository.Discover(currentDirectory);

            if (IsNullOrEmpty(repositoryPath))
                return Empty;

            var index = repositoryPath.LastIndexOf("/.git/", StringComparison.Ordinal);
            if(index == -1)
                index = repositoryPath.LastIndexOf("\\.git\\", StringComparison.Ordinal);

            return Repository.IsValid(repositoryPath)
                ? repositoryPath[..index]
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
