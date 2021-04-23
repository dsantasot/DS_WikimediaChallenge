﻿
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS_ProgramingChallengeLibrary.Helpers;
using DS_ProgramingChallengeLibrary.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DS_ProgramingChallengeLibrary
{
    public class FileParser : IFileParser
    {
        private readonly ILogger _log;
        private readonly IConfiguration _config;
        private readonly IFileSystem _fileSystem;

        public FileParser(ILogger<FileParser> log, IConfiguration config, IFileSystem fileSystem)
        {
            _log = log;
            _config = config;
            _fileSystem = fileSystem;
        }

        public Task<string> TransformDataAsync(string fileNamePath)
        {
            _log.LogInformation("Transforming data: {fileNamePath}", fileNamePath);
            var separator = new char[0];
            List<DataModelSummary> dataModel;
            List<DataModelSummary> dataModelSum;

            lock (this)
            {
                dataModel = GetDataModelSumFromFile(fileNamePath, separator);
                dataModelSum = GroupBySumData(dataModel);
                _log.LogInformation("Transforming data finished.");
            }

            return _fileSystem.SaveDataAsync(dataModelSum, fileNamePath);

            //return fileNamePath;

        }

        public Task<IEnumerable<OutputModel>> CountDataAsync(string fileNamePath)
        {
            _log.LogInformation("Transforming data: {fileNamePath}", fileNamePath);
            var separator = new char[0];
            List<DataModelSummary> dataModel;
            IEnumerable<OutputModel> outputModel;

            try
            {
                lock (this)
                {
                    dataModel = GetDataModelSumFromFile(fileNamePath, separator);

                    _log.LogInformation("Transforming data finished.");
                }

                var resultGroupByCount = GroupBySumData(dataModel);
                var resultGroupByMax = GroupByMaxData(resultGroupByCount);

                outputModel = from r in resultGroupByMax
                              join g in resultGroupByCount on new { r.domain_code, r.count_views } equals new { g.domain_code, g.count_views }
                              select new OutputModel
                              {
                                  domain_code = r.domain_code,
                                  page_title = g.page_title,
                                  max_count_views = r.count_views
                              };
            }
            finally
            {
                GC.Collect();
            }

            return Task.Run(() =>
            {
                return outputModel;
            });
        }

        private List<DataModelSummary> GetDataModelSumFromFile(string fileNamePath, char[] separator)
        {
            List<DataModelSummary> dataModel = new();
            try
            {
                using (FileStream fs = File.Open(fileNamePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (BufferedStream bs = new BufferedStream(fs))
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            var columns = line.Split(separator);

                            string domain_code = columns[0];
                            string page_title = columns[1];

                            if (int.TryParse(columns[2], out int count_views) == false)
                            {
                                foreach (var item in columns)
                                {
                                    if (int.TryParse(item, out count_views))
                                        break;
                                }
                            }

                            dataModel.Add(new DataModelSummary()
                            {
                                domain_code = domain_code,
                                page_title = page_title,
                                count_views = count_views
                            });
                        }
                    }
                }
            }
            finally
            {
                GC.Collect();
            }

            return dataModel;
        }

        private List<DataModelSummary> GroupBySumData(List<DataModelSummary> dataModel)
        {
            return (from e in dataModel
                   group e by new { e.domain_code, e.page_title } into gb
                   select new DataModelSummary
                   {
                       domain_code = gb.Key.domain_code,
                       page_title = gb.Key.page_title,
                       count_views = gb.Sum(e => e.count_views)
                   }).ToList();
        }

        private IEnumerable<DataModelSummary> GroupByMaxData(List<DataModelSummary> containedData)
        {
            return containedData
               .GroupBy(c => new { c.domain_code })
               .Select(gb => new DataModelSummary()
               {
                   domain_code = gb.Key.domain_code,
                   count_views = gb.Max(x => x.count_views)
               })
               .OrderByDescending(i => i.count_views)
               .Take(100);
        }
    }
}
