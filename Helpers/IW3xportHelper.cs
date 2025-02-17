﻿namespace MapPortingUtility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using System.Windows.Forms;
    using System.Diagnostics;

    public class IW3xportHelper : ExportHelper
    {
        private const string EXECUTABLE_NAME = "iw3xport.exe";

        public static Map[] GetMapList(ref Paths paths)
        {
            List<Map> maps = new List<Map>();
            string basePath = paths.IW3Path;

            for (int i = 0; i < CANONICAL_LANGUAGES.Length; i++)
            {
                var lang = CANONICAL_LANGUAGES[i];
                try
                {
                    var directory = Path.Combine(basePath, string.Format(CANONICAL_MAP_DIRECTORY_FMT, lang));

                    if (Directory.Exists(directory))
                    {
                        var files = Directory.GetFiles(directory);

                        foreach (var file in files)
                        {
                            if (!file.EndsWith(MAP_EXT_FILTER))
                                continue;
                            if (!Path.GetFileName(file).StartsWith(MAP_PREFIX_FILTER))
                                continue;
                            if (file.EndsWith(MAP_SUFFIX_FILTER + MAP_EXT_FILTER))
                                continue;

                            maps.Add(new Map()
                            {
                                Path = Path.Combine(file),
                                Category = Map.ECategory.CANONICAL
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not list canonical maps for iw3 for language {lang}!\n{ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (Directory.Exists(Path.Combine(basePath, USERMAPS_MAP_DIRECTORY))) {
                try {
                    var directories = Directory.GetDirectories(Path.Combine(basePath, USERMAPS_MAP_DIRECTORY));

                    foreach (var directory in directories) {
                        var files = Directory.GetFiles(directory);
                        foreach (var file in files) {
                            if (!file.EndsWith(MAP_EXT_FILTER)) continue;
                            if (!Path.GetFileName(file).StartsWith(MAP_PREFIX_FILTER)) continue;
                            if (file.EndsWith(MAP_SUFFIX_FILTER + MAP_EXT_FILTER)) continue;

                            maps.Add(new Map() {
                                Path = Path.Combine(file),
                                Category = Map.ECategory.USERMAPS
                            });
                        }
                    }
                }
                catch (Exception ex) {
                    MessageBox.Show($"Could not list user maps for iw3!\n{ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            return maps.ToArray();
        }

        public static int DumpMap(Map map, ref Paths paths, bool correctSpeculars, bool convertGSCs, uint correctSmodelsMethod, Action<string> pipe)
        {
            string exePath = Path.Combine(paths.IW3Path, EXECUTABLE_NAME);

            if (!File.Exists(exePath)) {
                pipe.Invoke($"Missing {exePath}!");
                return -1;
            }

            var proc = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = exePath,
                    WorkingDirectory = paths.IW3Path,
                    Arguments = $"-stdout +set iw3x_correct_speculars {(correctSpeculars ? 1 : 0)} +set iw3x_convert_gsc {(correctSpeculars ? 1 : 0)} +set iw3x_smodels_fix_method {correctSmodelsMethod} +set export_path {GetDumpDestinationPath(ref map, ref paths)} +dumpmap {map.Name} +quit",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            pipe($"{exePath} {proc.StartInfo.Arguments}");
            proc.OutputDataReceived += (sender, args) =>
            {
                pipe(args.Data);
            };
            proc.ErrorDataReceived += (sender, args) =>
            {
                pipe(args.Data);
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            return proc.ExitCode;
        }
    }
}
