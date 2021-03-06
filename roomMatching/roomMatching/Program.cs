﻿using BeanboxSoftware.BeanMap;
using LibSVMsharp;
using LibSVMsharp.Extensions;
using LibSVMsharp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConsoleApplication1
{
    class Program
    {
        //static void Main(string[] args)
        //{
        //    var segment=new PanGuSegment();
        //    var rooms1 = File.ReadAllText("../../room1.txt");
        //    var rooms2 = File.ReadAllText("../../room2.txt");

        //    var words1 = segment.ChineseSegemnt(rooms1);
        //    var words2 = segment.ChineseSegemnt(rooms2);

        //    foreach (var word in words1)
        //    {
        //        Console.WriteLine("{0}",word);
        //    }
        //    Console.ReadLine();
        //}
        static void Main(string[] args)
        {
            Console.WindowWidth = 188;
            Console.WindowHeight = 33;

            //HMM.HMMtest3.run();
            //HMM.HMMtest22.Viterbi();
           // HMM.HMMtest22.BaumWelchLearning();
            //wordsSvm();
            Console.ReadLine();

            
            var index = 11;
            var rooms11 = File.ReadAllText(string.Format("../../room{0}.txt", index));
            var rooms22 = File.ReadAllText(string.Format("../../room{0}.txt", ++index));


            var termFreqWeight = new  PanGuTermFreqWeight();

            var rooms1=rooms11.Split(';');
            var rooms2 = rooms22.Split(';');
            termFreqWeight.computerFW(rooms1, rooms2);
            var vv = termFreqWeight.computerSimilar();
            //foreach (var v in vv)
            //{
            //    if (v.Value > 0)
            //    {
            //        Console.WriteLine("{0},{1},{2}", v.Key.Item1, v.Key.Item2, v.Value);
            //    }
            //}

            var vv2 = vv.GroupBy(t => t.Key);
            foreach (var v2 in vv2)
            {
                var v22=v2.SelectMany(t=>t.Value).OrderByDescending(t=>t.Value);
                foreach (var item in v22)
                {
                    if (item.Value > 0)
                    {
                        Console.WriteLine("{0},{1},{2}", v2.Key, item.Key, item.Value);
                    }
                }
                Console.WriteLine("".PadLeft(40,'-'));
            }
            Console.ReadLine();
        }
        public static SVMPrintFunction mySVMPrintFunction = new SVMPrintFunction(Output);
        private static void Output(string log) 
        {
            Console.WriteLine(log);
        }
        static void wordsSvm()
        {//http://www.svm-tutorial.com/2014/10/svm-tutorial-classify-text-csharp/

            var index =11;
            var rooms11 = File.ReadAllText(string.Format("../../room{0}.txt", index));
            var rooms22 = File.ReadAllText(string.Format("../../room{0}.txt",++index));

            var rooms1 = rooms11.Split(';');
            var rooms2 = rooms22.Split(';');

            var termFreqWeight = new PanGuTermFreqWeight();
            termFreqWeight.computerFW(rooms1, rooms2);

            var problem = new SVMProblem(); //SVM.SetPrintStringFunction(mySVMPrintFunction);

            var lableFeaturesBuilder = new LableFeaturesBuilder();
            var segment = new PanGuSegment();
            foreach (var room in rooms2)
            {
                var words = termFreqWeight.TermsByDoc[1, room];
                words = words.Where(t => t.Length >1 && termFreqWeight.TermFWByGlobal.ContainsKey(t) && termFreqWeight.TermFWByType.ContainsKey(0, t)).ToArray();
                //lableFeaturesBuilder.AddToProblem(problem, room, words.Select(t => new KeyValuePair<string, double>(t,termFreqWeight.TermFWByDoc[1, room, t].Freq*GetW(t))));
                lableFeaturesBuilder.AddToProblem(problem, room, words.Select(t => new KeyValuePair<string, double>(t,Math.Max(termFreqWeight.TermFWByType[0, t].Freq,termFreqWeight.TermFWByType[1, t].Freq))));
            }

            SVMParameter parameter=new SVMParameter();
            parameter.Type=SVMType.C_SVC;
            parameter.Kernel=SVMKernelType.LINEAR;
            parameter.C=1;
            parameter.Probability = true;
            //parameter.
            //parameter.WeightLabels = lableFeaturesBuilder.CreateWeightFeatures("大", "双", "套", "大床", "双床").ToArray();
            //parameter.Weights = new double[] { 1.90, 1.90, 1.90, 1.99, 1.99 };

            //parameter.WeightLabels = lableFeaturesBuilder.CreateWeightLables<int>("行政湖景双床房").ToArray();
            //parameter.Weights = new double[] { 1.90, 1.90, 1.90, 1.99, 1.99 };

            problem = problem.Normalize(SVMNormType.L1);
            problem.CheckParameter(parameter);

            var model2 = LibSVMsharp.SVM.Train(problem, parameter);
             model2.SaveModel("roomMatching.model");

             var model = LibSVMsharp.SVM.LoadModel("roomMatching.model");
            foreach (var room in rooms1)
            {
                var words = termFreqWeight.TermsByDoc[0,room];
                words = words.Where(t => t.Length > 1 && termFreqWeight.TermFWByGlobal.ContainsKey(t) && termFreqWeight.TermFWByType.ContainsKey(1, t)).ToArray();

                //var nodes = lableFeaturesBuilder.CreateNodes(words.Select(t => new KeyValuePair<string, double>(t, termFreqWeight.TermFWByDoc[0, room, t].Freq * GetW(t))));
                var nodes = lableFeaturesBuilder.CreateNodes(words.Select(t => new KeyValuePair<string, double>(t, Math.Max(termFreqWeight.TermFWByType[0, t].Freq,termFreqWeight.TermFWByType[1, t].Freq))));
                if (nodes.Length > 0)
                {
                    nodes = nodes.Normalize(SVMNormType.L1);

                    double predictedY = 0;
                    predictedY = LibSVMsharp.SVM.Predict(model, nodes);

                    double[] values = null; double probabilityValue = 0;
                    probabilityValue = LibSVMsharp.SVM.PredictValues(model, nodes, out values);

                    double[] est = null; double probability = 0;
                    probability = LibSVMsharp.SVM.PredictProbability(model, nodes, out est);

                    Console.WriteLine("{0,22}\t{1},{2},{3},{4},{5}", room, lableFeaturesBuilder.GetLable(predictedY), predictedY, probabilityValue, probability,string.Empty);
                }
            }

            Console.WriteLine(new string('=', 80));
        }

        protected static double GetW(string key)
        {
            double weight = 1.0;
            //switch (key)
            //{
            //    case "标准": weight = 2; break;
            //    case "单人": weight = 1.4; break;
            //    case "双人": weight = 1.4; break;
            //    //case "无窗": weight =0.1; break;
            //}
            return weight;
        }
    }
    }

