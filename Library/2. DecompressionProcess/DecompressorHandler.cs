﻿using DS_ProgramingChallengeLibrary.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DS_ProgramingChallengeLibrary
{
    public class DecompressorHandler : IDecompressorHandler
    {
        private readonly ILogger _log;
        private readonly IConfiguration _config;

        public DecompressorHandler(ILogger<DecompressorHandler> log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }
        public void DecompressFiles()
        {
            string fileDownloadPath = GeneralHelper.GetDownloadedFilesPath(_config);
            foreach (var fileNamePath in Directory.GetFiles(fileDownloadPath))
            {
                _log.LogInformation("Decompressing Data: {0}", fileNamePath);
                DecompressHelper.DecompressFile(fileNamePath, true);
                _log.LogInformation("Decompressed: {0}", fileNamePath);
            }
        }

        public string DecompressFile(string fileNamePath)
        {
            string newFileNamePath = string.Empty;

            _log.LogInformation("Decompressing data: {0}", fileNamePath);
            DecompressHelper.DecompressFile(fileNamePath, out newFileNamePath);
            _log.LogInformation("Decompressing data finished.");
            return newFileNamePath;
        }
    }
}
