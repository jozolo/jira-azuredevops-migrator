using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Common.Config;

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using Migration.Common;
using Migration.Common.Config;
using Migration.Common.Log;
using Newtonsoft.Json;
using WikiImport.Models;
using Wikimport;

namespace WikiImport
{
    public class ImportCommandLine
    {
        private CommandLineApplication commandLineApplication;
        private string[] args;

        private static WikiMapping _wikiMapping;


        public ImportCommandLine(params string[] args)
        {
            InitCommandLine(args);
        }

        public void Run()
        {
            commandLineApplication.Execute(args);
        }

        private void InitCommandLine(params string[] args)
        {
            commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: true);
            this.args = args;
            ConfigureCommandLineParserWithOptions();
        }

        private void ConfigureCommandLineParserWithOptions()
        {
            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.FullName = "Work item migration tool that assists with moving Jira items to Azure DevOps or TFS.";
            commandLineApplication.Name = "wi-import";

            CommandOption tokenOption = commandLineApplication.Option("--token <accesstoken>", "Personal access token to use for authentication", CommandOptionType.SingleValue);
            CommandOption urlOption = commandLineApplication.Option("--url <accounturl>", "Url for the account", CommandOptionType.SingleValue);
            CommandOption configOption = commandLineApplication.Option("--config <configurationfilename>", "Import the work items based on the configuration file", CommandOptionType.SingleValue);
            CommandOption forceOption = commandLineApplication.Option("--force", "Forces execution from start (instead of continuing from previous run)", CommandOptionType.NoValue);
            CommandOption continueOnCriticalOption = commandLineApplication.Option("--continue", "Continue execution upon a critical error", CommandOptionType.SingleValue);


            commandLineApplication.OnExecute(() =>
            {
                bool forceFresh = forceOption.HasValue();

                if (configOption.HasValue())
                {
                    ExecuteMigrationAsync(tokenOption, urlOption, configOption, forceFresh, continueOnCriticalOption);
                }
                else
                {
                    commandLineApplication.ShowHelp();
                }

                return 0;
            });
        }

