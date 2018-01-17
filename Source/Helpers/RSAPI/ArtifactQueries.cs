using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using Relativity.API;

namespace DP.Helpers.RSAPI
{
    public class ArtifactQueries : IArtifactQueries
    {
        public IAPILog Logger { get; set; }

        public ArtifactQueries(IAPILog logger)
        {
            Logger = logger;
        }

        public async Task<kCura.Relativity.Client.DTOs.Field> GetIdentityFieldAsync(IRSAPIClient rsapiClient, int workspaceArtifactID, int artifactTypeID)
        {
            kCura.Relativity.Client.DTOs.Field retVal = null;

            var query = new kCura.Relativity.Client.DTOs.Query<kCura.Relativity.Client.DTOs.Field>()
            {
                Condition = new BooleanCondition(kCura.Relativity.Client.DTOs.FieldFieldNames.IsIdentifier, kCura.Relativity.Client.BooleanConditionEnum.EqualTo, true),
                Fields = new List<FieldValue>()
                {
                    new FieldValue(FieldFieldNames.ObjectType),
                    new FieldValue(FieldFieldNames.Name)
                }
            };

            try
            {
                rsapiClient.APIOptions = new APIOptions() { WorkspaceID = workspaceArtifactID };
                var resultSet = await Task.Run(() => rsapiClient.Repositories.Field.Query(query));
                if (resultSet.Success && resultSet.Results.Any())
                {
                    var identifierFieldForSelectedArtifactType = resultSet.Results.FirstOrDefault(x => x.Artifact.ObjectType.DescriptorArtifactTypeID.Value == artifactTypeID);
                    if (identifierFieldForSelectedArtifactType != null)
                    {
                        retVal = identifierFieldForSelectedArtifactType.Artifact;
                    }
                    else
                    {
                        Logger.LogError("Unable to find identifying document field");
                    }
                }
                else
                {
                    Logger.LogError("Unable to find identifying document field");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while querying identifying document field: {ex}");
            }
            return retVal;
        }

        public async Task<int> QueryProductionSetArtifactID(IRSAPIClient rsapiClient, int workspaceArtifactID, string nameOfProductionSet)
        {
            int retVal = 0;

            var query = new kCura.Relativity.Client.DTOs.Query<kCura.Relativity.Client.DTOs.RDO>()
            {
                ArtifactTypeName = ArtifactTypeNames.Production,
                Condition = new TextCondition("Name", TextConditionEnum.EqualTo, nameOfProductionSet),
                Fields = kCura.Relativity.Client.DTOs.FieldValue.NoFields
            };

            try
            {
                rsapiClient.APIOptions = new APIOptions() { WorkspaceID = workspaceArtifactID };
                var resultSet = await Task.Run(() => rsapiClient.Repositories.RDO.Query(query));
                if (resultSet.Success && resultSet.Results.Any())
                {
                    retVal = resultSet.Results[0].Artifact.ArtifactID;
                }
                else
                {
                    Logger.LogError($"Unable to find Production Set: {nameOfProductionSet}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while querying identifying document field: {ex}");
            }
            return retVal;
        }

        public async Task<int> QueryResourceFileArtifactIDAsync(IRSAPIClient rsapiClient, string fileName)
        {
            var retVal = 0;
            try
            {
                rsapiClient.APIOptions = new APIOptions() { WorkspaceID = -1 };
                var query = new kCura.Relativity.Client.DTOs.Query<RDO>()
                {
                    ArtifactTypeName = Constants.ArtifactTypeNames.ResourceFile,
                    Condition = new TextCondition("Name", TextConditionEnum.EqualTo, fileName),
                    Fields = FieldValue.AllFields
                };
                var results = await Task.Run(() => rsapiClient.Repositories.RDO.Query(query));
                if (results.Success && results.TotalCount > 0)
                {
                    retVal = results.Results[0].Artifact.ArtifactID;
                }
                else
                {
                    Logger.LogError("Unable to find configuration file");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"error while querying resource file ArtifactID: {ex}");
            }
            return retVal;
        }

        public async Task<string> RequestAuthTokenAsync(IRSAPIClient rsapiClient)
        {
            String retVal = null;
            try
            {
                rsapiClient.APIOptions = new APIOptions() { WorkspaceID = -1 };
                var readResult =
                    await Task.Run(() => rsapiClient.GenerateRelativityAuthenticationToken(rsapiClient.APIOptions));
                if (readResult.Success)
                {
                    retVal = readResult.Artifact.getFieldByName("AuthenticationToken").ToString();
                }
                else
                {
                    Logger.LogError("Unable to receive authorization token");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"error while requesting authorization token: {ex}");
            }

            return retVal;
        }
    }
}
