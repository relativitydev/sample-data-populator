using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Helpers;
using Helpers.Interfaces;
using Helpers.Models;
using Helpers.RSAPI;
using kCura.EventHandler;
using Populator;
using kCura.Relativity.Client;
using kCura.Relativity.ImportAPI;
using Populator.Interfaces;
using Relativity.API;

namespace IncrementalPostEH
{
    [kCura.EventHandler.CustomAttributes.Description("Post Install EventHandler")]
    [System.Runtime.InteropServices.Guid("13A5EA07-4989-446E-9FA5-C65AB5643DFB")]
    public class PostInstallEventHandler : kCura.EventHandler.PostInstallEventHandler
    {
        public IAPILog Logger { get; set; }
        public IArtifactQueries ArtifactQueries { get; set; }
        public IUtility WebUtility { get; set; }
        public IImportAPI ImportApi { get; set; }
        public IRSAPIClient RsapiClient { get; set; }
        public IPopulator Populator { get; set; }
        public IImportApiLoader ImportApiLoader { get; set; }
        public string ExecutingPath { get; }

        public PostInstallEventHandler()
        {
            ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public override kCura.EventHandler.Response Execute()
        {
            return ExecuteAsync().Result;
        }

        public async Task<Response> ExecuteAsync()
        {
            var retVal = new kCura.EventHandler.Response()
            {
                Success = true,
                Message = String.Empty
            };
            try
            {
                DeferredLoggerInstantiation();
                DeferredImportApiLoaderInstantiation();

                var importSettings = await RetrieveSettingsAsync();
                importSettings.Logger = Logger;

                if (!String.IsNullOrWhiteSpace(importSettings.RelativityLibraryFolder))
                {
                    ImportApiLoader.LoadImportApiDlls(importSettings.RelativityLibraryFolder, ExecutingPath);

                    DeferredImportApiInstantiation();
                    DeferredArtifactQueriesInstantiation();
                    DeferredWebUtilityInstantiation();
                    DeferredRsapiClientInstantiation();
                    DeferredPopulatorInstantiation();

                    await Populator.PopulateDataAsync();
                }
                else
                {
                    var msg = "Please enter a path to the Relativity Library Folder";
                    Logger.LogError(msg);
                    throw new Exception(msg);
                }
                
            }
            catch (Exception ex)
            {
                if (Logger != null)
                {
                    Logger.LogError($"Unable to populate test data: {ex}");
                }
                retVal.Success = false;
                retVal.Message = ex.ToString();
            }
            finally
            {
                if (RsapiClient != null)
                {
                    RsapiClient.Dispose();
                }
            }

            return retVal;
        }

        private async Task<DocumentImportSettings> RetrieveSettingsAsync()
        {
            DocumentImportSettings retVal = null;

            var configFileArtifactID = await ArtifactQueries.QueryResourceFileArtifactIDAsync(RsapiClient, Helpers.Constants.FileNames.ConfigFileName);

            if (configFileArtifactID > 0)
            {
                var authToken = await ArtifactQueries.RequestAuthTokenAsync(RsapiClient);
                if (authToken != null)
                {

                    var fileDownloadLocation = await DownloadConfigFile(configFileArtifactID, authToken);
                    retVal = new DocumentImportSettings(fileDownloadLocation);
                }
                else
                {
                    throw new Exception("Unable to generate auth token");
                }
            }
            else
            {
                throw new Exception($"Unable to find config file in resources tab: {Helpers.Constants.FileNames.ConfigFileName}");
            }

            return retVal;
        }

        private async Task<string> DownloadConfigFile(int configFileArtifactID, string authToken)
        {
            var fileDownloadLocation = Helpers.Utility.GenerateConfigFilePath();
            var domain = RsapiClient.EndpointUri.Host;

            try
            {
                //https
                var configFileUrl = Helpers.Constants.Protocols.Https + String.Format(Helpers.Constants.URLs.ResourceFileDownload,
                                        domain,
                                        configFileArtifactID,
                                        authToken);

                await WebUtility.DownloadFileAsync(new Uri(configFileUrl), fileDownloadLocation);
            }
            catch (Exception)
            {
                //http
                var configFileUrl = Helpers.Constants.Protocols.Http + String.Format(Helpers.Constants.URLs.ResourceFileDownload,
                                        domain,
                                        configFileArtifactID,
                                        authToken);

                await WebUtility.DownloadFileAsync(new Uri(configFileUrl), fileDownloadLocation);
            }
            return fileDownloadLocation;
        }

        #region DeferredInstantiation

        private void DeferredImportApiLoaderInstantiation()
        {
            if (ImportApiLoader != null)
            {
                ImportApiLoader = new ImportApiLoader();
            }
        }
        private void DeferredLoggerInstantiation()
        {
            if (Logger == null)
            {
                Logger = Helper.GetLoggerFactory().GetLogger();
            }
        }
        private void DeferredImportApiInstantiation()
        {
            if (ImportApi == null)
            {
                try
                {
                    //https
                    var importApiUrl = Helpers.Constants.Protocols.Https + String.Format(Helpers.Constants.URLs.WebApiUrl, Helper.GetServicesManager().GetServicesURL().Host);
                    ImportApi = new ImportAPI(Helpers.Constants.Credentials.UserName, Helpers.Constants.Credentials.Password, importApiUrl);
                }
                catch (Exception)
                {
                    //http
                    var importApiUrl = Helpers.Constants.Protocols.Http + String.Format(Helpers.Constants.URLs.WebApiUrl, Helper.GetServicesManager().GetServicesURL().Host);
                    ImportApi = new ImportAPI(Helpers.Constants.Credentials.UserName, Helpers.Constants.Credentials.Password, importApiUrl);
                }
            }
        }

        private void DeferredArtifactQueriesInstantiation()
        {
            if (ArtifactQueries == null)
            {
                ArtifactQueries = new ArtifactQueries(Logger);
            }
        }

        private void DeferredWebUtilityInstantiation()
        {
            if (WebUtility == null)
            {
                WebUtility = new Utility(Logger);
            }
        }

        private void DeferredRsapiClientInstantiation()
        {
            if (RsapiClient == null)
            {
                RsapiClient = Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System);
            }
        }

        private void DeferredPopulatorInstantiation()
        {
            if (Populator == null)
            {
                var settings = RetrieveSettingsAsync().Result;
                Populator = new DocumentPopulator(
                    rsapiClient: RsapiClient,
                    workspaceArtifactID: Helper.GetActiveCaseID(),
                    importApi: ImportApi,
                    artifactQueries: ArtifactQueries,
                    logger: Logger,
                    settings: settings);
            }
        }
        #endregion
    }
}
