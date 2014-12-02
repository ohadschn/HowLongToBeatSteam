using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Common.Entities;
using SteamHltbScraper.Logging;

namespace SteamHltbScraper.Imputation
{
    //*Note* Only R's 'bin', 'etc', and 'library' folders are needed. 
    //       The following packages must be present in the library folder:
    //----------------------------------------------------------------------------------------------------------------//
    //base,boot,class,cluster,codetools,colorspace,compiler,datasets,DEoptimR,digest,foreign,GGally,ggplot2,graphics
    //grDevices,grid,gtable,KernSmooth,lattice,MASS,Matrix,methods,mgcv,munsell,mvtnorm,nlme,nnet,parallel,pcaPP,pls
    //plyr,proto,Rcpp,reshape,reshape2,robCompositions,robustbase,rpart,rrcov,scales,spatial,splines,stats,stats4
    //stringr,survival,tcltk,tools,utils

    public static class Imputer
    {
        internal const string ImputedCsvFileName = "imputed.csv";

        internal static void Impute(IReadOnlyList<AppEntity> allApps, IReadOnlyList<AppEntity> updates)
        {
            HltbScraperEventSource.Log.ImputeStart();

            MarkImputed(updates);
            ZeroPreviouslyImputed(allApps.Except(updates));

            var notMissing = allApps.Where(a => !a.MainTtbImputed || !a.ExtrasTtbImputed || !a.CompletionistTtbImputed).ToArray();
            ImputeCore(notMissing);
            FillMissing(allApps, notMissing);

            HltbScraperEventSource.Log.ImputeStop();
        }

        private static void ZeroPreviouslyImputed(IEnumerable<AppEntity> previous)
        {
            foreach (var app in previous)
            {
                app.MainTtb = app.MainTtbImputed ? 0 : app.MainTtb;
                app.ExtrasTtb = app.ExtrasTtbImputed ? 0 : app.MainTtb;
                app.CompletionistTtb = app.CompletionistTtbImputed ? 0 : app.MainTtb;
            }
        }

        private static void FillMissing(IReadOnlyList<AppEntity> allApps, IReadOnlyList<AppEntity> notMissing)
        {
            int mainSum = 0;
            int extrasSum = 0;
            int completionistSum = 0;
            foreach (var app in notMissing)
            {
                mainSum += app.MainTtb;
                extrasSum += app.ExtrasTtb;
                completionistSum += app.CompletionistTtb;
            }

            int mainAvg = mainSum/notMissing.Count;
            int extrasAvg = extrasSum / notMissing.Count;
            int completionistAvg = completionistSum / notMissing.Count;
            foreach (var app in allApps.Except(notMissing)) //not not missing = missing
            {
                app.MainTtb = mainAvg;
                app.MainTtbImputed = true;
                app.ExtrasTtb = extrasAvg;
                app.ExtrasTtbImputed = true;
                app.CompletionistTtb = completionistAvg;
                app.CompletionistTtbImputed = true;
            }
        }

        private static void ImputeCore(IReadOnlyList<AppEntity> allApps)
        {
            HltbScraperEventSource.Log.CalculateImputationStart();

            var dataPath = GetDataPath();

            var csvString = string.Join(Environment.NewLine,
                allApps.Select(a => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}",
                    Observation(a.MainTtb), Observation(a.ExtrasTtb), Observation(a.CompletionistTtb))));

            File.WriteAllText(Path.Combine(dataPath, "ttb.csv"), csvString);

            HltbScraperEventSource.Log.InvokeRStart();
            using (var proc = Process.Start(@"R\bin\i386\Rscript.exe", @"Imputation\Impute.R"))
            {
                Trace.Assert(proc != null, "Cannot execute RScript.exe");
                proc.WaitForExit();
            }
            HltbScraperEventSource.Log.InvokeRStop();

            //skip header row and discard blank lines
            var imputed = File.ReadLines(Path.Combine(dataPath, ImputedCsvFileName)).Skip(1).Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            Trace.Assert(allApps.Count == imputed.Length,
                String.Format(CultureInfo.InvariantCulture, "imputation count mismatch: expected {0}, actual {1}", allApps.Count, imputed.Length));
            
            for (int i = 0; i < allApps.Count; i++)
            {
                UpdateFromCsvRow(allApps[i], imputed[i]);
                FixImputationMiss(allApps[i]);
            }

            HltbScraperEventSource.Log.CalculateImputationStop();
        }

        internal static string GetDataPath()
        {
            var dataPath = Environment.GetEnvironmentVariable("WEBJOBS_DATA_PATH");
            Trace.Assert(dataPath != null, "WEBJOBS_DATA_PATH environment variable undefined!");
            return dataPath;
        }

        private static void MarkImputed(IEnumerable<AppEntity> updates)
        {
            foreach (var app in updates)
            {
                app.MainTtbImputed = (app.MainTtb == 0);
                app.ExtrasTtbImputed = (app.ExtrasTtb == 0);
                app.CompletionistTtbImputed = (app.CompletionistTtb == 0);
            }
        }

        internal static void UpdateFromCsvRow(AppEntity appEntity, string row)
        {
            var ttbs = row.Split(',');
            Trace.Assert(ttbs.Length == 3);

            appEntity.MainTtb = GetRoundedValue(ttbs[0]);
            appEntity.ExtrasTtb = Math.Max(appEntity.MainTtb, GetRoundedValue(ttbs[1]));
            appEntity.CompletionistTtb = Math.Max(appEntity.ExtrasTtb, GetRoundedValue(ttbs[2]));
        }

        private static void FixImputationMiss(AppEntity appEntity)
        {
            if (appEntity.MainTtb > appEntity.ExtrasTtb)
            {
                if (appEntity.MainTtbImputed)
                {
                    appEntity.MainTtb = appEntity.ExtrasTtb;
                }
                else //only extras imputed
                {
                    appEntity.ExtrasTtb = appEntity.MainTtb;
                }
            }

            if (appEntity.ExtrasTtb > appEntity.CompletionistTtb)
            {
                if (appEntity.CompletionistTtbImputed)
                {
                    appEntity.CompletionistTtb = appEntity.ExtrasTtb;
                }
                else //only extras imputed
                {
                    appEntity.ExtrasTtb = appEntity.CompletionistTtb;
                }
            }
        }

        private static int GetRoundedValue(string value)
        {
            return (int)Math.Round(Double.Parse(value, CultureInfo.InvariantCulture));
        }

        private static string Observation(int value)
        {
            return value == 0 ? "NA" : value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
