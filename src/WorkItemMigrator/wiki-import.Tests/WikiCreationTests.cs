using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WikiImport;

namespace wiki_import.Tests
{
    [TestClass]
    public class WikiCreationTests
    {

        private static HttpClient client = new HttpClient();
        private static string token = "r23zw5zijqxp2x3555pvspzqasyuw6unq5pq2ezm6jxga7kvy65q";
        string _wikiNameUnitTest = "Created By Unit Test";
        string _wikiNameReal = "Export-from-Confluence";
        Guid _wikiGuidReal = new Guid("a0e03275-48b6-4691-ad52-a045df9182d2");

        private WikiMapping wikiMapping = new WikiMapping("palazzoloj", "Workday", token);

        [TestInitialize]
        public void InitialzeHTTPClient()
        {

            // Initialize the HttpClient (add the token)
            //if (!token.StartsWith(":")) token = ":" + token;
            //var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", encodedToken);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", token))));
        }


        /// <summary>
        /// Currently this is failing.  I _think_ it is because ADO only supports one Wiki per Project.
        /// I read conflicting things whether you can or can't have more than one. For now, I can't get more than one.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestCreateNewWiki()
        {
            wikiMapping.WikiName = _wikiNameUnitTest;

            Task<Guid> getWikiIDTask = ImportCommandLine.CreateOrGetWikiAsync(wikiMapping);
            Guid wikiGuid = await getWikiIDTask;

            Assert.AreNotEqual(Guid.Empty, wikiGuid);
        }

        [TestMethod]
        public async Task TestCreateNewWikiPage_1Simple()
        {
            wikiMapping.WikiID = _wikiGuidReal;
            wikiMapping.WikiName = _wikiNameReal;

            string pagePath = "unit-test-simple-page";
            string sContent = $"This is a simple test page created by Unit Test at {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";

            Task<string> taskInsert = ImportCommandLine.CreateWikiPage(wikiMapping, pagePath, sContent);
            string actual = await taskInsert;

            Assert.AreEqual($"/{pagePath}", actual);
        }

        [TestMethod]
        public async Task TestCreateNewWikiPage_1SimpleChild()
        {
            wikiMapping.WikiID = _wikiGuidReal;
            wikiMapping.WikiName = _wikiNameReal;

            string pagePath = "unit-test-simple-page/simple-child";
            string sContent = $"This is a simple test child page created inside the simple parent page.  This was created by Unit Test at {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";

            Task<string> taskInsert = ImportCommandLine.CreateWikiPage(wikiMapping, pagePath, sContent);
            string actual = await taskInsert;

            Assert.AreEqual($"/{pagePath}", actual);
        }

        [TestMethod]
        public async Task TestCreateNewWikiPage_2MDContent()
        {
            wikiMapping.WikiID = _wikiGuidReal;
            wikiMapping.WikiName = _wikiNameReal;

            string pagePath = "unit-test-md-page";
            string sContent = ReadTextFile("../../TestDataFiles/2019-05-30-Hartford-Integration-Meeting-notes_82346262.md");

            Task<string> taskInsert = ImportCommandLine.CreateWikiPage(wikiMapping, pagePath, sContent);
            string actual = await taskInsert;

            Assert.AreEqual($"/{pagePath}", actual);
        }

        [TestMethod]
        public async Task TestCreateNewWikiPage_3HTMLContent()
        {
            wikiMapping.WikiID = _wikiGuidReal;
            wikiMapping.WikiName = _wikiNameReal;

            string pagePath = "unit-test-html-page";
            string sContent = ReadTextFile("../../TestDataFiles/2019-05-30-Hartford-Integration-Meeting-notes_82346262.html");

            Task<string> taskInsert = ImportCommandLine.CreateWikiPage(wikiMapping, pagePath, sContent);
            string actual = await taskInsert;

            Assert.AreEqual($"/{pagePath}", actual);
        }

        [TestMethod]
        public async Task TestCreateAttachment_Binary()
        {
            wikiMapping.WikiID = _wikiGuidReal;
            wikiMapping.WikiName = _wikiNameReal;

            string fileName = $"SampleFile1.{DateTime.Now.ToString("ddMMhhmmss")}.xlsx";
            string fileContents = ReadBinaryFileAsString("../../TestDataFiles/SampleFile1.xlsx");
            //byte[] fileContents = ReadBinaryFile("../../TestDataFiles/SampleFile1.xlsx");

            Task<string> taskUpload = ImportCommandLine.UploadAttachment(wikiMapping, fileName, fileContents);
            string actual = await taskUpload;

            Assert.AreEqual($"/.attachments/{fileName}", actual);
        }

        [TestMethod]
        public async Task TestCreateAttachment_String()
        {
            wikiMapping.WikiID = _wikiGuidReal;
            wikiMapping.WikiName = _wikiNameReal;

            string fileName = "SampleFile1.txt";
            string fileContents = ReadStringAsBase64("This would be the file contents for the fictitious file we are uploading");


            Task<string> taskUpload = ImportCommandLine.UploadAttachment(wikiMapping, fileName, fileContents);
            string actual = await taskUpload;

            Assert.AreEqual($"/.attachments/{fileName}", actual);
        }

