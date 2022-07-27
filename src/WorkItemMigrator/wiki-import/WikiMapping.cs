using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WikiImport
{
    public class WikiMapping
    {
        public string WikiName { get; set; }
        public Guid WikiID { get; set; }

        public List<WikiPage> pages { get; set; }

        public string ApiVersion { get; set; }
        public string Organization { get; set; }
        public string ProjectName { get; set; }
        public HttpClient client { get; set; }
        public string PersonalAccessToken { get; set; }

        public WikiMapping(string Organization, string ProjectName, string Token)
        {
            ApiVersion = "6.0";
            this.Organization = Organization;
            this.ProjectName = ProjectName;
            PersonalAccessToken = Token;
            // Initialize the HttpClient and add the token
            client = new HttpClient();
            //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", Token))));

        }
    }

    public class WikiPage
    {
        public string PageTitle { get; set; }
        public Guid PageID { get; set; }
        public Guid ParentPageID { get; set; }
        public string Content { get; set; }

        /// <summary>
        /// This is used to tell the API where to place the page in the wiki.
        /// </summary>
        public string path { get; set; }

        public List<WikiPage> ChildPages { get; set; }
        public List<Attachments> PageAttachments { get; set; }
    }

    public class Attachments
    {
        // TODO: Figure out what makes up an attachment
    }

}
