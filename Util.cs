using System;
using System.Linq;
using System.Threading.Tasks;

namespace scpbot
{
	static class Util
	{
		public static void Sync(this Task asyncFunc)
			=> asyncFunc.GetAwaiter().GetResult();

		public static T Sync<T>(this Task<T> asyncFunc)
			=> asyncFunc.GetAwaiter().GetResult();

		public static string LRSubstring(this string s, int l, int r)
			=> s.Substring(l, s.Length - l - r);

		public static T Or<T>(this T s, T alt, Func<T, bool> cond)
			=> cond(s) ? alt : s;
	}
}