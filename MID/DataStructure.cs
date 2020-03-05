using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MID
{
    class SearchStatus
    {
        private List<string> attrNameList;
        private List<string> attrValueList;
        private List<double> attrEnList;
        private List<double> valEnList;

        public SearchStatus()
        {
            attrValueList = new List<string>();
            attrNameList = new List<string>();
            attrEnList = new List<double>();
            valEnList = new List<double>();
        }
        public int Count
        {
            get
            {
                return attrNameList.Count;
            }
        }

        public void AddTuple(string attrName, string attrValue, double attrEn, double valEn)
        {
            attrNameList.Add(attrName);
            attrValueList.Add(attrValue);
            attrEnList.Add(attrEn);
            valEnList.Add(valEn);
        }

        public void DelTuple(string attrName, string attrValue)
        {
            int removeId = attrNameList.IndexOf(attrName);
            attrNameList.RemoveAt(removeId);
            attrValueList.RemoveAt(removeId);
            attrEnList.RemoveAt(removeId);
            valEnList.RemoveAt(removeId);
        }

        public (string, string, double, double) PopTuple(Random randomizer)
        {
            int removeIndex = randomizer.Next(attrNameList.Count);
            string removedAttr = attrNameList[removeIndex];
            string removedValue = attrValueList[removeIndex];
            double attrEn = attrEnList[removeIndex];
            double valEn = valEnList[removeIndex];
            attrNameList.RemoveAt(removeIndex);
            attrValueList.RemoveAt(removeIndex);
            attrEnList.RemoveAt(removeIndex);
            valEnList.RemoveAt(removeIndex);

            return (removedAttr, removedValue, attrEn, valEn);
        }

        public (string, string, double, double) PopMinEnTuple()
        {
            int removeIndex = IndexOfMin(valEnList);
            string removedAttr = attrNameList[removeIndex];
            string removedValue = attrValueList[removeIndex];
            double attrEn = attrEnList[removeIndex];
            double valEn = valEnList[removeIndex];
            attrNameList.RemoveAt(removeIndex);
            attrValueList.RemoveAt(removeIndex);
            attrEnList.RemoveAt(removeIndex);
            valEnList.RemoveAt(removeIndex);

            return (removedAttr, removedValue, attrEn, valEn);
        }

        private int IndexOfMin(List<double> valList)
        {
            double min = valList[0];
            int minIndex = 0;
            for (int i = 1; i < valList.Count; i++)
            {
                if (valList[i] < min)
                {
                    min = valList[i];
                    minIndex = i;
                }
            }
            return minIndex;
        }

        public (string, string) GetAttrComb(int i)
        {
            return (attrNameList[i], attrValueList[i]);
        }

        public void PrintCurrentStatus()
        {
            string outputString = "";
            for (int i = 0; i < attrNameList.Count; i++)
                outputString += attrNameList[i] + "=" + attrValueList[i] + ";";
            Console.WriteLine(outputString);
        }

        public HashSet<(string, string)> GetCurrentCombination()
        {
            HashSet<(string, string)> curCombination = new HashSet<(string, string)>();
            for (int i = 0; i < attrNameList.Count; i++)
                curCombination.Add((attrNameList[i], attrValueList[i]));
            return curCombination;
        }
    }

    class SearchResults
    {
        private List<double> objValueList;
        private List<HashSet<(string, string)>> attrCombinationList;
        private List<int[]> timeSeriesList;

        public SearchResults()
        {
            objValueList = new List<double>();
            attrCombinationList = new List<HashSet<(string, string)>>();
            timeSeriesList = new List<int[]>();
        }

        public int Count
        {
            get
            {
                return objValueList.Count;
            }
        }

        public void Add(HashSet<(string, string)> attrCombination, double objValue, int[] timeSeries)
        {
            objValueList.Add(objValue);
            attrCombinationList.Add(attrCombination);
            timeSeriesList.Add(timeSeries);
        }

        public int CheckSimilarity(int[] timeSeries)
        {
            double tmpSimilarity = 0.8;
            int renVal = -1;
            for (int i = 0; i < timeSeriesList.Count; i++)
            {
                double cosSimilarity = CalculateSimilarity(timeSeriesList[i], timeSeries);
                if (cosSimilarity > tmpSimilarity)
                    renVal = i;
            }
            return renVal;
        }

        public (HashSet<(string, string)>, double) getCombination(int id)
        {
            return (attrCombinationList[id], objValueList[id]);
        }
        private double CalculateSimilarity(int[] timeSeries1, int[] timeSeries2)
        {
            double powSum = 0.0;
            for (int i = 0; i < timeSeries1.Length; i++)
            {
                powSum += Math.Pow(timeSeries1[i] - timeSeries2[i], 2);
            }
            return 1.0 / (Math.Sqrt(powSum) + 1.0);
        }

        public double MinObjValue
        {
            get
            {
                if (objValueList.Count < 1)
                    return -1;
                return objValueList.Min();
            }
        }

        public void Remove(int id)
        {
            objValueList.RemoveAt(id);
            attrCombinationList.RemoveAt(id);
            timeSeriesList.RemoveAt(id);
        }

        public int MinObjId
        {
            get
            {
                return objValueList.IndexOf(objValueList.Min());
            }
        }

        public void ShowResults(string resultPath)
        {
            var sortedObjIndexList = objValueList.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderBy(x => x.Key).ToList();
            List<double> sortedObjValueList = sortedObjIndexList.Select(x => x.Key).ToList();
            List<int> sortedIndexList = sortedObjIndexList.Select(x => x.Value).ToList();
            sortedIndexList.Reverse();
            sortedObjValueList.Reverse();
            double threshold = sortedObjValueList[0] * 0.3;
            StreamWriter file = new StreamWriter(resultPath);
            for (int i = 0; i < sortedObjValueList.Count; i++)
            {
                if (sortedObjValueList[i] > threshold)
                {
                    string resultCombination = AttrCombinationToString(attrCombinationList[sortedIndexList[i]]);
                    file.WriteLine("{0} {1}", sortedObjValueList[i].ToString(), resultCombination);
                }
                else
                    break;
            }
            file.Close();
        }

        private string AttrCombinationToString(HashSet<(string, string)> attrComb)
        {
            string outputString = "";
            foreach ((string attrName, string attrValue) in attrComb)
                outputString += attrName + "=" + attrValue + ";";
            return outputString;
        }
    }
}