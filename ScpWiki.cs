using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.IO;
using System;

namespace scpbot
{
	class ScpWiki
	{
		public class Entry
		{
			private const string baseUrl = "http://www.scpwiki.com/scp-";

			public string Title { get; }
			public int Number { get; }

			public string Url
				=> $"{baseUrl}{Number:000}";

			public override string ToString()
				=> $"{Url}\t{Title}";

			internal Entry(string title, int number)
			{
				this.Number = number;
				this.Title = title;
			}
		}

		private class Series
		{
			private const string baseUrl = "http://www.scpwiki.com/scp-series";

			// The amount of series there are
			public const int Count = 6;
			public readonly int Number;

			public string Url
				=> Number == 1 ? baseUrl : $"{baseUrl}-{Number}";

			internal Series(int number)
			{
				Number = number;
			}

			public IEnumerable<Entry> GetEntries()
			{
				// var page = new HtmlWeb().Load(Url);
				var page = new HtmlDocument();
				page.Load(new HttpClient().GetStreamAsync(Url).Sync<Stream>());
				var ereg = new Regex(@"SCP-[0-9]{3,4} - .*");

				foreach (var ul in page.DocumentNode.SelectNodes(
					"//div[@id='page-content']//div[@class='content-panel standalone series']/ul")
					.Skip(1))
				{
					foreach (var li in ul.ChildNodes)
					{
						var s = WebUtility.HtmlDecode(li.InnerText);

						if(!ereg.Match(s).Success)
							continue;

						string[] parts = s.Split(" - ", 2);

						if(parts[1] == "[ACCESS DENIED]")
							continue;

						yield return new Entry(parts[1], int.Parse(parts[0].Substring(4)));
					}
				}
			}
		}

		/// <summary>
		/// The program configuration
		/// </summary>
		private readonly Config conf;

		/// <summary>
		/// Maps titles onto entries
		/// </summary>
		private readonly FuzzyDict<Entry> titles;

		/// <summary>
		/// Maps entry numbers onto entries
		/// </summary>
		private readonly Dictionary<int, Entry> numbers;

		public ScpWiki(Config conf)
		{
			this.conf = conf;
			var results = new List<Entry>[conf.SeriesCount];

			if(!Parallel.For(0, results.Length,
				i => results[i] = new Series((int)i + 1).GetEntries().ToList()).IsCompleted)
				throw new Exception("Failed to load at least one series!");

			var all = results.SelectMany(s => s);

			titles = new FuzzyDict<Entry>(conf.DeleteCost, conf.ReplaceCost, conf.InsertCost);

			titles.Add(all.Select(e => (e.Title, e)));
			numbers = all.ToDictionary(e => e.Number);
		}

		public IEnumerable<Entry> SearchTitle(string title)
			=> titles.Search(title, conf.MinSearchResults, conf.MaxSearchResults);
		public Entry GetEntry(int number)
			=> numbers[number];

		public IEnumerable<Entry> AllEntries
			=> numbers.Values;
	}
}
