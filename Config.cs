
namespace scpbot
{
	/// <summary>
	/// Encapsulates the configuration of the bot
	/// </summary>
	class Config
	{
		/// <summary>
		/// The minimum amount of search results to display
		/// </summary>
		public int MinSearchResults { get; set; }
		/// <summary>
		/// The maximum amount of search results to display
		/// </summary>
		public int MaxSearchResults { get; set; }

		/// <summary>
		/// The cost of omitting a search term
		/// </summary>
		public int DeleteCost { get; set; }

		/// <summary>
		/// The cost of including an unrelated search term
		/// </summary>
		public int InsertCost { get; set; }

		/// <summary>
		/// The cost of replacing a search term
		/// </summary>
		public int ReplaceCost { get; set; }
	}
}