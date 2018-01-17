using System;
using kCura.Vendor.Castle.Core.Resource;

namespace DP.Helpers
{
    public class Constants
    {
        public const int MaximumNumberOfDocuments = 20000;

        public static Version RelativityOIUpdateVersion = new Version("9.5.253.62");
        public class URLs
        {
            public const string WebApiUrl = "{0}/Relativitywebapi/";
            public const string ResourceFileDownload = "{0}/Relativity.Distributed/Download.aspx?AppID=-1&AssemblyArtifactID={1}&AuthenticationToken={2}";
        }

        public class Protocols
        {
            public const string Https = "https://";
            public const string Http = "http://";
        }

        public class FileNames
        {
            public const string ImageName = "Sample.tif";
            public const string NativeName = "native.htm";
            public const string ProductionImageName = "Prod.tif";
            public const string ConfigFileName = "DataPopulateConfiguration.Json";
        }

        public class ArtifactTypeNames
        {
            public const string ResourceFile = "Resource File";
        }

        public class ArtifactTypeIDs
        {
            public const int Document = 10;
            public const int Production = 17;
        }

        public static readonly string[] MainImportApiDlls =
        {
            "kCura.Relativity.DataReaderClient.dll",
            "kCura.Relativity.ImportAPI.dll",

        };

        public static readonly string[] ExtraineousImportApiDlls =
        {
            "FreeImage.dll",
            "FreeImageNET.dll",
            "itextsharp.dll",
            "kCura.Data.dll",
            "kCura.ImageValidator.dll",
            "kCura.OI.FileID.dll",
            "kCura.Utility.dll",
            "kCura.Windows.Forms.dll",
            "kCura.Windows.Process.dll",
            "kCura.WinEDDS.dll",
            "kCura.WinEDDS.ImportExtension.dll",
            "Relativity.dll",
            "sccfi.dll",
            "sccfut.dll",
            "scclo.dll",
            "sccut.dll",
            "wvcore.dll"
        };

        public static readonly string[] ExtraineousImportApiDllsAfterOIUpdate = 
        {
            "FreeImage.dll",
            "FreeImageNET.dll",
            "itextsharp.dll",
            "kCura.dll",
            "kCura.ImageValidator.dll",
            "kCura.OI.FileID.dll",
            "kCura.Windows.Forms.dll",
            "kCura.Windows.Process.dll",
            "kCura.WinEDDS.dll",
            "kCura.WinEDDS.ImportExtension.dll",
            "oi.dll",
            "Relativity.dll",
            "oi\\cmmap000.bin",
            "oi\\oilink.exe",
            "oi\\oilink.jar",
            "oi\\sccca-R1.dll",
            "oi\\sccch-R1.dll",
            "oi\\sccda-R1.dll",
            "oi\\sccex-R1.dll",
            "oi\\sccfa-R1.dll",
            "oi\\sccfi-R1.dll",
            "oi\\sccfmt-R1.dll",
            "oi\\sccfnt-R1.dll",
            "oi\\sccfut-R1.dll",
            "oi\\sccind-R1.dll",
            "oi\\scclo-R1.dll",
            "oi\\sccut-R1.dll",
            "oi\\wvcore-R1.dll"
        };


    }
}