        [TestMethod]
        public async Task LinkAttachmentToPage()
        {
            wikiMapping.WikiID = _wikiGuidReal;
            wikiMapping.WikiName = _wikiNameReal;

            string fileName1 = $"LinkedFile1.{DateTime.Now.ToString("ddMMhhmmss")}.txt";
            string fileContents1 = ReadStringAsBase64($"This would be the fictitious file we are uploading at {DateTime.Now.ToShortTimeString()}");

            // First upload the file
            Task<string> taskUpload = ImportCommandLine.UploadAttachment(wikiMapping, fileName1, fileContents1);
            string actualFilePath1 = await taskUpload;

            Assert.AreEqual($"/.attachments/{fileName1}", actualFilePath1);

            // Second, upload the second file
            string fileName2 = $"LinkedFile2.{DateTime.Now.ToString("ddMMhhmmss")}.xlsx";
            string fileContents2 = ReadBinaryFileAsString("../../TestDataFiles/SampleFile1.xlsx");

            Task<string> taskUpload2 = ImportCommandLine.UploadAttachment(wikiMapping, fileName2, fileContents2);
            string actualFilePath2 = await taskUpload2;
            Assert.AreEqual($"/.attachments/{fileName2}", actualFilePath2);


            // Now, create a new page that contains a link to the attachment.
            string pageContents = $"This would be the [file contents](/.attachments/{fileName1})  for the fictitious file we are uploading at {DateTime.Now.ToShortTimeString()}{Environment.NewLine}This is the other [ficitious file](/.attachments/{fileName2})";
            string pagePath = $"unit-test-simple-page/attachment-child-{DateTime.Now.ToString("ddMMhhmmss")}";

            Task<string> taskInsert = ImportCommandLine.CreateWikiPage(wikiMapping, pagePath, pageContents);
            string actualPagePath = await taskInsert;

            Assert.AreEqual($"/{pagePath}", actualPagePath);

            // [Oakland County Roadmap items 4_18_22.csv .csv](/.attachments/Oakland%20County%20Roadmap%20items%204_18_22.csv%20-92482fb4-e94e-448e-93db-37c6d2b8f904.csv). 


        }

        [TestMethod]
        public async Task TestGetExistingWiki()
        {
            wikiMapping.WikiName = _wikiNameReal;

            Task<Guid> getWikiIDTask = ImportCommandLine.CreateOrGetWikiAsync(wikiMapping);
            Guid wikiGuidActual = await getWikiIDTask;

            Assert.AreNotEqual(Guid.Empty, wikiGuidActual);
            Assert.AreEqual(_wikiGuidReal, wikiGuidActual);
        }


        [TestMethod]
        public async Task TestGetExistingRootPages()
        {
            wikiMapping.WikiName = _wikiNameReal;
            wikiMapping.WikiID = _wikiGuidReal;

            Task<List<WikiImport.Models.PageResponse>> taskGetWikiPages = ImportCommandLine.GetWikiPages(wikiMapping);
            List<WikiImport.Models.PageResponse> lwpr = await taskGetWikiPages;

            Assert.IsTrue(lwpr.Count > 0);
        }

        [TestMethod]
        public async Task TestGetProjectID()
        {
            Guid expectedProjectID = new Guid("6a6cfd21-c305-478f-bb3c-fbe14a86e269");

            Task<Guid> getIdTask = ImportCommandLine.GetProjectID(wikiMapping);
            Guid id = await getIdTask;

            Assert.AreEqual(expectedProjectID, id);
        }

        [TestMethod]
        public async Task TestGetProjects()
        {
            GetProjects(wikiMapping);
        }
        /// <summary>
        /// Microsoft's example of how to talk to the REST API.
        /// </summary>
        /// <param name="wikiMapping"></param>
        /// <see cref="https://docs.microsoft.com/en-us/azure/devops/integrate/how-to/call-rest-api?view=azure-devops"/>
        public static async void GetProjects(WikiMapping wikiMapping)
        {
            try
            {
                //var personalaccesstoken = "PAT_FROM_WEBSITE";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", wikiMapping.PersonalAccessToken))));

                    using (HttpResponseMessage response = client.GetAsync($"https://dev.azure.com/{wikiMapping.Organization}/_apis/projects").Result)
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);

                        Assert.IsFalse(responseBody.Contains("Enhanced Security Configuration"));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static string ReadTextFile(string FileName)
        {
            using (StreamReader sr = new StreamReader(FileName, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="FileName"></param>
        ///// <returns></returns>
        ///// <see cref="https://stackoverflow.com/questions/2426190/how-to-read-file-binary-in-c"/>
        //private static string ReadBinaryFile(string FileName)
        //{
        //    byte[] fileBytes = File.ReadAllBytes(FileName);

        //    StringBuilder sb = new StringBuilder();

        //    foreach (byte b in fileBytes)
        //    {
        //        sb.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
        //    }

        //    return (sb.ToString());
        //}

        private static byte[] ReadBinaryFileAsByteArray(string FileName)
        {
            byte[] fileBytes = File.ReadAllBytes(FileName);

            return fileBytes;

            //ByteArrayContent byteContent = new ByteArrayContent(fileBytes);

            //return byteContent;
        }

        private static ByteArrayContent ReadBinaryFileAsByteArrayContent(string FileName)
        {
            byte[] fileBytes = File.ReadAllBytes(FileName);

            ByteArrayContent byteContent = new ByteArrayContent(fileBytes);

            return byteContent;
        }

        private static string ReadBinaryFileAsString(string FileName)
        {
            byte[] fileBytes = File.ReadAllBytes(FileName);

            string byteContent = Convert.ToBase64String(fileBytes);

            return byteContent;
        }

        private static string ReadStringAsBase64(string text)
        {
            byte[] stringBytes = Encoding.ASCII.GetBytes(text);
            return Convert.ToBase64String(stringBytes);
        }

        private static byte[] ReadString(string text)
        {
            return Encoding.ASCII.GetBytes(text); 

            //byte[] fileBytes = Encoding.ASCII.GetBytes(text);

            //ByteArrayContent byteContent = new ByteArrayContent(fileBytes);

            //return byteContent;
        }



    }
}
