using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace scpbot
{
	static class Program
	{
		static ScpBot bot;

		static void debug(ScpWiki w)
		{
			while(true)
			{
				Console.Write("query> ");
				string q = Console.ReadLine();

				if(q.Length == 0)
					return;

				try
				{
					bool count = q[0] == '#';

					if(count)
						q = q.Substring(1);

					var s = q.Split("..");

					var e =
						(s.Length == 2 && int.TryParse(s[0], out int a) && int.TryParse(s[1], out int b))
							? w.AllEntries.Where(x => x.Number >= a && x.Number <= b).ToArray()
							: int.TryParse(q, out int n)
								? new[]{ w.GetEntry(n) }
								: w.SearchTitle(q).ToArray();

					Console.WriteLine($"Found {e.Length} matches");

					if(!count) foreach (var x in e)
						Console.WriteLine($"{x.Number}: '{x.Title}' @{x.Url}");
				}
				catch(Exception ex)
				{
					Console.WriteLine(ex);
				}
			}
		}

		static void Main(string[] args)
		{
			var conf = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
			Console.WriteLine("Loading SCP wiki...");
			var wiki = new ScpWiki(conf);
			Console.WriteLine("Done.");

			if(args.Length == 1 && args[0] == "debug")
			{
				debug(wiki);
				return;
			}

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
