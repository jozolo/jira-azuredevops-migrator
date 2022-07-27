using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiImport.Models
{
    public class Wiki
    {
        public string type { get; set; }
        public string name { get; set; }
        public Guid projectId { get; set; }
    }

    /// <summary>
    /// This is used to PUT a new page.
    /// </summary>
    public class WikiPage
    {
        /// <summary>
        /// The markup formatted content of the page
        /// </summary>
        public string content { get; set; }
    }

    public class WikiProjectListResponse
    {
        public List<ProjectResponse> value { get; set; }
        public int count { get; set; }
    }

    public class ProjectResponse
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public string state { get; set; }
        public int revision { get; set; }
        public string visibility { get; set; }
        public DateTime lastUpdateTime { get; set; }
    }

    public class WikiCreateRequest
    {
        public string type { get { return "projectWiki"; } private set {} }
        public string name { get; set; }
        public Guid projectId { get; set; }

        public WikiCreateRequest(string Name, Guid ProjectID)
        {
            name = Name;
            projectId = ProjectID;
        }
    }

    public class WikiListResponse
    {
        public List<WikiResponse> value { get; set; }
        public int count { get; set; }
    }

    public class WikiPageResponse
    {
        public List<PageResponse> value { get; set; }
        public int count { get; set; }
    }

    public class PageRequest
    {
        public string content { get; set; }

        public PageRequest(string PageContent)
        {
            content = PageContent;
        }
    }

    public class AttachmentContent
    {
        public string body { get; set; }

        public AttachmentContent(string FileContents)
        {
            body = FileContents;
        }
    }

    public class AttachmentResponse
    {
        public string name { get; set; }
        public string path { get; set; }
    }

    public class PageResponse
    {
        /// <summary>
        /// I think that when you create a new page, the response is an ID as an INT.
        /// However, when you just list all of the pages, the response is an ID as a GUID.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Order of the wiki page, relative to other pages in the same hierarchy level.
        /// </summary>
        public int order { get; set; }

        /// <summary>
        /// List of subpages of the current page.
        /// </summary>
        public List<PageResponse> subPages { get; set; }

        /// <summary>
        /// REST url for this wiki page.
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// Remote web url to the wiki page.
        /// </summary>
        public string remoteUrl { get; set; }

        /// <summary>
        /// Path of the git item corresponding to the wiki page stored in the backing Git repository.
        /// </summary>
        public string getItemPath { get; set; }
        public string path { get; set; }
        public string name { get; set; }

        /// <summary>
        /// Content of the wiki page.
        /// </summary>
        public string content { get; set; }
        /// <summary>
        /// True if this page has subpages under its path.
        /// </summary>
        public bool isParentPage { get; set; }

        /// <summary>
        /// True if a page is non-conforming, i.e. 1) if the name doesn't match page naming standards. 2) if the page does not have a valid entry in the appropriate order file.
        /// </summary>
        public bool isNonConformant { get; set; }
    }

    public class WikiResponse
    {
        public Guid id { get; set; }
        public List<Versions> versions { get; set; }
        public string url { get; set; }
        public string remoteUrl { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public Guid projectId { get; set; }
        public Guid repositoryId { get; set; }
        public string mappedPath { get; set; }

        public class Versions
        {
            string version { get; set; }
        }
    }

}
