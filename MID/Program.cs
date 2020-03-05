using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;

namespace MID
{
    class Program
    {
        static string[] sources = { "8-4-200-1", "8-4-200-2", "8-4-300-1", "8-4-300-2", "8-4-400-1", "8-4-400-2", "8-6-200-1", "8-6-200-2", "8-8-200-1", "8-8-200-2", "10-4-200-1", "10-4-200-2", "12-4-200-1", "12-4-200-2" };
        static string source = null;

        static void Main(string[] args)
        {
            int[] bmsList = { 5, 10, 15, 20, 25, 30 };
            foreach (string eachSource in sources)
            {
                source = eachSource;
                Console.WriteLine(eachSource);
                //StreamWriter exeTimeFileWriter = new StreamWriter("../../data/" + source + "exe_time.txt");
                var startTime = DateTime.Now;
                for (int i = 0; i < 50; i++)
                {
                    var tmpStartTime = DateTime.Now;
                    doHeuristic(i);
                    var tmpEndTime = DateTime.Now;
                    //exeTimeFileWriter.WriteLine("File {0} Execution Time: {1}", i, tmpEndTime - tmpStartTime);
                    Console.WriteLine("{0}: {1}", i, (tmpEndTime - tmpStartTime).TotalSeconds);
                }
                var endTime = DateTime.Now;
                //exeTimeFileWriter.WriteLine("Total Execution Time: {0}", totalTime);
                //exeTimeFileWriter.WriteLine("{0}", totalTime.TotalSeconds);
                //exeTimeFileWriter.Close();
                Console.WriteLine("Total Execution Time: {0}", (endTime - startTime).TotalSeconds);
            }
        }

        private static (Dictionary<string, string>, Dictionary<string, int>) BuildConfig(int i)
        {
            string resourceDirectory = "../../data/" + source + "/";

            string source_file_name = "syn_" + i + ".csv";

            string lineSeperator = ",";

            string target_file_name = "Results_syn_" + i + ".txt";

            // the input file path.
            string dataPath = new FileInfo(resourceDirectory + source_file_name).FullName;

            // the output file path.
            string resultPath = new FileInfo(resourceDirectory + target_file_name).FullName;

            string timeColName = "date";
            string valueColName = "value";
            string startDate = "2019-03-01";
            string endDate = "2019-04-29";

            int tabuSize = 16;
            int modeProb = 20;
            int maxSearchLen = 5;
            int resultSize = 10;
            int bmsSize = 10;

            Dictionary<string, string> dataConfig = new Dictionary<string, string>();
            dataConfig.Add("TimeColName", timeColName);
            dataConfig.Add("ValueColName", valueColName);
            dataConfig.Add("LineSeperator", lineSeperator);
            dataConfig.Add("DataPath", dataPath);
            dataConfig.Add("ResultPath", resultPath);
            dataConfig.Add("StartDate", startDate);
            dataConfig.Add("EndDate", endDate);

            Dictionary<string, int> algConfig = new Dictionary<string, int>();
            algConfig.Add("TabuSize", tabuSize);
            algConfig.Add("ModeProb", modeProb);
            algConfig.Add("MaxSearchLen", maxSearchLen);
            algConfig.Add("ResultSize", resultSize);
            algConfig.Add("BmsSize", bmsSize);

            return (dataConfig, algConfig);

        }

