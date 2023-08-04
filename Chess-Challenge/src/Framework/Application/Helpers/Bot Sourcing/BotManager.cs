using System.IO;
using System.Collections.Generic;

namespace ChessChallenge.Application
{
    class BotManager
    {
        public static BotManager instance = new();

        public Dictionary<string, BotSource> bots;
        public Dictionary<string, BotSource> activeBots = new();

        public bool HasBot(string name)
        {
            return bots.ContainsKey(name);
        }
        public void AddBot(BotSource bot)
        {
            bots.Add(bot.hash, bot);
        }
        public void SetBot(BotSource bot)
        {
            bots[bot.hash] = bot;
        }

        public BotSource this[string hash]
        {
            get => bots[hash];
            set => bots[hash] = value;
        }

        public bool IsActive(string hash)
        {
            return activeBots.ContainsKey(hash);
        }
        public void ActivateBot(string hash)
        {
            activeBots.Add(hash, bots[hash]);
        }
        public void DeactivateBot(string hash)
        {
            activeBots.Remove(hash);
        }

        public List<BotSource> GetInactiveBots()
        {
            List<BotSource> inactiveBots = new();
            foreach (BotSource bot in bots.Values)
            {
                if (!IsActive(bot.hash))
                {
                    inactiveBots.Add(bot);
                }
            }
            return inactiveBots;
        }
        public List<BotSource> GetActiveBots()
        {
            return new(activeBots.Values);
        }

        private BotManager()
        {
            LoadBots();
        }

        private void LoadBots()
        {
            bots = new();
            foreach (string file in Directory.GetFiles(FileHelper.BotDirectory))
            {
                if (file.EndsWith(".cs"))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    AddBot(new BotSource(name.ToLower()));
                }
            }
        }
    }
}