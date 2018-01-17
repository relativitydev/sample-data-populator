using System.Threading.Tasks;
using kCura.Relativity.Client;

namespace DP.Helpers.RSAPI
{
    public interface IArtifactQueries
    {
        Task<kCura.Relativity.Client.DTOs.Field> GetIdentityFieldAsync(IRSAPIClient rsapiClient, int workspaceArtifactID, int artifactTypeID);
        Task<int> QueryResourceFileArtifactIDAsync(IRSAPIClient rsapiClient, string fileName);
        Task<string> RequestAuthTokenAsync(IRSAPIClient rsapiClient);
        Task<int> QueryProductionSetArtifactID(IRSAPIClient rsapiClient, int workspaceArtifactID, string nameOfProductionSet);
    }
}
