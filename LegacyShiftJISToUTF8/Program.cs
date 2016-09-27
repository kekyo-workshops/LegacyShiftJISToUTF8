/////////////////////////////////////////////////////////////////////////////////////////////////
//
// LegacyShiftJISToUTF8
// Copyright (c) 2016 Kouji Matsui (@kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
/////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace LegacyShiftJISToUTF8
{
    public static class Program
    {
        private static bool IsKana(char ch)
        {
            return (ch >= 0xff01) && (ch <= 0xff9f);
        }

        private static string ConvertString(string origin)
        {
            var sb = new StringBuilder();
            var index = 0;
            while (index < origin.Length)
            {
                var ch = origin[index];
                if (IsKana(ch))
                {
                    var startIndex = index;
                    index++;
                    while (index < origin.Length)
                    {
                        ch = origin[index];
                        if (IsKana(ch) == false)
                        {
                            break;
                        }
                        index++;
                    }

                    var target = origin.Substring(startIndex, index - startIndex);
                    var widen = Strings.StrConv(target, VbStrConv.Wide, 0);
                    sb.Append(widen);
                }
                else
                {
                    sb.Append(ch);
                    index++;
                }
            }

            return sb.ToString();
        }

        private static async Task ConvertFileAsync(
            string inputPath, Encoding inputEncoding,
            string outputPath, Encoding outputEncoding)
        {
            using (var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    var tr = new StreamReader(inputStream, inputEncoding, true);
                    var tw = new StreamWriter(outputStream, outputEncoding);

                    while (true)
                    {
                        var line = await tr.ReadLineAsync();
                        if (line == null)
                        {
                            break;
                        }

                        var normalized = ConvertString(line);
                        await tw.WriteLineAsync(normalized);
                    }

                    await outputStream.FlushAsync();
                    outputStream.Close();
                }
            }
        }

        private static async Task<int> MainAsync(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine(
                    string.Format("usage: {0} <input file> <output file>",
                    Path.GetFileName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath)));
                return 1;
            }

            var inputPath = Path.GetFullPath(args[0]);
            var outputPath = Path.GetFullPath(args[1]);

            var inputEncoding = Encoding.GetEncoding("Shift_JIS");
            var outputEncoding = Encoding.UTF8;

            await ConvertFileAsync(inputPath, inputEncoding, outputPath, outputEncoding);

            return 0;
        }

        public static int Main(string[] args)
        {
            try
            {
                return MainAsync(args).Result;
            }
            catch (Exception ex)
            {
                var e = ex.InnerException ?? ex;
                Console.Error.WriteLine(e.ToString());
                return Marshal.GetHRForException(e);
            }
        }
    }
}
