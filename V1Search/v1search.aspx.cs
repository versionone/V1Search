using System;
using System.Data;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace V1Search {

	public partial class SearchPage : System.Web.UI.Page {
		
		private static string FILTER_PARAMETER = "find";
		private static string LUCENSE_INDEX_DIR = "Index";

		// these strings define the search result columns displayed to the user
		// they need to match the values expected on the page
		protected static string ID_COLUMN = "ID";
		protected static string NAME_COLUMN = "Name";
		protected static string URL_COLUMN = "AssetURL";
		protected static string IS_CLOSED_COLUMN = "IsClosed";

		// these strings defined the Lucene Field names that are used to populate the result set
		// they _must_ match the field names in the Lucene Index.
		private static string ID_FIELD_NAME = "ID";
		private static string NAME_FIELD_NAME = "Name";
		private static string DESCRIPTION_FIELD_NAME = "Description";
		private static string URL_FIELD_NAME = "URL";
		private static string IS_CLOSED_FIELD_NAME = "isClosed";

		// defines the maximum number of result items displayed on a page
		private static int MAX_ITEMS_PER_PAGE = 10;

		protected void Page_Load(object sender, EventArgs e) {
			// Press ButtonSearch on enter
			ClientScript.RegisterHiddenField("__EVENTTARGET", "SearchButton");

			if (!IsPostBack) {
				if(SearchTerms != null) {
					search();
					DataBind();
				}
			}
		}

		/// <summary>
		/// Action to take when user clicks the search button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void SearchButton_Click(object sender, EventArgs e) {
			Response.Redirect("v1search.aspx?" + FILTER_PARAMETER + "=" + QueryString.Text);
		}

		/// <summary>
		/// Extract search terms from the Request Parameters
		/// returns null if the parameter is not part of the URL
		/// </summary>
		protected string SearchTerms {
			get
			{
				string terms = Request.Params[FILTER_PARAMETER];
				return (String.Empty != terms) ? terms : null;
			}
		}

		private int firstItemOnPage = 0;
		private int lastItemOnPage  = 0;
		private int totalHits = 0;
		private int startIndex = 0;
		private TimeSpan searchTime;
		protected DataTable SearchResults = new DataTable();

		/// <summary>
		/// Search the Lucene Index to find document that meet the search terms specified by the user
		/// </summary>
		protected void search()
		{
			DateTime startTime = DateTime.Now;

			string indexDirectory = Server.MapPath(LUCENSE_INDEX_DIR);
			IndexSearcher searcher = new IndexSearcher(indexDirectory);

			// by default we search name and description
			MultiFieldQueryParser parser = new MultiFieldQueryParser(new string[] { NAME_FIELD_NAME, DESCRIPTION_FIELD_NAME }, new StandardAnalyzer());

			// parse the query, "text" is the default field to search
			Query query = parser.Parse(SearchTerms);

			SearchResults.Columns.Add(ID_COLUMN, typeof(string));
			SearchResults.Columns.Add(NAME_COLUMN, typeof(string));
			SearchResults.Columns.Add(URL_COLUMN, typeof(string));
			SearchResults.Columns.Add(IS_CLOSED_COLUMN, typeof(string));

			// Perform Lucense Search
			Hits hits = searcher.Search(query);
			totalHits = hits.Length();

			// index where we'll start processing results
			startIndex = CalculateStartIndex();

			// max number of documents we'll process 
			int maxResultsThisPage = (totalHits < (MAX_ITEMS_PER_PAGE + startIndex)) ? totalHits : (MAX_ITEMS_PER_PAGE + startIndex);

			// Create a DataRow from the Lucene document and update the results
			for (int i = startIndex; i < maxResultsThisPage; i++) {
				Document doc = hits.Doc(i);

				DataRow row = SearchResults.NewRow();
				row[ID_COLUMN] = doc.Get(ID_FIELD_NAME);
				row[NAME_COLUMN] = doc.Get(NAME_FIELD_NAME);
				row[URL_COLUMN] = doc.Get(URL_FIELD_NAME);
				if ("True".Equals(doc.Get(IS_CLOSED_FIELD_NAME)))
					row[IS_CLOSED_COLUMN] = "closed";
				SearchResults.Rows.Add(row);
			}
			searcher.Close();

			searchTime = DateTime.Now - startTime;
			firstItemOnPage = startIndex + 1;
			lastItemOnPage = ((startIndex + maxResultsThisPage) < totalHits) ? (startIndex + maxResultsThisPage) : totalHits;
		}

		/// <summary>
		/// Return search summary information
		/// </summary>
		protected string Summary {
			get {
				if (totalHits> 0)
					return "Results <b>" + firstItemOnPage + " - " + lastItemOnPage + "</b> of <b>" + totalHits + "</b> for <b>" + SearchTerms + "</b>. (" + searchTime.TotalSeconds + " seconds)";
				return "No results found";
			}
		}

		/// <summary>
		/// Paging Control
		/// </summary>
		protected DataTable Paging {
			get {
				// pageNumber starts at 1
				int pageNumber = (startIndex + MAX_ITEMS_PER_PAGE - 1) / MAX_ITEMS_PER_PAGE;

				DataTable dt = new DataTable();
				dt.Columns.Add("html", typeof(string));

				DataRow ar = dt.NewRow();
				ar["html"] = BuildPageHtmlLink(startIndex, pageNumber + 1, false);
				dt.Rows.Add(ar);

				int previousPagesCount = 4;
				for (int i = pageNumber - 1; i >= 0 && i >= pageNumber - previousPagesCount; i--) {
					int step = i - pageNumber;
					DataRow r = dt.NewRow();
					r["html"] = BuildPageHtmlLink(startIndex + (MAX_ITEMS_PER_PAGE * step), i + 1, true);
					dt.Rows.InsertAt(r, 0);
				}

				int nextPagesCount = 4;
				for (int i = pageNumber + 1; i <= ResultPageCount && i <= pageNumber + nextPagesCount; i++) {
					int step = i - pageNumber;
					DataRow r = dt.NewRow();
					r["html"] = BuildPageHtmlLink(startIndex + (MAX_ITEMS_PER_PAGE * step), i + 1, true);
					dt.Rows.Add(r);
				}

				DataRow gotoFront = dt.NewRow();
				gotoFront["html"] = "<a href=\"v1search.aspx?" + FILTER_PARAMETER + "=" + SearchTerms + "&startAt=0\"> &lt;&lt;</a>";
				dt.Rows.InsertAt(gotoFront, 0);

				DataRow gotoEnd = dt.NewRow();
				gotoEnd["html"] = "<a href=\"v1search.aspx?" + FILTER_PARAMETER + "=" + SearchTerms + "&startAt=" + LastPageStartIndex + "\"> &gt;&gt;</a>";
				dt.Rows.Add(gotoEnd);

				return dt;
			}
		}

		/// <summary>
		/// Prepares HTML of a paging item (bold number for current page, links for others).
		/// </summary>
		/// <param name="start">start item for the page</param>
		/// <param name="number">page number</param>
		/// <param name="active">is this the acive page</param>
		/// <returns></returns>
		private string BuildPageHtmlLink(int start, int number, bool active) {
			if (active)
				return "<a href=\"v1search.aspx?" + FILTER_PARAMETER + "=" + SearchTerms + "&startAt=" + start + "\">" + number + "</a>";
			else
				return "<b>" + number + "</b>";
		}

		/// <summary>
		/// Calcualte the starting point for returning documents
		/// </summary>
		/// <returns></returns>
		protected int CalculateStartIndex()
		{
			int rc = 0;
			try {
				rc = Convert.ToInt32(Request.Params["startAt"]);
				if (rc < 0)
					rc = 0;
				if (rc >= totalHits - 1) {
					rc = LastPageStartIndex;
				}
			}
			catch {}
			return rc;
		}

		/// <summary>
		/// Returns the number of pages in the query
		/// </summary>
		protected int ResultPageCount {
			get {
				return (totalHits - 1)/MAX_ITEMS_PER_PAGE;
			}
		}

		/// <summary>
		/// How many pages in the to display all the results
		/// </summary>
		protected int TotalPages {
			get {
				return (totalHits - 1)/MAX_ITEMS_PER_PAGE;
			}
		}

		/// <summary>
		/// Returns the index of the first item of the last page
		/// </summary>
		protected int LastPageStartIndex {
			get {
				return TotalPages * MAX_ITEMS_PER_PAGE;
			}
		}
	}
}
