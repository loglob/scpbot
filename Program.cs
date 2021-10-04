using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace scpbot
{
	static class Program
	{
		static ScpBot bot;

		static void Main(string[] args)
		{
			var conf = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
			Console.WriteLine("Loading SCP wiki...");
			var wiki = new ScpWiki(conf);
			Console.WriteLine("Done.");
			bot = new ScpBot(wiki);

			// Hooks SIGTERM and SIGINT ()
			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
			Console.CancelKeyPress += OnProcessExit;

			var token = File.ReadAllText("token").Trim();
			bot.Login(token).Sync();
			Thread.Sleep(Timeout.Infinite);
		}

		static void OnProcessExit(object sender, EventArgs e)
		{
			bot.Logout().Sync();
			Environment.ExitCode = 0;
		}
	}
}
