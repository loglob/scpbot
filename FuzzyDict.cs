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
		/// Levenshtein distance with branch pruning
		/// </summary>
		/// <param name="l">The original array</param>
		/// <param name="r">The edited array</param>
		/// <param name="curMin">The current known minimum</param>
		/// <param name="cur">The cost of reaching this state</param>
		/// <returns>The edit distance from l to r, or curMin, whichever is lower</returns>
		private int distance(ArraySegment<string> l, ArraySegment<string> r, int curMin = int.MaxValue, int cur = 0)
		{
			if(cur >= curMin
			 || (l.Count > r.Count && cur + (l.Count - r.Count) * deleteCost >= curMin)
			 || (l.Count < r.Count && cur + (r.Count - l.Count) * insertCost >= curMin))
			// prune this branch.
				return curMin;

			if(l.Count == 0)
				return cur + r.Count * insertCost;
			if(r.Count == 0)
				return cur + l.Count * deleteCost;
			if(l[0] == r[0])
				return distance(l.Slice(1), r.Slice(1), curMin, cur);

			if(replaceCost < deleteCost + insertCost)
				curMin = distance(l.Slice(1), r.Slice(1), curMin, cur + replaceCost);

			curMin = distance(l, r.Slice(1), curMin, cur + insertCost);

			curMin = distance(l.Slice(1), r, curMin, cur + deleteCost);

			return curMin;
		}

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