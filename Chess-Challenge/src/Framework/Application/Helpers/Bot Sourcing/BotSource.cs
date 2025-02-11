using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using ChessChallenge.API;

namespace ChessChallenge.Application
{
    class BotInfo
    {
        public string Name { get; set; }
        public string Identifier { get; set; }
        public string Description { get; set; }
    }
    class BotSource
    {
        private static readonly object buildLock = new();

        private BotInfo? info;
        private string? code;
        private int? totalTokenCount;
        private int? debugTokenCount;
        private Type? chessBotType = null;
        public readonly string hash;

        private string BasePath { get => $"{FileHelper.BotDirectory}/{hash.ToUpper()}"; }
        private string CodePath { get => $"{BasePath}.cs"; }
        private string DllPath { get => $"{BasePath}.dll"; }
        private string InfoPath { get => $"{BasePath}.json"; }

        public BotInfo Info
        {
            get
            {
                if (info == null)
                {
                    info = JsonSerializer.Deserialize<BotInfo>(File.ReadAllText(InfoPath)) ??
                        throw new Exception("Could not deserialize bot info");
                }
                return info;
            }
        }
        public string Code
        {
            get
            {
                if (code == null)
                {
                    code = File.ReadAllText(CodePath);
                }
                return code;
            }
        }
        public int TotalTokenCount
        {
            get
            {
                if (totalTokenCount == null)
                {
                    (totalTokenCount, debugTokenCount) = TokenCounter.CountTokens(Code);
                }
                return totalTokenCount.Value;
            }
        }
        public int DebugTokenCount
        {
            get
            {
                if (debugTokenCount == null)
                {
                    (totalTokenCount, debugTokenCount) = TokenCounter.CountTokens(Code);
                }
                return debugTokenCount.Value;
            }
        }

        public BotSource(string code, string identifier, string name)
        {
            info = new BotInfo { Name = name, Identifier = identifier, Description = "" };
            this.code = code;
            hash = SyntaxTreeHashGenerator.GenerateHash(this.code);
            Directory.CreateDirectory(FileHelper.BotDirectory);
            File.WriteAllText(CodePath, this.code);
            SaveInfo();
        }
        public BotSource(SourceDescription sourceDescription, string name)
            : this(sourceDescription.GetFileContents(), sourceDescription.GetShortIdentifier(), name) { }
        public BotSource(SourceDescription sourceDescription)
            : this(sourceDescription, sourceDescription.GetFileName()) { }

        public BotSource(string hash)
        {
            this.hash = hash;
        }


        public async Task<IChessBot> CreateBot()
        {
            Type type = await Task.Run(() => LoadBot());
            IChessBot chessBot =
                (IChessBot?)Activator.CreateInstance(type) ??
                throw new Exception("Could not create instance of bot");
            return chessBot;
        }
        private bool DllExists() => File.Exists(DllPath);
        private Type LoadBot()
        {
            lock (buildLock)
            {
                if (chessBotType != null)
                {
                    return chessBotType;
                }
                if (!DllExists())
                {
                    BuildBot();
                }
                Console.WriteLine($"Loading bot from {DllPath}");
                Assembly assembly = Assembly.LoadFrom(DllPath);
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IChessBot).IsAssignableFrom(type))
                    {
                        chessBotType = type;
                        return type;
                    }
                }
                throw new Exception("Could not find IChessBot in assembly");
            }
        }
        private void BuildBot()
        {
            string buildDirectory = $"{Directory.GetCurrentDirectory()}/build-chessbot";
            Directory.CreateDirectory(buildDirectory);
            File.WriteAllText($"{buildDirectory}/MyBot.cs", Code);
            File.WriteAllText($"{buildDirectory}/Chess-Bot.csproj", $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputRoot>.\bin</OutputRoot>
    <TargetFramework>net6.0</TargetFramework>
    <AssemblyName>{hash.ToUpper()}</AssemblyName>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\Chess-Challenge.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <None Include=""..\..\.editorconfig"" Link="".editorconfig"" />
  </ItemGroup>

</Project>
");
            string output = RunDotnet(buildDirectory, "build");
            File.Delete($"{buildDirectory}/MyBot.cs");
            File.Delete($"{buildDirectory}/Chess-Bot.csproj");
            if (output.Contains("Build FAILED"))
            {
                throw new Exception("Build failed");
            }
            string buildDllPath = $"{buildDirectory}/bin/Debug/net6.0/{hash.ToUpper()}.dll";
            File.Move(buildDllPath, DllPath);
        }

        private void SaveInfo()
        {
            string json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(InfoPath, json);
        }

        private static string RunDotnet(string cwd, string arguments)
        {
            Process git = new();
            git.StartInfo.WorkingDirectory = cwd;
            git.StartInfo.FileName = "dotnet";
            git.StartInfo.Arguments = arguments;
            git.StartInfo.UseShellExecute = false;
            git.StartInfo.RedirectStandardOutput = true;
            git.StartInfo.RedirectStandardError = true;
            git.Start();
            string error = git.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Error output of dotnet -----------------\n{error}\n----------------------------------------");
            }
            string output = git.StandardOutput.ReadToEnd();
            git.WaitForExit();
            return output;
        }
    }
}