using System;
using System.Configuration;
using System.Text;
using VersionOne.SDK.ObjectModel;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

/// The V1Extractor is responsible for reading data from a VersionOne instance
/// and creating the Lucene Index
namespace V1Extractor {

	class Program {

		private string _v1Url;						// URL to V1 instance 
		private V1Instance _v1Server;				// Connection to V1 server
		private IndexWriter _luceneIndexWriter;		// Lucense Index writer

		/// <summary>
		/// Main entry point for exrtractor
		/// </summary>
		static void Main() {

			DateTime start = DateTime.Now;
			Program extractor = new Program();
			extractor.connect();
			extractor.run();
			TimeSpan duration = DateTime.Now - start;
			Console.WriteLine("\n\nExtraction took " + duration.TotalSeconds + " seconds");
		}

		/// <summary>
		/// Connect to each system
		/// </summary>
		private void connect()
		{
			V1Connect();
			LuceneConnect();
		}

		/// <summary>
		/// Run the extraction process
		/// </summary>
		private void run()
		{
			foreach(Project p in _v1Server.Projects)
			{
				ProcessProject(p);
			}
			_luceneIndexWriter.Optimize();
			_luceneIndexWriter.Close();
		}

		/// <summary>
		/// Extract data from a project
		/// </summary>
		/// <param name="project">VersionOne project object to extract</param>
		private void ProcessProject(Project project)
		{
			Console.WriteLine("Processing project " + project.Name);

			foreach (Story story in project.GetStories(null))
				Index(story);

			foreach (Defect defect in project.GetDefects(null))
				Index(defect);

			foreach (Request request in project.GetRequests(null))
				Index(request);

			foreach (Issue issue in project.GetIssues(null))
				Index(issue);

			foreach (Project childProject in project.GetChildProjects(null))
				ProcessProject(childProject);
		}

		/// <summary>
		/// Index a story 
		/// </summary>
		/// <param name="story">the story to process</param>
		private void Index(Story story)
		{
			Document doc = IndexCommonFields(story);
			doc.Add(UnStored("Build", story.Build));
			if(null != story.Customer)
				doc.Add(UnStored("Customer", story.Customer.Name));
			doc.Add(UnStored("RequestedBy", story.RequestedBy));
			if(null != story.Risk)
				doc.Add(UnStored("Risk", story.Risk.CurrentValue));
			if(null != story.Type)
				doc.Add(UnStored("Type", story.Type.CurrentValue));
			_luceneIndexWriter.AddDocument(doc);
		}

		/// <summary>
		/// Index a defect
		/// </summary>
		/// <param name="defect">defect to index</param>
		private void Index(Defect defect) {
			Document doc = IndexCommonFields(defect);
			if (null != defect.Type)
				doc.Add(UnStored("Type", defect.Type.CurrentValue));

			doc.Add(UnStored("Environment", defect.Environment));
			doc.Add(UnStored("FixedInBuild", defect.ResolvedInBuild));
			doc.Add(UnStored("FoundBy", defect.FoundBy));
			doc.Add(UnStored("FoundInBuild", defect.FoundInBuild));
			doc.Add(UnStored("FoundInVersion", defect.FoundInVersion));

			doc.Add(UnStored("ResolutionDetails", defect.ResolutionDetails));
			doc.Add(UnStored("ResolutionReason", defect.ResolutionReason.CurrentValue));
			doc.Add(UnStored("ResolvedInBuild", defect.ResolvedInBuild));

			if(null != defect.VerifiedBy)
				doc.Add(UnStored("VerifiedBy", defect.VerifiedBy.Name));

			_luceneIndexWriter.AddDocument(doc);
		}

