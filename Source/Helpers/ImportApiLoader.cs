using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DP.Helpers.Interfaces;

namespace DP.Helpers
{
    public class ImportApiLoader : IImportApiLoader
    {
        public Version RelativityVersion;

        public ImportApiLoader(Version relativityVersion)
        {
            RelativityVersion = relativityVersion;
        }
        public void LoadImportApiDlls(string relativityLibraryFolder, string destination)
        {
            ValidateLibraryLocation(relativityLibraryFolder);
            CopyImportApiDllsFromRelativityLibrary(relativityLibraryFolder, destination);
            LoadAssemblies(destination);
        }

        private void ValidateLibraryLocation(string relativityLibraryFolder)
        {
            if (String.IsNullOrWhiteSpace(relativityLibraryFolder))
            {
                throw new Exception("Please enter a path to the Relativity Library Folder");
            }

            if (!Directory.Exists(relativityLibraryFolder))
            {
                throw new Exception($"Unable to access Relativity Library Folder: {relativityLibraryFolder}");
            }

            var missingImportApiDlls = ValidateAllImportApiDllsExists(relativityLibraryFolder);
            if (missingImportApiDlls.Any())
            {
                var printDlls = String.Join("<br />", missingImportApiDlls);
                throw new Exception($"The following Dlls are missing from your specified library location: {relativityLibraryFolder}<br />{printDlls}");
            }

        }

        private IEnumerable<string> ValidateAllImportApiDllsExists(string libraryLocation)
        {
            var missingDlls = new List<String>();
            var allImportApiDlls = GetAllImportAPIDllNames();

            foreach (var dll in allImportApiDlls)
            {
                var currentDllPath = Path.Combine(libraryLocation, dll);
                if (!File.Exists(currentDllPath))
                {
                    missingDlls.Add(dll);
                }
            }
            return missingDlls;
        }

        private void CopyImportApiDllsFromRelativityLibrary(string relativityLibraryFolder, string destination)
        {
            var allImportApiDlls = GetAllImportAPIDllNames();

            foreach (var importApiDll in allImportApiDlls)
            {
                var relativityPath = Path.Combine(relativityLibraryFolder, importApiDll);
                var fullFilePath = Path.Combine(destination, importApiDll);

                try
                {
                    var fileDirectory = Path.GetDirectoryName(fullFilePath);
                    if (!Directory.Exists(fileDirectory))
                    {
                        Directory.CreateDirectory(fileDirectory);
                    }
                    File.Copy(relativityPath, fullFilePath, true);
                }
                catch (Exception)
                {
                    //This exception is eaten because the file is most likely already in the app domain from a previous execution and we are ok if it is not copied
                }
            }
        }

        private IEnumerable<String> GetAllImportAPIDllNames()
        {
            var retVal = new List<string>();
            if (RelativityVersion >= Constants.RelativityOIUpdateVersion)
            {
                retVal.AddRange(Helpers.Constants.MainImportApiDlls.Concat(Helpers.Constants.ExtraineousImportApiDllsAfterOIUpdate));
            }
            else
            {
                retVal.AddRange(Helpers.Constants.MainImportApiDlls.Concat(Helpers.Constants.ExtraineousImportApiDlls));
            }
            return retVal;
        }

        private void LoadAssemblies(string path)
        {
            foreach (var importApiDll in Helpers.Constants.MainImportApiDlls)
            {
                var dllPath = Path.Combine(path, importApiDll);
                try
                {
                    using (var fs = new FileStream(dllPath, FileMode.Open))
                    {
                        var rawAssembly = new byte[fs.Length];
                        fs.Read(rawAssembly, 0, rawAssembly.Length);
                        Assembly.Load(rawAssembly);
                    }
                }
                catch (Exception)
                {
                    /*This exception is eaten because the file is most likely already in the app domain and therefore already loaded
                     * otherwise an exception will propagate up to the user if the importAPI is unaccessible*/
                }
            }
        }
    }
}
