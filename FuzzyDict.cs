using System;
using System.Collections.Generic;
using System.Linq;

namespace scpbot
{
	/// <summary>
	/// A dictionary that maps strings to elements, alowing for seaching, thus it's "fuzzy"
	/// </summary>
	/// <typeparam name="T">The type of Element mapped to</typeparam>
	public class FuzzyDict<T>
	{
#region Fields
		/// <summary>
		/// The cost of deletion form the search query
		/// </summary>
		private readonly int deleteCost;

		/// <summary>
		/// The cost of replaying a term of the search query
		/// </summary>
		private readonly int replaceCost;

		/// <summary>
		/// The cost of inserting a term into the search query
		/// </summary>
		private readonly int insertCost;

		/// <summary>
		/// Maps a word to all items that contain that word
		/// </summary>
		private Dictionary<string, List<(string[] key, T value)>> wordDict =
			new Dictionary<string, List<(string[], T)>>();

#endregion // Fields

		/// <summary>
		/// Constructs an empty fuzzy dictionary
		/// </summary>
		/// <param name="deleteCost">
		/// The cost of omitting a search term for the Levenshtein algorithm
		/// </param>
		/// <param name="replaceCost">
		/// The cost of replacing a search term for the Levenshtein algorithm
		/// </param>
		/// <param name="insertCost">
		/// The cost of adding an unrelated search term for the Levenshtein algorithm
		/// </param>
		public FuzzyDict(int deleteCost, int replaceCost, int insertCost)
		{
			this.deleteCost = deleteCost;
			this.replaceCost = replaceCost;
			this.insertCost = insertCost;
		}

#region Methods
		/// <summary>
		/// Transforms a search term into searchable form
		/// </summary>
		/// <param name="terms">A string of words separated by whitespace</param>
		/// <returns>The normalized word of the term</returns>
		private static string[] toSearchTerms(string terms)
			=> terms.ToLower().Split();

		/// <summary>
		/// Determines the edit distance from one array to another with offsets
		/// </summary>
		/// <param name="l">The original array</param>
		/// <param name="r">The edited array</param>
		/// <param name="l0">The current index in l</param>
		/// <param name="r0">The current index in r</param>
		/// <returns>The edit distance from l to r</returns>
		private int distance(string[] l, string[] r, int l0, int r0)
		{
			recurse:
			if(l0 >= l.Length)
				return (r.Length - r0) * insertCost;
			if(r0 >= r.Length)
				return (l.Length - l0) * deleteCost;
			if(l[l0] == r[r0])
			{
				l0++;
				r0++;
				goto recurse;
			}

			return Math.Min(
				distance(l,r,l0 + 1, r0) + deleteCost,
				Math.Min(
					distance(l,r,l0, r0 + 1) + insertCost,
					distance(l,r,l0 + 1, r0 + 1) + replaceCost));
		}

		/// <summary>
		/// Determines the edit distance from one array to another
		/// </summary>
		/// <param name="original">The original array</param>
		/// <param name="str">The edited array</param>
		/// <returns>The edit distance from original to str</returns>
		private int distance(string[] original, string[] str)
			=> distance(original, str, 0, 0);

		/// <summary>
		/// Searches for a search term.
		/// Returns a List of best matches.
		/// If there are any exact matches, returns exactly those, ignoring min and max.
		/// If there are only inexact matches, returns at least min and at most max items,
		/// 	provided there are enough matches.
		/// </summary>
		/// <param name="term">The search term</param>
		/// <param name="min">The minimum amount of inexact search results</param>
		/// <param name="max">The maximum amount of inexact search results</param>
		/// <returns>A list of matches</returns>
		public IEnumerable<T> Search(string term, int min, int max)
		{
			var terms = toSearchTerms(term);
			IEnumerable<(T item, string[] key, int dist)> f = terms
				.SelectMany(t => wordDict.TryGetValue(t, out var vals) ? vals : Enumerable.Empty<(string[] key,T item)>())
				.Distinct()
				.Select(e => (e.item, e.key, distance(terms, e.key)))
				.OrderBy(e => e.Item3);
			var exact = f.TakeWhile(i => i.dist == 0);

			if(exact.Any())
				return exact.Select(x => x.item).Take(max);

			int took = 0;
			int ld = 0;

			return f.TakeWhile(i => {
				bool r = took++ < min || ld == i.dist;
				ld = i.dist;
				return r;
			}).Select(i => i.item).Take(max);
		}

		/// <summary>
		/// Adds an element
		/// </summary>
		/// <param name="term">The full search term</param>
		/// <param name="item">The element</param>
		public void Add(string term, T item)
		{
			string[] words = toSearchTerms(term);
			var e = (words, item);

			foreach (var w in words)
			{
				if(wordDict.TryGetValue(w, out var entries))
					entries.Add(e);
				else
					wordDict[w] = new List<(string[], T)>{e};
			}
		}

		/// <summary>
		/// Adds every given entry
		/// </summary>
		/// <param name="entries">Enumeration of entries to add</param>
		public void Add(IEnumerable<(string term, T item)> entries)
		{
			foreach (var e in entries)
				Add(e.term, e.item);
		}
#endregion // Methods
	}
}