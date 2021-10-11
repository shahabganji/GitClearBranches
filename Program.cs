using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using static System.Console;
using static System.String;

namespace DotnetGlobalToolGitClearBranches
{
    internal static class Program
    {
        private static Task<int> Main(string[] whitelisted)
        {
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
            
            /**
             * git branch --merged | %{$_.trim()}  | ?{$_ -notmatch 'dev' -and $_ -notmatch 'master'} | %{git branch -d $_}
             */
            
            WriteLine(repositoryPath);
            var repository = new Repository(repositoryPath);

            var notTrackingBranches = repository.Branches.Where(
                b=>!b.IsTracking && !whitelisted.Contains(b.FriendlyName) ).ToList();
            
            foreach (var branch in notTrackingBranches)
            {
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
