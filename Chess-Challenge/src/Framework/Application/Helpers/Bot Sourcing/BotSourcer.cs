using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChessChallenge.Application
{
    record SourceOrSourcePathDescription(string Path);

    abstract record SourceDescription(string Path) : SourceOrSourcePathDescription(Path)
    {
        public abstract string GetFileName();
        public abstract string GetFileContents();
        public virtual string GetShortIdentifier() => BotSourcer.ClampStringEnd(Path, 20);
    }
    record FileSourceDescription(string Path) : SourceDescription(Path)
    {
        public override string GetFileName() => System.IO.Path.GetFileName(Path);
        public override string GetFileContents() => File.ReadAllText(Path);
    }
    record GitCommitFileSourceDescription(string Path, string CommitHash, string CommitMessage, string GitPath) : SourceDescription(Path)
    {
        public override string GetFileName() => System.IO.Path.GetFileName(GitPath);
        public override string GetFileContents() => BotSourcer.ReadFileFromRepository(Path, CommitHash, GitPath);
        public override string ToString() => base.ToString() + "@" + CommitHash[..8];
        public override string GetShortIdentifier()
            => base.GetShortIdentifier() +
            "#" + CommitHash[..8] + " " + BotSourcer.ClampStringStart(CommitMessage, 40) +
            ":" + BotSourcer.ClampStringEnd(GitPath, 20);
    }
    record SourcePathDescription(string Path) : SourceOrSourcePathDescription(Path)
    {
        public override string ToString() => BotSourcer.ClampString(Path, 60);
    }
    record GitRepositorySourcePathDescription(string Path) : SourcePathDescription(Path)
    {
        public override string ToString() => $"{base.ToString()}@";
    }
    record GitBranchSourcePathDescription(string Path, string Branch) : GitRepositorySourcePathDescription(Path)
    {
        public override string ToString() => $"{base.ToString()}{BotSourcer.ClampString(Branch, 20)}";
    }
    record GitCommitSourcePathDescription(string Path, string Branch, string CommitHash, string CommitMessage) : GitBranchSourcePathDescription(Path, Branch)
    {
        public override string ToString() => $"{base.ToString()}#{CommitHash[..8]}:{BotSourcer.ClampStringStart(CommitMessage, 40)}";
    }


    class BotSourcer
    {
        public static string RunGit(string cwd, string arguments)
        {
            Process git = new();
            git.StartInfo.WorkingDirectory = cwd;
            git.StartInfo.FileName = "git";
            git.StartInfo.Arguments = arguments;
            git.StartInfo.UseShellExecute = false;
            git.StartInfo.RedirectStandardOutput = true;
            git.Start();
            string output = git.StandardOutput.ReadToEnd();
            git.WaitForExit();
            return output;
        }

        public static string ClampStringEnd(string str, int maxLength)
        {
            if (str.Length <= maxLength)
            {
                return str;
            }
            return "..." + str[(str.Length - maxLength + 3)..];
        }
        public static string ClampStringStart(string str, int maxLength)
        {
            if (str.Length <= maxLength)
            {
                return str;
            }
            return str[..(maxLength - 3)] + "...";
        }
        public static string ClampString(string str, int maxLength)
        {
            if (str.Length <= maxLength)
            {
                return str;
            }
            return str[..(maxLength / 2 - 2)] + "..." + str[(str.Length - maxLength / 2 + 1)..];
        }

        public static string ReadFileFromRepository(string repositoryPath, string commitHash, string filePath)
        {
            return RunGit(repositoryPath, $"show \"{commitHash}:{filePath}\"");
        }

        public static bool MightBeBotScript(string code)
        {
            return Regex.IsMatch(code, @"class\s+\w+\s*:\s*(?:[\w.]+\.)?IChessBot");
        }


        SourcePathDescription sourcePathDescription = new(Directory.GetCurrentDirectory());
        Dictionary<string, SourceOrSourcePathDescription> options;

        public BotSourcer()
        {
            options = GetOptionsDict();
        }

        public string GetPath()
        {
            return sourcePathDescription.ToString();
        }

        private Dictionary<string, SourceOrSourcePathDescription> GetOptionsDict()
        {
            Dictionary<string, SourceOrSourcePathDescription> options = new();
            switch (sourcePathDescription)
            {
                case GitCommitSourcePathDescription gitCommitSourcePathDescription:
                    {
                        string output = RunGit($"ls-tree -r --name-only {gitCommitSourcePathDescription.CommitHash}");
                        string[] files = output.Split('\n');
                        foreach (string file in files)
                        {
                            if (!file.EndsWith(".cs"))
                            {
                                continue;
                            }
                            string code = ReadFileFromRepository(
                                gitCommitSourcePathDescription.Path,
                                gitCommitSourcePathDescription.CommitHash,
                                file
                            );
                            if (!MightBeBotScript(code))
                            {
                                continue;
                            }
                            options.Add(file, new GitCommitFileSourceDescription(
                                gitCommitSourcePathDescription.Path,
                                gitCommitSourcePathDescription.CommitHash,
                                gitCommitSourcePathDescription.CommitMessage,
                                file
                            ));
                        }
                        return options;
                    }
                case GitBranchSourcePathDescription gitBranchSourcePathDescription:
                    {
                        string output = RunGit($"log --pretty=format:%H_%s {gitBranchSourcePathDescription.Branch}");
                        string[] commits = output.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string commit in commits)
                        {
                            string[] parts = commit.Split(new[] { '_' }, 2);
                            string hash = parts[0];
                            string message = parts[1];
                            options.Add(hash[..8] + " " + ClampStringStart(message, 70), new GitCommitSourcePathDescription(
                                gitBranchSourcePathDescription.Path,
                                gitBranchSourcePathDescription.Branch,
                                hash,
                                message
                            ));
                        }
                        return options;
                    }
                case GitRepositorySourcePathDescription gitRepositorySourcePathDescription:
                    {
                        options.Add("working tree", new SourcePathDescription(
                            gitRepositorySourcePathDescription.Path
                        ));
                        string output = RunGit("branch");
                        string[] branches = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string branch in branches)
                        {
                            options.Add(branch[2..], new GitBranchSourcePathDescription(
                                gitRepositorySourcePathDescription.Path,
                                branch[2..]
                            ));
                        }
                        return options;
                    }
                case SourcePathDescription:
                    return GetDirectoryOptions();
                default:
                    throw new Exception("Invalid source path description");
            }
        }

        private Dictionary<string, SourceOrSourcePathDescription> GetDirectoryOptions()
        {
            Dictionary<string, SourceOrSourcePathDescription> options = new();
            foreach (var file in Directory.EnumerateFiles(sourcePathDescription.Path))
            {
                if (!file.EndsWith(".cs"))
                {
                    continue;
                }
                string code = File.ReadAllText(file);
                if (!MightBeBotScript(code))
                {
                    continue;
                }
                string fileName = Path.GetFileName(file);
                options.Add(fileName, new FileSourceDescription(file));
            }
            foreach (var directory in Directory.EnumerateDirectories(sourcePathDescription.Path))
            {
                string directoryName = Path.GetFileName(directory);
                if (Directory.Exists(Path.Combine(directory, ".git")))
                {
                    options.Add(directoryName, new GitRepositorySourcePathDescription(directory));
                }
                else
                {
                    options.Add(directoryName, new SourcePathDescription(directory));
                }
            }
            return options;
        }

        private string RunGit(string arguments)
        {
            return RunGit(sourcePathDescription.Path, arguments);
        }

        public List<string> GetOptions()
        {
            return new List<string>(options.Keys);
        }

        public void GoUp()
        {
            switch (sourcePathDescription)
            {
                case GitCommitSourcePathDescription gitCommitSourcePathDescription:
                    sourcePathDescription = new GitBranchSourcePathDescription(
                        gitCommitSourcePathDescription.Path,
                        gitCommitSourcePathDescription.Branch
                    );
                    break;
                case GitBranchSourcePathDescription gitBranchSourcePathDescription:
                    sourcePathDescription = new GitRepositorySourcePathDescription(
                        gitBranchSourcePathDescription.Path
                    );
                    break;
                case GitRepositorySourcePathDescription gitRepositorySourcePathDescription:
                    sourcePathDescription = new SourcePathDescription(
                        Directory.GetParent(gitRepositorySourcePathDescription.Path).FullName
                    );
                    break;
                case SourcePathDescription rawSourcePathDescription:
                    bool isGitRepository = Directory.Exists(Path.Combine(rawSourcePathDescription.Path, ".git"));
                    if (isGitRepository)
                    {
                        sourcePathDescription = new GitRepositorySourcePathDescription(
                            rawSourcePathDescription.Path
                        );
                    }
                    else
                    {
                        if (rawSourcePathDescription.Path == Directory.GetDirectoryRoot(rawSourcePathDescription.Path))
                        {
                            return;
                        }
                        sourcePathDescription = new SourcePathDescription(
                            Directory.GetParent(rawSourcePathDescription.Path).FullName
                        );
                    }
                    break;
                default:
                    throw new Exception("Invalid source path description");
            }
            options = GetOptionsDict();
        }

        public bool SelectOption(string option, out SourceDescription? source)
        {
            SourceOrSourcePathDescription selected = options[option];
            if (selected is SourcePathDescription description)
            {
                sourcePathDescription = description;
                options = GetOptionsDict();
            }
            else
            {
                source = (SourceDescription)selected;
                return true;
            }
            source = null;
            return false;
        }
    }
}