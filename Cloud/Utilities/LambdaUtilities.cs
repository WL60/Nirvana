﻿using System;
using System.IO;
using Genome;
using IO;

namespace Cloud.Utilities
{
    public static class LambdaUtilities
    {
        public const string SuccessMessage = "Success";
        public const string SnsTopicKey    = "SnsTopicArn";

        public static void GarbageCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static string GetEnvironmentVariable(string key)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(value)) throw new InvalidDataException($"Environment variable {key} is not set.");
            return value;
        }

        public static void DeleteTempOutput()
        {
            string[] files = Directory.GetFiles(Path.GetTempPath());
            if (files.Length == 0) return;
            foreach (string tempFile in files) File.Delete(tempFile);
        }

        public static string GetManifestUrl(string version, GenomeAssembly genomeAssembly, string baseUrl = null)
        {
            if (string.IsNullOrEmpty(version)) version = "latest";
            string s3BaseUrl = LambdaUrlHelper.GetBaseUrl(baseUrl);
            switch (version)
            {
                case "latest":
                    return $"{s3BaseUrl}latest_SA_{genomeAssembly}.txt";
                case "release":
                    return $"{s3BaseUrl}DRAGEN_3.4_{genomeAssembly}.txt";
                default:
                    return $"{s3BaseUrl}{version}_SA_{genomeAssembly}.txt";
            }
        }

        public static void ValidateSupplementaryData(GenomeAssembly genomeAssembly, string lambdaSaVersion, string baseUrl = null)
        {
            //lambdaSaVersion == "latest" or "release", i.e. anything mentioned in the context as the sa version
            var manifestUrl = GetManifestUrl(lambdaSaVersion, genomeAssembly, baseUrl);
            HttpUtilities.ValidateUrl(manifestUrl,false);

            Console.WriteLine("Validating supplementary data files");
            using (var reader = new StreamReader(PersistentStreamUtils.GetReadStream(manifestUrl)))
            {
                string line;
                string s3BaseUrl = LambdaUrlHelper.GetBaseUrl(baseUrl);
                while ((line = reader.ReadLine()) != null)
                {
                    HttpUtilities.ValidateUrl(s3BaseUrl + line, false);
                }
            }

            Console.WriteLine("done");
        }

        public static void ValidateCoreData(GenomeAssembly genomeAssembly, string baseUrl=null)
        {
            HttpUtilities.ValidateUrl(LambdaUrlHelper.GetRefUrl(genomeAssembly, baseUrl), false);

            string cachePathPrefix = GetCachePathPrefix(genomeAssembly, baseUrl);
            HttpUtilities.ValidateUrl(CacheConstants.TranscriptPath(cachePathPrefix), false);
            HttpUtilities.ValidateUrl(CacheConstants.SiftPath(cachePathPrefix), false);
            HttpUtilities.ValidateUrl(CacheConstants.PolyPhenPath(cachePathPrefix), false);

        }

        public static string GetCachePathPrefix(GenomeAssembly genomeAssembly, string baseUrl=null)
        {
            return LambdaUrlHelper.GetCacheFolder(baseUrl).UrlCombine(genomeAssembly.ToString())
                .UrlCombine(LambdaUrlHelper.DefaultCacheSource);
        }
    }
}
