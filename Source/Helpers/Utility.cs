using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DP.Helpers.Interfaces;
using DP.Helpers.Models;
using DP.Helpers.RSAPI;
using kCura.Relativity.Client;
using Relativity.API;

namespace DP.Helpers
{
    public class Utility : IUtility
    {
        public IAPILog Logger { get; set; }

        public Utility(IAPILog logger)
        {
            Logger = logger;
        }
        public async Task DownloadFileAsync(Uri fileWebAddress, string localDownloadLocation)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                        client.Headers.Add("Cache-Control", "no-cache");
                        client.DownloadFile(fileWebAddress, localDownloadLocation);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Unable to download file: {fileWebAddress}. {ex}");
                    throw;
                }

            });
        }

        public string GenerateConfigFilePath()
        {
            var fileName = $"PopulatorConfigFile_{Guid.NewGuid().ToString()}_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.Json";
            var path = Path.GetTempPath();
            return Path.Combine(path, fileName);
        }

        public async Task<DocumentImportSettings> RetrieveSettingsAsync(IRSAPIClient rsapiClient, IArtifactQueries artifactQueries, string configFileName)
        {
            DocumentImportSettings retVal = null;

            var configFileArtifactID = await artifactQueries.QueryResourceFileArtifactIDAsync(rsapiClient, configFileName);

            if (configFileArtifactID > 0)
            {
                var authToken = await artifactQueries.RequestAuthTokenAsync(rsapiClient);
                if (authToken != null)
                {

                    var fileDownloadLocation = await DownloadConfigFile(rsapiClient, configFileArtifactID, authToken);
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

        private async Task<string> DownloadConfigFile(IRSAPIClient rsapiClient, int configFileArtifactID, string authToken)
        {
            var fileDownloadLocation = GenerateConfigFilePath();
            var domain = rsapiClient.EndpointUri.Host;
            var urlWIthoutProtocol = String.Format(Helpers.Constants.URLs.ResourceFileDownload,
                domain,
                configFileArtifactID,
                authToken);

            try
            {
                //https
                var configFileUrl = Helpers.Constants.Protocols.Https + urlWIthoutProtocol;
                await DownloadFileAsync(new Uri(configFileUrl), fileDownloadLocation);
            }
            catch (Exception)
            {
                //http
                var configFileUrl = Helpers.Constants.Protocols.Http + urlWIthoutProtocol;
                await DownloadFileAsync(new Uri(configFileUrl), fileDownloadLocation);
            }
            return fileDownloadLocation;
        }
    }
}