        private static void doHeuristic(object o)
        {
            Random rnd = new Random();
            int i = (int)o;
            Console.WriteLine("Mining dataset {0}", i);

            (Dictionary<string, string> dataConfig, Dictionary<string, int> algConfig) = BuildConfig(i);

            List<string[]> rowData;
            List<string> header;
            List<DateTime> timeList;
            Dictionary<string, double> attrEntropyDic;
            Dictionary<string, Dictionary<string, double>> attrValueEntropyDic;

            DataOpts.ReadDataWithEntropy(out rowData, out header, out attrEntropyDic, out timeList, out attrValueEntropyDic, dataConfig);

            Dictionary<DateTime, int> dateTimeDictionary;
            DataOpts.GetTimeIndex(out dateTimeDictionary, dataConfig);
            SearchStatus curSearchStatus = new SearchStatus();

            int curSearchStep = 0;
            int maxSearchLen = algConfig["MaxSearchLen"];
            int valueIndex = header.IndexOf(dataConfig["ValueColName"]);
            int resultSize = algConfig["ResultSize"];
            int tabuSize = algConfig["TabuSize"];
            int bmsSize = algConfig["BmsSize"];
            int modeProb = algConfig["ModeProb"];

            int maxSearchStep;

            if (attrEntropyDic.Keys.Count > 15)
                maxSearchStep = 20000;
            else
                maxSearchStep = 10000;


            SearchResults searchResults = new SearchResults();
            Dictionary<string, int> tabuDic = new Dictionary<string, int>();
            int updatedStep = 0;
            while (curSearchStep < maxSearchStep)
            {
                int prob = rnd.Next(0, 100);
                int nextOpt = getSearchOpt(curSearchStatus, maxSearchLen, rnd);

                bool doSearch = false;
                if (prob < modeProb)
                    doSearch = RandomOpts.doRandomSearch(nextOpt, curSearchStatus, attrEntropyDic, attrValueEntropyDic, rnd);
                else
                    doSearch = GreedyOpts.doGreedySearch(nextOpt, curSearchStatus, attrEntropyDic, attrValueEntropyDic, tabuDic, curSearchStep, tabuSize, bmsSize, rnd);

                if (!doSearch)
                    continue;
                List<DateTime> timeCol = new List<DateTime>();
                List<int> valueCol = new List<int>();
                DataOpts.GetTimeSeriesData(timeCol, valueCol, curSearchStatus, rowData, header, timeList, valueIndex);
                if (valueCol.Count < 1)
                    continue;
                int[] dailyIssueValue;
                DataOpts.TimeSeriesPostProcess(out dailyIssueValue, timeCol, valueCol, dateTimeDictionary);
                (double objValue, int[] filteredTimeSeries) = DataOpts.CalculateObjValue(dailyIssueValue);
                //curSearchStatus.PrintCurrentStatus();
                //Console.WriteLine(objValue);

                if (objValue > searchResults.MinObjValue)
                {
                    updatedStep = curSearchStep;
                    int similarId = searchResults.CheckSimilarity(filteredTimeSeries);

                    // no similar results in searchResults
                    if (similarId < 0)
                    {
                        if (searchResults.Count < resultSize)
                            searchResults.Add(curSearchStatus.GetCurrentCombination(), objValue, filteredTimeSeries);
                        else
                        {
                            int minId = searchResults.MinObjId;
                            searchResults.Remove(minId);
                            searchResults.Add(curSearchStatus.GetCurrentCombination(), objValue, filteredTimeSeries);
                        }
                    }
                    else
                    {
                        (HashSet<(string, string)> preCombination, double preObjValue) = searchResults.getCombination(similarId);
                        HashSet<(string, string)> curCombination = curSearchStatus.GetCurrentCombination();
                        if (preCombination.Equals(curCombination))
                            continue;

                        if (preObjValue < objValue)
                        {
                            searchResults.Remove(similarId);
                            searchResults.Add(curCombination, objValue, filteredTimeSeries);
                        }
                    }
                }

                if (curSearchStep - updatedStep > 300)
                    break;

                curSearchStep++;
            }
            string resultPath = dataConfig["ResultPath"];
            searchResults.ShowResults(resultPath);
        }

        public static int getSearchOpt(SearchStatus curSearchStatus, int maxSearchLen, Random rnd)
        {
            if (curSearchStatus.Count == 0)
                return 2;
            else if (curSearchStatus.Count == maxSearchLen)
            {
                List<int> candidateOpts = new List<int> { 0, 1, 3 };
                List<double> optsProb = new List<double> { 0.35, 0.35, 0.3 };
                double diceRoll = rnd.NextDouble();
                double cumulative = 0.0;
                for (int i = 0; i < candidateOpts.Count; i++)
                {
                    cumulative += optsProb[i];
                    if (diceRoll < cumulative)
                        return candidateOpts[i];
                }
                return 0;
            }
            else if (curSearchStatus.Count == 1)
            {
                List<int> candidateOpts = new List<int> { 0, 1, 2 };
                List<double> optsProb = new List<double> { 0.35, 0.35, 0.3 };
                double diceRoll = rnd.NextDouble();
                double cumulative = 0.0;
                for (int i = 0; i < candidateOpts.Count; i++)
                {
                    cumulative += optsProb[i];
                    if (diceRoll < cumulative)
                        return candidateOpts[i];
                }
                return 0;
            }
            else
            {
                List<int> candidateOpts = new List<int> { 0, 1, 2, 3 };
                List<double> optsProb = new List<double> { 0.25, 0.25, 0.25, 0.25 };
                double diceRoll = rnd.NextDouble();
                double cumulative = 0.0;
                for (int i = 0; i < candidateOpts.Count; i++)
                {
                    cumulative += optsProb[i];
                    if (diceRoll < cumulative)
                        return candidateOpts[i];
                }
                return 0;
            }

        }
    }
}