        private async Task ExecuteMigrationAsync(CommandOption token, CommandOption url, CommandOption configFile, bool forceFresh, CommandOption continueOnCritical)
        {
            ConfigJson config = null;
            var itemCount = 0;
            var revisionCount = 0;
            var importedItems = 0;
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                string configFileName = configFile.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                config = configReaderJson.Deserialize();

                var context = MigrationContext.Init("wiki-import", config.Workspace, config.LogLevel, forceFresh, continueOnCritical.Value());

                // connection settings for Azure DevOps/TFS:
                // full base url incl https, name of the project where the items will be migrated (if it doesn't exist on destination it will be created), personal access token
                var settings = new Settings(url.Value(), config.TargetProject, token.Value())
                {
                    BaseAreaPath = config.BaseAreaPath ?? string.Empty, // Root area path that will prefix area path of each migrated item
                    BaseIterationPath = config.BaseIterationPath ?? string.Empty, // Root iteration path that will prefix each iteration
                    IgnoreFailedLinks = config.IgnoreFailedLinks,
                    ProcessTemplate = config.ProcessTemplate
                };

                // initialize Azure DevOps/TFS connection. Creates/fetches project, fills area and iteration caches.
                var agent = Agent.Initialize(context, settings);

                if (agent == null)
                {
                    Logger.Log(LogLevel.Critical, "Azure DevOps/TFS initialization error.");
                    return;
                }





                _wikiMapping = new WikiMapping("palazzoloj", "Workday", token.Value());
                _wikiMapping.WikiName = "Export from Confluence";





                // Ensure Wiki exists.  Create if doesn't.  If it does, get the GUID for the Wiki
                //https://dev.azure.com/palazzoloj/Workday/_apis/wiki/wikis?api-version=6.0
                Task<Guid> taskWikiGuid = CreateOrGetWikiAsync(_wikiMapping);
                Guid wikiID = await taskWikiGuid;


                // Create the top level pages for the wiki


                // Create the child level page(s) for the wiki


                // Upload the attachments


                // Link the pages to each other







                //var executionBuilder = new ExecutionPlanBuilder(context);
                //var plan = executionBuilder.BuildExecutionPlan();

                //itemCount = plan.ReferenceQueue.AsEnumerable().Select(x => x.OriginId).Distinct().Count();
                //revisionCount = plan.ReferenceQueue.Count;

                //BeginSession(configFileName, config, forceFresh, agent, itemCount, revisionCount);

                //while (plan.TryPop(out ExecutionPlan.ExecutionItem executionItem))
                //{
                //    try
                //    {
                //        if (!forceFresh && context.Journal.IsItemMigrated(executionItem.OriginId, executionItem.Revision.Index))
                //            continue;

                //        WorkItem wi = null;

                //        if (executionItem.WiId > 0)
                //            wi = agent.GetWorkItem(executionItem.WiId);
                //        else
                //            wi = agent.CreateWorkItem(executionItem.WiType);

                //        Logger.Log(LogLevel.Info, $"Processing {importedItems + 1}/{revisionCount} - wi '{(wi.Id > 0 ? wi.Id.ToString() : "Initial revision")}', jira '{executionItem.OriginId}, rev {executionItem.Revision.Index}'.");

                //        agent.ImportRevision(executionItem.Revision, wi);
                //        importedItems++;
                //    }
                //    catch (AbortMigrationException)
                //    {
                //        Logger.Log(LogLevel.Info, "Aborting migration...");
                //        break;
                //    }
                //    catch (Exception ex)
                //    {
                //        try
                //        {
                //            Logger.Log(ex, $"Failed to import '{executionItem.ToString()}'.");
                //        }
                //        catch (AbortMigrationException)
                //        {
                //            break;
                //        }
                //    }
                //}
            }
            catch (CommandParsingException e)
            {
                Logger.Log(LogLevel.Error, $"Invalid command line option(s): {e}");
            }
            catch (Exception e)
            {
                Logger.Log(e, $"Unexpected migration error.");
            }
            finally
            {
                EndSession(itemCount, revisionCount, sw);
            }
        }

        //public static async Task<string> UploadAttachment(WikiMapping wikiMapping, string FileName, string FileContents)
        //{
        //    string apiUrl = $"https://dev.azure.com/{wikiMapping.Organization}/{wikiMapping.ProjectName}/_apis/wiki/wikis/{wikiMapping.WikiID}/attachments?name={Uri.EscapeDataString(FileName)}&api-version={wikiMapping.ApiVersion}";

        //    try
        //    {
        //        //AttachmentContent ac = new AttachmentContent(FileContents);
        //        //string httpPostData = JsonConvert.SerializeObject(ac);
        //        //StringContent content = new StringContent(httpPostData, Encoding.UTF8, "application/octet-stream");
        //        StringContent content = new StringContent(FileContents, Encoding.UTF8, "application/octet-stream");

        //        using (HttpResponseMessage responseMsg = await wikiMapping.client.PutAsync(apiUrl, content))
        //        {
        //            responseMsg.EnsureSuccessStatusCode();
        //            string responseString = await responseMsg.Content.ReadAsStringAsync();

        //            // Parse the response to whatever it is.
        //            AttachmentResponse ar = JsonConvert.DeserializeObject<AttachmentResponse>(responseString);
        //            return ar.path;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }

        //    // If we got an error, return null;
        //    // This won't ever get hit because we are throwing the exception...
        //    // Once we figure out logging exceptions and then don't throw, we will return null for failure. 
        //    return null;
        //}

        public static async Task<string> UploadAttachment(WikiMapping wikiMapping, string FileName, string FileContents)
        {
            string apiUrl = $"https://dev.azure.com/{wikiMapping.Organization}/{wikiMapping.ProjectName}/_apis/wiki/wikis/{wikiMapping.WikiID}/attachments?name={Uri.EscapeDataString(FileName)}&api-version={wikiMapping.ApiVersion}";

            try
            {
                StringContent content = new StringContent(FileContents, Encoding.UTF8, "application/octet-stream");

                using (HttpResponseMessage responseMsg = await wikiMapping.client.PutAsync(apiUrl, content))
                {
                    responseMsg.EnsureSuccessStatusCode();
                    string responseString = await responseMsg.Content.ReadAsStringAsync();

                    // Parse the response to whatever it is.
                    AttachmentResponse ar = JsonConvert.DeserializeObject<AttachmentResponse>(responseString);
                    return ar.path;
                }
            }
            catch (Exception)
            {
                throw;
            }

            // If we got an error, return null;
            // This won't ever get hit because we are throwing the exception...
            // Once we figure out logging exceptions and then don't throw, we will return null for failure. 
            return null;
        }


        /// <summary>
        /// Create the given WikiName in the specified ADO site using the WikiMappings.  Return the ID.
        /// </summary>
        /// <param name="wikiMapping"></param>
        /// <param name="v"></param>
        public static async Task<Guid> CreateOrGetWikiAsync(WikiMapping wikiMapping)
        {
            // The same URL is used to Get the list of Wikis or Create a new Wiki.  The only difference is GET vs. POST
            string apiUrl = $"https://dev.azure.com/{wikiMapping.Organization}/{wikiMapping.ProjectName}/_apis/wiki/wikis?api-version={wikiMapping.ApiVersion}";

            try
            {
                // See if Wiki already exists.  If so, get the ID.
                using (HttpResponseMessage responseMsg = await wikiMapping.client.GetAsync(apiUrl))
                {
                    responseMsg.EnsureSuccessStatusCode();
                    string responseString = await responseMsg.Content.ReadAsStringAsync();
                    WikiListResponse wlr = JsonConvert.DeserializeObject<WikiListResponse>(responseString);

                    foreach(WikiResponse wr in wlr.value)
                    {
                        if (wr.name == wikiMapping.WikiName) 
                        {
                            wikiMapping.WikiID = wr.id;
                            return wr.id; 
                        }
                    }
                }

                // If Wiki does not yet exist, create it and get the ID. 
                WikiCreateRequest wcr = new WikiCreateRequest(wikiMapping.WikiName, await GetProjectID(wikiMapping));
                string httpPostData = JsonConvert.SerializeObject(wcr);

                StringContent content = new StringContent(httpPostData, Encoding.UTF8, "application/json");

                using (HttpResponseMessage responseMsg = await wikiMapping.client.PostAsync(apiUrl, content))
                {
                    responseMsg.EnsureSuccessStatusCode();
                    string responseString = await responseMsg.Content.ReadAsStringAsync();
                    WikiListResponse wlr = JsonConvert.DeserializeObject<WikiListResponse>(responseString);

                    foreach (WikiResponse wr in wlr.value)
                    {
                        if (wr.name == wikiMapping.WikiName)
                        {
                            wikiMapping.WikiID = wr.id;
                            return wr.id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If we don't get a success response code from either of the web requests, it'll throw an exception.
                Logger.Log(LogLevel.Error, $"ERROR: CreateOrGetWikiAsync {ex.Message}");
            }

            return Guid.Empty;
        }


        public async static Task<Guid> GetProjectID(WikiMapping wikiMapping)
        {
            string apiUrl = $"https://dev.azure.com/{wikiMapping.Organization}/_apis/projects?api-version={wikiMapping.ApiVersion}";

            try
            {
                var responseString = await wikiMapping.client.GetStringAsync(apiUrl);

                WikiProjectListResponse wikiProjectList = JsonConvert.DeserializeObject<WikiProjectListResponse>(responseString);

                foreach (ProjectResponse pr in wikiProjectList.value )
                {
                    if (pr.name == wikiMapping.ProjectName)
                        return pr.id;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return Guid.Empty;
        }

        public async static Task<List<PageResponse>> GetWikiPages(WikiMapping wikiMapping, string PagePath = null)
        {
            string apiUrl = $"https://dev.azure.com/{wikiMapping.Organization}/{wikiMapping.ProjectName}/_apis/wiki/wikis/{wikiMapping.WikiID}/pages?api-version={wikiMapping.ApiVersion}&recursionLevel=1";

            if (PagePath != null) apiUrl = $"{apiUrl}&path={Uri.EscapeDataString(PagePath)}";

            List<PageResponse> lpr = new List<PageResponse>();

            try
            {
                using (HttpResponseMessage responseMsg = await wikiMapping.client.GetAsync(apiUrl))
                {
                    responseMsg.EnsureSuccessStatusCode();
                    string responseString = await responseMsg.Content.ReadAsStringAsync();

                    if (PagePath == null)
                    {
                        PageResponse prRoot = JsonConvert.DeserializeObject<PageResponse>(responseString);
                        lpr = prRoot.subPages;
                    }
                    else
                    {
                        PageResponse pr = JsonConvert.DeserializeObject<PageResponse>(responseString);
                        lpr.Add(pr);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                throw ex;
            }

            return lpr;
        }

        /// <summary>
        /// Create or update a page in the Wiki at the specified path.
        /// Currently, this will only create a new and it fails to update an existing.  
        /// My gut says that it will update if you figure out the current version number and then pass it a new, incremented version number.
        /// </summary>
        /// <param name="wikiMapping"></param>
        /// <param name="PagePath">Wiki page path. This is required when creating a page.</param>
        /// <returns></returns>
        public async static Task<string> CreateWikiPage(WikiMapping wikiMapping, string PagePath, string PageContent)
        {
            string apiUrl = $"https://dev.azure.com/{wikiMapping.Organization}/{wikiMapping.ProjectName}/_apis/wiki/wikis/{wikiMapping.WikiID}/pages?path={Uri.EscapeDataString(PagePath)}&api-version={wikiMapping.ApiVersion}";

            List<PageResponse> lpr = new List<PageResponse>();

            try
            {
                PageRequest pageRequest = new PageRequest(PageContent);
                string httpPostData = JsonConvert.SerializeObject(pageRequest);
                StringContent content = new StringContent(httpPostData, Encoding.UTF8, "application/json");

                using (HttpResponseMessage responseMsg = await wikiMapping.client.PutAsync(apiUrl, content))
                {
                    responseMsg.EnsureSuccessStatusCode();
                    string responseString = await responseMsg.Content.ReadAsStringAsync();

                    // Parse the response to whatever it is.
                    PageResponse pr = JsonConvert.DeserializeObject<PageResponse>(responseString);
                    return pr.path;
                }
            }
            catch (Exception)
            {
                throw;
            }

            // If we got an error, return null;
            // This won't ever get hit because we are throwing the exception...
            // Once we figure out logging exceptions and then don't throw, we will return null for failure. 
            return null;
        }

        private static void BeginSession(string configFile, ConfigJson config, bool force, Agent agent, int itemsCount, int revisionCount)
        {
            var toolVersion = VersionInfo.GetVersionInfo();
            var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
            var machine = System.Environment.MachineName;
            var user = $"{System.Environment.UserDomainName}\\{System.Environment.UserName}";
            var hostingType = GetHostingType(agent);

            Logger.Log(LogLevel.Info, $"Import started. Importing {itemsCount} items with {revisionCount} revisions.");

            Logger.StartSession("Azure DevOps Work Item Import",
                "wi-import-started",
                new Dictionary<string, string>() {
                    { "Tool version         :", toolVersion },
                    { "Start time           :", DateTime.Now.ToString() },
                    { "Telemetry            :", Logger.TelemetryStatus },
                    { "Session id           :", Logger.SessionId },
                    { "Tool user            :", user },
                    { "Config               :", configFile },
                    { "User                 :", user },
                    { "Force                :", force ? "yes" : "no" },
                    { "Log level            :", config.LogLevel },
                    { "Machine              :", machine },
                    { "System               :", osVersion },
                    { "Azure DevOps url     :", agent.Settings.Account },
                    { "Azure DevOps version :", "n/a" },
                    { "Azure DevOps type    :", hostingType }
                    },
                new Dictionary<string, string>() {
                    { "item-count", itemsCount.ToString() },
                    { "revision-count", revisionCount.ToString() },
                    { "system-version", "n/a" },
                    { "hosting-type", hostingType } });
        }

        private static string GetHostingType(Agent agent)
        {
            var uri = new Uri(agent.Settings.Account);
            switch (uri.Host.ToLower())
            {
                case "dev.azure.com":
                case "visualstudio.com":
                    return "Cloud";
                default:
                    return "Server";
            }
        }

        private static void EndSession(int itemsCount, int revisionCount, Stopwatch sw)
        {
            sw.Stop();

            Logger.Log(LogLevel.Info, $"Import complete. Imported {itemsCount} items, {revisionCount} revisions ({Logger.Errors} errors, {Logger.Warnings} warnings) in {string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed)}.");

            Logger.EndSession("wi-import-completed",
                new Dictionary<string, string>() {
                    { "item-count", itemsCount.ToString() },
                    { "revision-count", revisionCount.ToString() },
                    { "error-count", Logger.Errors.ToString() },
                    { "warning-count", Logger.Warnings.ToString() },
                    { "elapsed-time", string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed) } });
        }
    }
}