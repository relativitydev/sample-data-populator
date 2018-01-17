using System.IO;
using System.Text;
using System.Threading.Tasks;
using DP.Helpers.Models;
using NUnit.Framework;

namespace DP.Helpers.NUnit
{
    [TestFixture]
    public class DocumentImportSettingsTests
    {
        [Description("All properties are set on Document Import Settings Model")]
        [Test]
        public async Task ExecuteAsync_DocumentImportPropertiesAreSetCorrectly()
        {
            var username = "test@test.com";
            var password = "admin123";
            var relLibraryPath = @"C:\\Program Files\\kCura Corporation\\Relativity\\Library";
            var numDoc = 10;
            var importImagesWithDocs = false;
            var importProductionImagesWithDocs = true;

            var settings = new DocumentImportSettings(CreateJsonConfigStream(username, password, relLibraryPath, numDoc,
                importImagesWithDocs, importProductionImagesWithDocs));

            Assert.AreEqual(username, settings.RelativityUsername);
            Assert.AreEqual(password, settings.RelativityPassword);
            Assert.AreEqual(relLibraryPath.Replace("\\\\","\\"), settings.RelativityLibraryFolder);
            Assert.AreEqual(numDoc, settings.NumberOfDocuments);
            Assert.AreEqual(importImagesWithDocs, settings.ImportImagesWithDocuments);
            Assert.AreEqual(importProductionImagesWithDocs, settings.ImportProductionImagesWithDocuments);
        }

        [Description("Make sure the maximum number of documents is enforced")]
        [Test]
        public async Task ExecuteAsync_MaxDocumentLimitEnforced()
        {
            var username = "test@test.com";
            var password = "admin123";
            var relLibraryPath = @"C:\\Program Files\\kCura Corporation\\Relativity\\Library";
            var numDoc = Helpers.Constants.MaximumNumberOfDocuments * 2;
            var importImagesWithDocs = false;
            var importProductionImagesWithDocs = true;

            var settings = new DocumentImportSettings(CreateJsonConfigStream(username, password, relLibraryPath, numDoc,
                importImagesWithDocs, importProductionImagesWithDocs));

            Assert.AreEqual(Helpers.Constants.MaximumNumberOfDocuments, settings.NumberOfDocuments);
        }

        private Stream CreateJsonConfigStream(string username, string password, string relativityLibraryFolder, int numberOfDocuments,
            bool importImagesWithDocuments, bool importProductionImagesWithDocuments)
        {
            var json = "{" +
                       $@"""RelativityUsername"": ""{username}""," +
                       $@"""RelativityPassword"": ""{password}""," +
                       $@"""RelativityLibraryFolder"": ""{relativityLibraryFolder}""," +
                       $@"""NumberOfDocuments"": {numberOfDocuments}," +
                       $@"""ImportImagesWithDocuments"": {importImagesWithDocuments.ToString().ToLower()}," +
                       $@"""ImportProductionImagesWithDocuments"": {importProductionImagesWithDocuments.ToString().ToLower()}," +
                       "}";
            var bytes = Encoding.UTF8.GetBytes(json);
            return new MemoryStream(bytes);
        }

        private Stream CreateJsonConfigStream(string wholeJsonFile)
        {
            var bytes = Encoding.UTF8.GetBytes(wholeJsonFile);
            return new MemoryStream(bytes);
        }
    }
}
