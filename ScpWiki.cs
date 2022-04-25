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
		internal const string wikiUrl = "https://scp-wiki.wikidot.com/";

		public class Entry
		{
			private const string baseUrl = wikiUrl + "scp-";

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
			private const string baseUrl = wikiUrl + "scp-series";

			public readonly int Number;

			public string Url
				=> Number == 1 ? baseUrl : $"{baseUrl}-{Number}";

			private HtmlDocument page;

			internal Series(int number)
			{
				Number = number;
				page = new HtmlDocument();
				page.Load(new HttpClient().GetStreamAsync(Url).Sync<Stream>());
			}

			public int TotalSeriesCount
			{
				get
				{
					var sreg = new Regex(@"/scp-series-([0-9]+)");

					try
					{
						return page.DocumentNode.SelectNodes(@"//div[@class='side-block']/div[@class='menu-item small'][1]/a")
							.Select(i => sreg.Match(i.Attributes["href"].Value))
							.Where(m => m.Success)
							.Select(m => int.Parse(m.Groups[1].Value))
							.Max();
					}
					catch(Exception ex)
					{
						throw new Exception("Failed to parse total series count", ex);
					}
				}
			}

			public IEnumerable<Entry> GetEntries()
			{
				// var page = new HtmlWeb().Load(Url);
				var ereg = new Regex(@"SCP-[0-9]{3,4} - .*");
				int lastnum = (Number == 1) ? 0 : (Number - 1) * 1000 - 1;

				foreach (var ul in page.DocumentNode.SelectNodes(
					"//div[@id='page-content']//div[@class='content-panel standalone series']/ul"))
				{
					foreach (var li in ul.ChildNodes)
					{
						var s = WebUtility.HtmlDecode(li.InnerText);

						if(string.IsNullOrWhiteSpace(s))
							continue;

						string[] parts = s.Split(" - ", 2);

						if(!ereg.Match(s).Success)
						{ // format screw entry
							yield return new Entry(parts.Length == 2 ? parts[1] : s, ++lastnum);
							continue;
						}

						if(parts[1] == "[ACCESS DENIED]")
						{
							lastnum++;
							continue;
						}

						yield return new Entry(parts[1], lastnum = int.Parse(parts[0].Substring(4)));
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
			// the first series is used to determine the total count of series
			var s1 = new Series(1);
			var results = new List<Entry>[s1.TotalSeriesCount];

			Console.WriteLine($"Loading {results.Length} series");

			if(!Parallel.For(0, results.Length,
				i => results[i] = (i == 0 ? s1 : new Series((int)i + 1))
					.GetEntries().ToList()).IsCompleted)
				throw new Exception("Failed to load at least one series!");

			var all = results.SelectMany(s => s);

			titles = new FuzzyDict<Entry>(conf.DeleteCost, conf.ReplaceCost, conf.InsertCost);

			foreach(var g in all.GroupBy(e => e.Number)
				.Where(g => g.Count() > 1))
			{
				Console.WriteLine($"Duplicates for number {g.Key}:");

				foreach (var i in g)
					Console.WriteLine($"{i.Title}");
			}

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