		/// <summary>
		/// Index a request
		/// </summary>
		/// <param name="request">request to index</param>
		private void Index(Request request)
		{
			Document doc = CreateDocument(request);
			if(null != request.Owner)
			{
				doc.Add(UnStored("Owner", request.Owner.Name));
				doc.Add(UnStored("OwnerId", request.Owner.Username));				
			}

			doc.Add(UnStored("Reference", request.Reference));
			if (null != request.Status) 
				doc.Add(UnStored("Status", request.Status.CurrentValue));
			if (null != request.Priority)
				doc.Add(UnStored("Priority", request.Priority.CurrentValue));
			if (null != request.Source)
				doc.Add(UnStored("Source", request.Source.CurrentValue));

			doc.Add(UnStored("RequestedBy", request.RequestedBy));
			doc.Add(UnStored("ResolutionDetails", request.ResolutionDetails));
			doc.Add(UnStored("ResolutionReason", request.ResolutionReason.CurrentValue));

			if(null != request.Type)
				doc.Add(UnStored("Type", request.Type.CurrentValue));

			_luceneIndexWriter.AddDocument(doc);
		}

		/// <summary>
		/// Index an issue
		/// </summary>
		/// <param name="issue">issue to index</param>
		private void Index(Issue issue)
		{
			Document doc = CreateDocument(issue);
			if(null != issue.Owner)
			{
				doc.Add(UnStored("Owner", issue.Owner.Name));
				doc.Add(UnStored("OwnerId", issue.Owner.Username));				
			}

			doc.Add(UnStored("Reference", issue.Reference));
			if(null != issue.Priority)
				doc.Add(UnStored("Priority", issue.Priority.CurrentValue));
			if(null != issue.IdentifiedBy)
				doc.Add(UnStored("Source", issue.Source.CurrentValue));

			doc.Add(UnStored("IdentifiedBy", issue.IdentifiedBy));

			doc.Add(UnStored("ResolutionDetails", issue.ResolutionDetails));
			if(null != issue.ResolutionReason)
				doc.Add(UnStored("ResolutionReason", issue.ResolutionReason.CurrentValue));
			
			if(null != issue.Type)
				doc.Add(UnStored("Type", issue.Type.CurrentValue));

			_luceneIndexWriter.AddDocument(doc);
		}

		/// <summary>
		/// Stories and Defects inherit from PrimartWorkitems, 
		/// This method indexes those fields
		/// </summary>
		/// <param name="primaryWorkItem">PrimaryWorkitem to index</param>
		/// <returns></returns>
		private Document IndexCommonFields(PrimaryWorkitem primaryWorkItem) 
		{
			Document doc = CreateDocument(primaryWorkItem);
			StringBuilder ownerBuffer = new StringBuilder();
			StringBuilder ownerIdBuffer = new StringBuilder();
			foreach (Member o in primaryWorkItem.Owners) {
				ownerBuffer.Append(o.Name);
				ownerIdBuffer.Append(o.Username);
			}
			doc.Add(UnStored("Owner", ownerBuffer.ToString()));
			doc.Add(UnStored("OwnerId", ownerIdBuffer.ToString()));

			StringBuilder goalBuffer = new StringBuilder();
			foreach (Goal goal in primaryWorkItem.Goals)
				goalBuffer.Append(goal.Name);
			doc.Add(UnStored("Goals", goalBuffer.ToString()));

			doc.Add(UnStored("Reference", primaryWorkItem.Reference));
			if(null != primaryWorkItem.Status)
				doc.Add(UnStored("Status", primaryWorkItem.Status.CurrentValue));
			if(null != primaryWorkItem.Priority)
				doc.Add(UnStored("Priority", primaryWorkItem.Priority.CurrentValue));
			if(null != primaryWorkItem.Source)
				doc.Add(UnStored("Source", primaryWorkItem.Source.CurrentValue));	
			if(null != primaryWorkItem.Team)
				doc.Add(UnStored("Team", primaryWorkItem.Team.Name));
			if(null != primaryWorkItem.Theme)
				doc.Add(UnStored("Theme", primaryWorkItem.Theme.Name));

			return doc;
		}

