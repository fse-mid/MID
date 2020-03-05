using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MID
{
    class DataOpts
    {
        public static void ReadDataWithEntropy(out List<string[]> data, out List<string> header, out Dictionary<string, double> attrEntropyDic, out List<DateTime> timeList, out Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Dictionary<string, string> config)
        {
            string filePath = config["DataPath"];
            char lineSeperator = config["LineSeperator"].ToCharArray()[0];
            string timeColName = config["TimeColName"];
            string valueColName = config["ValueColName"];
            data = new List<string[]>();
            header = new List<string>();
            attrEntropyDic = new Dictionary<string, double>();
            timeList = new List<DateTime>();
            attrValueEntropyDic = new Dictionary<string, Dictionary<string, double>>();

            Dictionary<string, Dictionary<string, int>> attrValueCountDic = new Dictionary<string, Dictionary<string, int>>();

            using (StreamReader fileReader = new StreamReader(filePath))
            {
                //Get header
                string[] line = fileReader.ReadLine().Split(lineSeperator);
                int timeIndex = 0; int valueIndex = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == timeColName)
                    {
                        timeIndex = i;
                        header.Add(line[i]);
                    }
                    else if (line[i] == valueColName)
                    {
                        valueIndex = i;
                        header.Add(line[i]);
                    }
                    else
                    {
                        header.Add(line[i]);
                        attrValueCountDic.Add(line[i], new Dictionary<string, int>());
                    }
                }

                //Get data
                string[] transaction;
                while (!fileReader.EndOfStream)
                {
                    transaction = fileReader.ReadLine().Split(lineSeperator);
                    timeList.Add(DateTime.Parse(transaction[timeIndex]));
                    data.Add(transaction);
                    for (int i = 0; i < header.Count; i++)
                    {
                        if (i != timeIndex && i != valueIndex)
                        {
                            if (!attrValueCountDic[header[i]].ContainsKey(transaction[i]))
                                attrValueCountDic[header[i]].Add(transaction[i], 1);
                            else
                                attrValueCountDic[header[i]][transaction[i]] += 1;
                        }
                    }
                }

                foreach (var attr in attrValueCountDic.Keys)
                {
                    if (attrValueCountDic[attr].Count > 1)
                    {
                        double attrEntropy = 0.0;
                        attrValueEntropyDic.Add(attr, new Dictionary<string, double>());
                        foreach (var key in attrValueCountDic[attr].Keys)
                        {
                            double valEntropy = attrValueCountDic[attr][key] / (double)data.Count;
                            attrEntropy += valEntropy;
                            attrValueEntropyDic[attr].Add(key, valEntropy);
                        }
                        attrEntropyDic.Add(attr, attrEntropy);
                    }     
                }
            }
            return;
        }

        public static void GetTimeIndex(out Dictionary<DateTime, int> dateTimeDictionary, Dictionary<string, string> dataConfig)
        {
            dateTimeDictionary = new Dictionary<DateTime, int>();
            DateTime startDate = DateTime.Parse(dataConfig["StartDate"]).Date;
            DateTime endDate = DateTime.Parse(dataConfig["EndDate"]).Date;
            int dateIntIndex = 0;
            for (DateTime dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                dateTimeDictionary.Add(dt, dateIntIndex++);
        }

        public static void GetTimeSeriesData(List<DateTime> timeCol, List<int> valueCol, SearchStatus curSearchStatus, List<string[]> data, List<string> header, List<DateTime> timeList, int valueIndex)
        {
            
            List<int> tmpAttrIndexList = new List<int>();
            List<string> tmpAttrValueList = new List<string>();
            for (int i = 0; i < curSearchStatus.Count; i++)
            {
                (string attrName, string attrValue) = curSearchStatus.GetAttrComb(i);
                tmpAttrIndexList.Add(header.IndexOf(attrName));
                tmpAttrValueList.Add(attrValue);
            }

            for (int i = 0; i< data.Count; i++)
            {
                bool chooseRow = true;
                for (int k = 0; k < tmpAttrIndexList.Count; k++)
                    chooseRow &= data[i][tmpAttrIndexList[k]] == tmpAttrValueList[k];

                if (chooseRow)
                {
                    //Console.WriteLine(chooseRow);
                    timeCol.Add(timeList[i]);
                    valueCol.Add(int.Parse(data[i][valueIndex]));
                }
            }
            return;
        }

        
        public static void TimeSeriesPostProcess(out int[] dailyIssueValue, List<DateTime> timeCol, List<int> valueCol, Dictionary<DateTime, int> dateTimeDic)
        {
            dailyIssueValue = new int[dateTimeDic.Count];

            for (int i = 0; i < dailyIssueValue.Length; i++)
                dailyIssueValue[i] = 0;

            for (int i = 0; i < timeCol.Count; i++)
            {
                int tmpIndex = dateTimeDic[timeCol[i]];
                dailyIssueValue[tmpIndex] += valueCol[i];
            }

            return;
        }

        public static (double, int[]) CalculateObjValue(int[] dailyIssueValue)
        {
            double objValue = 0.0;
            double mean = dailyIssueValue.Average();
            int[] filteredValueList = new int[dailyIssueValue.Length];
            for (int i = 0; i < dailyIssueValue.Length; i++)
            {
                if (dailyIssueValue[i] < mean)
                    filteredValueList[i] = 0;
                else
                    filteredValueList[i] = dailyIssueValue[i];
            }
            for (int i = 1; i < filteredValueList.Length; i++)
            {
                if (filteredValueList[i - 1] == 0 && filteredValueList[i] > 0)
                {
                    //calculate plnp
                    double plnp = CalPlnp(dailyIssueValue, i);
                    if (plnp > objValue)
                        objValue = plnp;
                }
            }
            return (objValue, filteredValueList);
        }

        private static double CalPlnp(int[] value, int cgPoint)
        {
            double p_a = 0.0;
            double p_b = 0.0;
            for (int i = 0; i < cgPoint; i++)
                p_b += value[i];
            for (int i = cgPoint; i < value.Length; i++)
                p_a += value[i];
            p_a = p_a / (value.Length - cgPoint);
            p_b = p_b / cgPoint;
            if (p_a / p_b < 1)
                return 0;
            if (p_b < 1)
                p_b = 1.0;
            return p_a * Math.Log(p_a / p_b);
        }

        private static List<int> GetRowIndex(SearchStatus curSearchStatus, List<string[]> rowData, List<string> header)
        {
            List<int> rowIndex = new List<int>();
            List<int> tmpAttrIndexList = new List<int>();
            List<string> tmpAttrValueList = new List<string>();

            for (int i = 0; i < curSearchStatus.Count; i++)
            {
                (string attrName, string attrValue) = curSearchStatus.GetAttrComb(i);
                tmpAttrIndexList.Add(header.IndexOf(attrName));
                tmpAttrValueList.Add(attrValue);
            }

            for (int i = 0; i < rowData.Count; i++)
            {
                string[] dataRow = rowData[i];
                bool chooseRow = true;
                for (int j = 0; j < tmpAttrIndexList.Count; j++)
                {
                    if (dataRow[j] != tmpAttrValueList[j])
                        chooseRow = false;
                }
                if (chooseRow)
                    rowIndex.Add(i);
            }

            return rowIndex;
        }

        public static string ChooseAttrValue(string attrName, List<KeyValuePair<string, double>> valueEnList, Dictionary<string, int> tabuDic, int curStep, int tabuSize)
        {
            int i = 0;
            foreach (var valTuple in valueEnList)
            {
                string tabuKey = attrName + ":" + valTuple.Key;
                if (tabuDic.ContainsKey(tabuKey))
                {
                    int stepDiff = curStep - tabuDic[tabuKey];
                    if (stepDiff > tabuSize)
                    {
                        tabuDic[tabuKey] = curStep;
                        return valTuple.Key;
                    }
                }
                else
                {
                    tabuDic.Add(tabuKey, curStep);
                    return valTuple.Key;
                }
                i++;
            }
            return string.Empty;
        }
    }
}
