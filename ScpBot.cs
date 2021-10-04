using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System.Linq;
using System.Text.RegularExpressions;

/* This async method lacks 'await' operators and will run synchronously */
#pragma warning disable 1998

namespace scpbot
{
	/// <summary>
	/// Contains the bot code
	/// </summary>
	class ScpBot
	{
		/// <summary>
		/// Matches messages that request a wiki lookup
		/// </summary>
		private static readonly Regex trigger =
			new Regex(@"(^|\W)scp(-|$|\W)", RegexOptions.IgnoreCase);

		/// <summary>
		/// Matches numbers for looking up entry numbers
		/// </summary>
		private static readonly Regex numberFinder = new Regex(@"[0-9]+");

		/// <summary>
		/// Matches an escaped search string
		/// </summary>
		private static readonly Regex searchFinder = new Regex("(\".*?\"|'.*?')");

		/// <summary>
		/// The bot client for interacting with discord
		/// </summary>
		private readonly DiscordSocketClient client;

		/// <summary>
		/// The loaded SCP wiki entries
		/// </summary>
		private readonly ScpWiki wiki;

		public ScpBot(ScpWiki wiki)
		{
			this.wiki = wiki;
			client = new DiscordSocketClient();

			client.Log += async (str) => Console.WriteLine(str);
			client.MessageReceived += OnMessageReceived;
		}

		protected async Task OnMessageReceived(SocketMessage msg)
		{
			if(msg.Author.IsBot)
				return;

			// listen for trigger phrase, or direct mention
			if(!trigger.IsMatch(msg.Content)
				&& msg.MentionedUsers.All(u => u.Id != this.client.CurrentUser.Id))
				return;

			var numbers = numberFinder.Matches(msg.Content)
				.Select(m => wiki.GetEntry(int.Parse(m.Value)));

			var search = searchFinder.Matches(msg.Content)
				.Select(m => m.Value.LRSubstring(1,1))
				.SelectMany(s =>
					wiki.SearchTitle(s)
					.Select(e => e.ToString())
					.Or(new string[]{ "No results found." }, s => !s.Any())
					.Prepend($"> {s}:"));

			var res = numbers.Select(e => e.ToString()).Concat(search);

			if(res.Any())
				await msg.Channel.SendMessageAsync(string.Join("\n", res));
		}

		public async Task Login(string token)
		{
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();
		}

		public async Task Logout()
		{
			await client.LogoutAsync();
		}
	}
}