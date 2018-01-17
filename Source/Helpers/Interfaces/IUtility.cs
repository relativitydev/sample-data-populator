using System;
using System.Threading.Tasks;
using DP.Helpers.Models;
using DP.Helpers.RSAPI;
using kCura.Relativity.Client;

namespace DP.Helpers.Interfaces
{
    public interface IUtility
    {
        Task DownloadFileAsync(Uri fileWebAddress, string localDownloadLocation);

        Task<DocumentImportSettings> RetrieveSettingsAsync(IRSAPIClient rsapiClient, IArtifactQueries artifactQueries, string configFileName);
    }
}