		/// <summary>
		/// Create the Lucene document and index fields common to a ProjectAsset object. 
		/// Stories, Defects, Requests, and Issues inherit from ProjectAsset
		/// </summary>
		/// <param name="projectAsset">ProjectAsset to index</param>
		/// <returns></returns>
		private Document CreateDocument(ProjectAsset projectAsset)
		{
			Document doc = new Document();
			doc.Add(UnIndexed("URL", _v1Url + "/assetdetail.v1?oid=" + projectAsset.ID));
			doc.Add(Keyword("ID", projectAsset.DisplayID));
			doc.Add(UnStored("OID", projectAsset.ID));
			doc.Add(Text("type", projectAsset.GetType().Name));
			doc.Add(Text("isClosed", projectAsset.IsClosed.ToString()));
			doc.Add(Text("Name", projectAsset.Name));
			doc.Add(UnStored("Description", projectAsset.Description));
			if(null != projectAsset.Project)
				doc.Add(Text("Project", projectAsset.Project.Name));

			StringBuilder noteBuffer = new StringBuilder();
			foreach (Note note in projectAsset.GetNotes(null))
				noteBuffer.Append(note.Name);
			doc.Add(UnStored("Notes", noteBuffer.ToString()));

			return doc;
		}

		/// <summary>
		/// Connect to the VersionOne server specified in the configuration file
		/// </summary>
		private void V1Connect() {
			_v1Url = ConfigurationManager.AppSettings["V1Server"];
			string user = ConfigurationManager.AppSettings["V1User"];
			string password = ConfigurationManager.AppSettings["V1Password"];
			Console.WriteLine("Connecting to " + _v1Url + " as " + user);
			_v1Server = new V1Instance(_v1Url, user, password);
		}

		/// <summary>
		/// Connect to Lucene index specified in the configuration file
		/// </summary>
		private void LuceneConnect() {
			string indexDir = ConfigurationManager.AppSettings["IndexDir"];
			Console.WriteLine("Creating Index in " + indexDir);
			Analyzer analyzer = new StandardAnalyzer();
			Directory directory = FSDirectory.GetDirectory(indexDir, true);
			_luceneIndexWriter = new IndexWriter(directory, analyzer, true);
		}

		#region LucenseHelper
		/// <summary>
		/// From "Lucene in Action": Creates a field that is not analyzed, but has been indexed and stored
		/// Suitable for fields whoes value needs to be searched and preserved in its entirety
		/// </summary>
		/// <param name="fieldName">name of field in Lucene</param>
		/// <param name="value">value in Lucene</param>
		/// <returns></returns>
		private static Field Keyword(string fieldName, string value)
		{
			return new Field(fieldName, value ?? string.Empty, Field.Store.YES, Field.Index.UN_TOKENIZED);
		}

		/// <summary>
		/// From "Lucene in Action": Creates a field that is not analyzed or indexed, but is stored
		/// Suitable for fields you need to display with search results but whoes value you'll never search directly (i.e. URL)
		/// </summary>
		/// <param name="fieldName">name of field in Lucene</param>
		/// <param name="value">value in Lucene</param>
		/// <returns></returns>
		private static Field UnIndexed(string fieldName, string value)
		{
			return new Field(fieldName, value ?? string.Empty, Field.Store.YES, Field.Index.NO);
		}

		/// <summary>
		/// From "Lucene in Action": Opposite of UnIndexed.  Creates a field that is analyzed and indexed, but NOT stored
		/// Suitable for indexing large amounts of text that needs to be searched against, but retrieved in it's origional form
		/// </summary>
		/// <param name="fieldName">name of field in Lucene</param>
		/// <param name="value">value in Lucene</param>
		/// <returns></returns>
		private static Field UnStored(string fieldName, string value)
		{
			return new Field(fieldName, value ?? string.Empty, Field.Store.NO, Field.Index.TOKENIZED);
		}

		/// <summary>
		/// From "Lucene in Action": Opposite of UnIndexed.  Creates a field that is analyzed, indexed, and stored
		/// Suitable for indexing large amounts of text that needs to be searched against AND retrieved in it's origional form
		/// </summary>
		/// <param name="fieldName">name of field in Lucene</param>
		/// <param name="value">value in Lucene</param>
		/// <returns></returns>
		private static Field Text(string fieldName, string value) {
			return new Field(fieldName, value ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED);
		}
		#endregion
	}
}
