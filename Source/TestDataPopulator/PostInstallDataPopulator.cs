using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DP.Helpers;
using DP.Helpers.Interfaces;
using DP.Helpers.Models;
using DP.Helpers.RSAPI;
using DP.Populator;
using DP.Populator.Interfaces;
using kCura.EventHandler;
using kCura.Relativity.Client;
using kCura.Relativity.ImportAPI;
using Relativity.API;

namespace DP.EventHandlers
{
    [kCura.EventHandler.CustomAttributes.Description("Post Install EventHandler")]
    [System.Runtime.InteropServices.Guid("d70032d0-646f-4fb1-a361-5d17c316c0b6")]
    public class PostInstallDataPopulator : kCura.EventHandler.PostInstallEventHandler
    {
        public IAPILog Logger { get; set; }
        public IArtifactQueries ArtifactQueries { get; set; }
        public IUtility WebUtility { get; set; }
        public IImportAPI ImportApi { get; set; }
        public IRSAPIClient RsapiClient { get; set; }
        public IPopulator Populator { get; set; }
        public IImportApiLoader ImportApiLoader { get; set; }
        public string ExecutingPath { get; }

        public PostInstallDataPopulator()
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
                DeferredArtifactQueriesInstantiation();
                DeferredRsapiClientInstantiation();
                DeferredWebUtilityInstantiation();

                var importSettings = await WebUtility.RetrieveSettingsAsync(RsapiClient, ArtifactQueries, Helpers.Constants.FileNames.ConfigFileName);
                importSettings.Logger = Logger;

                DeferredImportApiLoaderInstantiation();
                ImportApiLoader.LoadImportApiDlls(importSettings.RelativityLibraryFolder, ExecutingPath);

                DeferredImportApiInstantiation(importSettings);
                DeferredPopulatorInstantiation(importSettings);

                await Populator.PopulateDataAsync();
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

        #region DeferredInstantiation

        private void DeferredImportApiLoaderInstantiation()
        {
            if (ImportApiLoader == null)
            {
                var currentRelativityVersion = typeof(kCura.EventHandler.Field).Assembly.GetName().Version;
                ImportApiLoader = new ImportApiLoader(currentRelativityVersion);
            }
        }
        private void DeferredLoggerInstantiation()
        {
            if (Logger == null)
            {
                Logger = Helper.GetLoggerFactory().GetLogger();
            }
        }
        private void DeferredImportApiInstantiation(DocumentImportSettings settings)
        {
            if (ImportApi == null)
            {
                try
                {
                    //https
                    var importApiUrl = Helpers.Constants.Protocols.Https + String.Format(Helpers.Constants.URLs.WebApiUrl, Helper.GetServicesManager().GetServicesURL().Host);
                    ImportApi = new ImportAPI(settings.RelativityUsername, settings.RelativityPassword, importApiUrl);
                }
                catch (Exception)
                {
                    //http
                    var importApiUrl = Helpers.Constants.Protocols.Http + String.Format(Helpers.Constants.URLs.WebApiUrl, Helper.GetServicesManager().GetServicesURL().Host);
                    ImportApi = new ImportAPI(settings.RelativityUsername, settings.RelativityPassword, importApiUrl);
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

        private void DeferredPopulatorInstantiation(DocumentImportSettings settings)
        {
            if (Populator == null)
            {
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
