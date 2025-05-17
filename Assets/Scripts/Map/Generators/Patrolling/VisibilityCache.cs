// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;

using Maes.Utilities;

using Unity.Collections;
using Unity.Jobs;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling
{
    public static class VisibilityCache
    {
        private static readonly Dictionary<(Bitmap, float), string> CachedVisibilities = new();

        // Cache index to avoid overwriting the same file
        private static int _cacheIndex = 0;

        internal static Dictionary<Vector2Int, Bitmap> ComputeVisibilityCached(Bitmap map, float maxDistance = 0f)
        {
            Dictionary<Vector2Int, Bitmap> visibilities;
            if (CachedVisibilities.TryGetValue((map, maxDistance), out var filepath))
            {
                // Load the cached visibility from the json file
                return LoadFromCache(filepath);
            }

            visibilities = ComputeVisibility(map, maxDistance);
            // Save the visibility to a json file
            SaveToCache(map, maxDistance, visibilities);
            return visibilities;
        }

        private static void SaveToCache(Bitmap map, float maxDistance, Dictionary<Vector2Int, Bitmap> visibilities)
        {
            if (_cacheIndex == 0)
            {
                // If the cache index is 0, we need to delete the old cache files
                ClearCacheDirectory();
            }

            var json = JsonUtility.ToJson(visibilities);
            var filename = $"visibility-{map.Width}X{map.Height}-maxDist{maxDistance}-index{_cacheIndex++}.json";
            var path = System.IO.Path.Combine(GlobalSettings.MapCacheLocation, filename);

            System.IO.Directory.CreateDirectory(GlobalSettings.MapCacheLocation);
            System.IO.File.WriteAllText(path, json);
            CachedVisibilities[(map, maxDistance)] = path;
        }

        private static void ClearCacheDirectory()
        {
            if (!System.IO.Directory.Exists(GlobalSettings.MapCacheLocation))
            {
                return;
            }

            foreach (var file in System.IO.Directory.GetFiles(GlobalSettings.MapCacheLocation))
            {
                System.IO.File.Delete(file);
            }
        }

        private static Dictionary<Vector2Int, Bitmap> LoadFromCache(string filepath)
        {
            if (!System.IO.File.Exists(filepath))
            {
                throw new System.IO.FileNotFoundException($"Cached visibility-file not found: {filepath}");
            }

            Dictionary<Vector2Int, Bitmap> precomputedVisibilities;
            var json = System.IO.File.ReadAllText(filepath);
            precomputedVisibilities = JsonUtility.FromJson<Dictionary<Vector2Int, Bitmap>>(json);
            return precomputedVisibilities;
        }

        internal static Dictionary<Vector2Int, Bitmap> ComputeVisibility(Bitmap map, float maxDistance = 0f)
        {
            var startTime = Time.realtimeSinceStartup;

            var nativeMap = map.ToNativeArray();
            var nativeVisibilities = new NativeArray<ulong>[map.Width];

            for (var i = 0; i < nativeVisibilities.Length; i++)
            {
                nativeVisibilities[i] = new NativeArray<ulong>(nativeMap.Length * map.Height, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            }

            var jobs = new JobHandle[map.Width];

            // Outermost loop parallelized to improve performance
            for (var x = 0; x < map.Width; x++)
            {
                var job = new VisibilityJob()
                {
                    Width = map.Width,
                    Height = map.Height,
                    X = x,
                    Map = nativeMap,
                    Visibility = nativeVisibilities[x],
                    MaxDistance = maxDistance,
                };

                jobs[x] = job.Schedule();
            }

            foreach (var job in jobs)
            {
                job.Complete();
            }

            var precomputedVisibilities = new Dictionary<Vector2Int, Bitmap>();

            for (var i = 0; i < nativeVisibilities.Length; i++)
            {
                var bitmaps = Bitmap.FromNativeArray(map.Width, map.Height, nativeMap.Length, nativeVisibilities[i]);
                for (var y = 0; y < map.Height; y++)
                {
                    var bitmap = bitmaps[y];
                    if (bitmap.Any)
                    {
                        var tile = new Vector2Int(i, y);
                        precomputedVisibilities[tile] = bitmap;
                    }
                    else
                    {
                        bitmap.Dispose();
                    }
                }

                nativeVisibilities[i].Dispose();
            }

            nativeMap.Dispose();

            // To debug the ComputeVisibility method, use the following utility method to save as image
            // SaveAsImage.SaveVisibileTiles();

            Debug.LogFormat("Compute visibility took {0} seconds", Time.realtimeSinceStartup - startTime);

            return precomputedVisibilities;
        }
    }
}