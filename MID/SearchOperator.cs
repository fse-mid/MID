using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MID
{
    class RandomOpts
    {
        private static void doRandomSwapValue(SearchStatus curSearchStatus, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Random rnd)
        {
            (string attrName, string attrValue, double attrEn, double valEn) = curSearchStatus.PopTuple(rnd);
            var newTuple = getRandomAttr(attrValueEntropyDic[attrName], rnd);
            curSearchStatus.AddTuple(attrName, newTuple.Key, attrEn, newTuple.Value);

            attrValueEntropyDic[attrName].Remove(newTuple.Key);
            attrValueEntropyDic[attrName].Add(attrValue, valEn);
        }

        private static void doRandomSwapTuple(SearchStatus curSearchStatus, Dictionary<string, double> attrEntropyDic, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Random rnd)
        {
            (string attrName, string attrValue, double attrEn, double valEn) = curSearchStatus.PopTuple(rnd);
            var newAttrTuple = getRandomAttr(attrEntropyDic, rnd);
            attrEntropyDic.Remove(newAttrTuple.Key);
            var newValueTuple = getRandomAttr(attrValueEntropyDic[newAttrTuple.Key], rnd);
            curSearchStatus.AddTuple(newAttrTuple.Key, newValueTuple.Key, newAttrTuple.Value, newValueTuple.Value);
            attrEntropyDic.Add(attrName, attrEn);

            attrValueEntropyDic[attrName].Add(attrValue, valEn);
            attrValueEntropyDic[newAttrTuple.Key].Remove(newValueTuple.Key);
        }

        private static void doRandomAddTuple(SearchStatus curSearchStatus, Dictionary<string, double> attrEntropyDic, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Random rnd)
        {
            var newAttrTuple = getRandomAttr(attrEntropyDic, rnd);
            attrEntropyDic.Remove(newAttrTuple.Key);

            var newValueTuple = getRandomAttr(attrValueEntropyDic[newAttrTuple.Key], rnd);
            curSearchStatus.AddTuple(newAttrTuple.Key, newValueTuple.Key, newAttrTuple.Value, newValueTuple.Value);

            attrValueEntropyDic[newAttrTuple.Key].Remove(newValueTuple.Key);
        }

        private static void doRandomDelTuple(SearchStatus curSearchStatus, Dictionary<string, double> attrEntropyDic, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Random rnd)
        {
            (string attrName, string attrValue, double attrEn, double valEn) = curSearchStatus.PopTuple(rnd);
            attrEntropyDic.Add(attrName, attrEn);
            attrValueEntropyDic[attrName].Add(attrValue, valEn);
        }

        private static KeyValuePair<string, double> getRandomAttr(Dictionary<string, double> valEnDic, Random rnd)
        {
            int elementIndex = rnd.Next(valEnDic.Count);
            return valEnDic.ElementAt(elementIndex);
        }

        public static bool doRandomSearch(int nextOpt, SearchStatus curSearchStatus, Dictionary<string, double> attrEntropyDic, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Random rnd)
        {
            switch (nextOpt)
            {
                case 0:
                    //Console.WriteLine("Swap Value...");
                    doRandomSwapValue(curSearchStatus, attrValueEntropyDic, rnd);
                    break;
                case 1:
                    //Console.WriteLine("Swap Tuple...");
                    doRandomSwapTuple(curSearchStatus, attrEntropyDic, attrValueEntropyDic, rnd);
                    break;
                case 2:
                    //Console.WriteLine("Add Tuple...");
                    doRandomAddTuple(curSearchStatus, attrEntropyDic, attrValueEntropyDic, rnd);
                    break;
                case 3:
                    //Console.WriteLine("Del Tuple...");
                    doRandomDelTuple(curSearchStatus, attrEntropyDic, attrValueEntropyDic, rnd);
                    break;
            }
            return true;
            //curSearchStatus.PrintCurrentStatus();
        }
    }

    class GreedyOpts
    {
        private static bool doGreedyAddTuple(SearchStatus curSearchStatus, Dictionary<string, double> attrEntropyDic, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Dictionary<string, int> tabuDic, int curSearchStep, int tabuSize, int bmsSize, Random rnd)
        {
            var attrEnList = BMS(attrEntropyDic, bmsSize, rnd);
            attrEnList.Sort((x, y) => y.Value.CompareTo(x.Value));
            int bms_count = 0;
            for (int i = 0; i < attrEnList.Count; i++)
            {
                if (bms_count > bmsSize)
                    break;
                var attrTuple = attrEnList[i];
                string attrName = attrTuple.Key;
                List<KeyValuePair<string, double>> valueEnList = attrValueEntropyDic[attrName].ToList();
                valueEnList.Sort((x, y) => y.Value.CompareTo(x.Value));
                string newValue = DataOpts.ChooseAttrValue(attrName, valueEnList, tabuDic, curSearchStep, tabuSize);
                if (newValue.Length > 0)
                {
                    curSearchStatus.AddTuple(attrName, newValue, attrTuple.Value, attrValueEntropyDic[attrName][newValue]);
                    attrEntropyDic.Remove(attrName);
                    attrValueEntropyDic[attrName].Remove(newValue);
                    return true;
                }
                bms_count += valueEnList.Count;
            }
            return false;
        }

        private static bool doGreedySwapValue(SearchStatus curSearchStatus, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Dictionary<string, int> tabuDic, int curSearchStep, int tabuSize, int bmsSize, Random rnd)
        {
            (string attrName, string attrValue, double attrEn, double valEn) = curSearchStatus.PopMinEnTuple();

            var valueEnList = BMS(attrValueEntropyDic[attrName], bmsSize, rnd);

            valueEnList.Sort((x, y) => y.Value.CompareTo(x.Value));
            
            string newValue = DataOpts.ChooseAttrValue(attrName, valueEnList, tabuDic, curSearchStep, tabuSize);
            if (newValue.Length > 0)
            {
                curSearchStatus.AddTuple(attrName, newValue, attrEn, attrValueEntropyDic[attrName][newValue]);
                attrValueEntropyDic[attrName].Remove(newValue);
                attrValueEntropyDic[attrName].Add(attrValue, valEn);
                return true;
            }
            else
            {
                curSearchStatus.AddTuple(attrName, attrValue, attrEn, valEn);
                return false;
            }
        }

        private static List<KeyValuePair<string, double>> BMS(Dictionary<string, double> totalValueDic, int bmsSize, Random rnd)
        {
            if (totalValueDic.Count < bmsSize)
            {
                return totalValueDic.ToList();
            }
            else
            {
                List<KeyValuePair<string, double>> bmsList = new List<KeyValuePair<string, double>>();
                var totalValueList = totalValueDic.ToList();
                for (int i = 0; i < bmsSize; i++)
                {
                    var choosedHead = totalValueList.ElementAt(rnd.Next(totalValueList.Count));
                    bmsList.Add(choosedHead);
                    totalValueList.Remove(choosedHead);
                }
                return bmsList;
            }
        }

        private static bool doGreedySwapTuple(SearchStatus curSearchStatus, Dictionary<string, double> attrEntropyDic, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Dictionary<string, int> tabuDic, int curSearchStep, int tabuSize, int bmsSize, Random rnd)
        {
            (string attrName, string attrValue, double attrEn, double valEn) = curSearchStatus.PopMinEnTuple();

            var addTuple = doGreedyAddTuple(curSearchStatus, attrEntropyDic, attrValueEntropyDic, tabuDic, curSearchStep, tabuSize, bmsSize, rnd);
            if (!addTuple)
                curSearchStatus.AddTuple(attrName, attrValue, attrEn, valEn);
            else
            {
                attrEntropyDic.Add(attrName, attrEn);
                attrValueEntropyDic[attrName].Add(attrValue, valEn);
            }
            return addTuple;
        }

        private static bool doGreedyDelTuple(SearchStatus curSearchStatus, Dictionary<string, double> attrEntropyDic, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic)
        {
            (string attrName, string attrValue, double attrEn, double valEn) = curSearchStatus.PopMinEnTuple();
            attrEntropyDic.Add(attrName, attrEn);
            attrValueEntropyDic[attrName].Add(attrValue, valEn);
            return true;
        }

        public static bool doGreedySearch(int nextOpt, SearchStatus curSearchStatus, Dictionary<string, double> attrEntropyDic, Dictionary<string, Dictionary<string, double>> attrValueEntropyDic, Dictionary<string, int> tabuDic, int curSearchStep, int tabuSize, int bmsSize, Random rnd)
        {
            bool doSearch = false;
            switch (nextOpt)
            {
                case 0:
                    //Console.WriteLine("Swap Value...");
                    doSearch = doGreedySwapValue(curSearchStatus, attrValueEntropyDic, tabuDic, curSearchStep, tabuSize, bmsSize, rnd);
                    break;
                case 1:
                    //Console.WriteLine("Swap Tuple...");
                    doSearch = doGreedySwapTuple(curSearchStatus, attrEntropyDic, attrValueEntropyDic, tabuDic, curSearchStep, tabuSize, bmsSize, rnd);
                    break;
                case 2:
                    //Console.WriteLine("Add Tuple...");
                    doSearch = doGreedyAddTuple(curSearchStatus, attrEntropyDic, attrValueEntropyDic, tabuDic, curSearchStep, tabuSize, bmsSize, rnd);
                    break;
                case 3:
                    //Console.WriteLine("Del Tuple...");
                    doSearch = doGreedyDelTuple(curSearchStatus, attrEntropyDic, attrValueEntropyDic);
                    break;
            }
            return doSearch;
            //curSearchStatus.PrintCurrentStatus();
        }
    }
}
