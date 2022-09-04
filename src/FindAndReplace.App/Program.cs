using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FindAndReplace.App
{
    internal static class Program_
    {


        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main_()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new MainForm());
        }
    }


	//from http://www.rootsilver.com/2007/08/how-to-create-a-consolewindow
	internal static class Program
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		[DllImport("kernel32", SetLastError = true)]
		private static extern bool AttachConsole(int dwProcessId);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		private static int Main(string[] args)
        {
            //args = new[] { "--cl", "--help" };
			// from http://blogs.msdn.com/b/microsoft_press/archive/2010/02/03/jeffrey-richter-excerpt-2-from-clr-via-c-third-edition.aspx
			AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;

			if (args.Length != 0 && args.Contains("--cl"))
			{ 
				Process process;

				if (args.Contains("--consoleByProcessId"))
				{
					//Get a parent process for see where send the output.
					process = Process.GetProcessById(Environment.ProcessId).Parent();
				}
				else
				{
					// Get a pointer to the foreground window. The idea here is that
					// If the user is starting our application from an existing console
					// shell, that shell will be the uppermost window. We'll get it
					// and attach to it.
					var ptr = GetForegroundWindow();

					var processId = GetWindowThreadProcessId(ptr, out var u);

					process = Process.GetProcessById(u);
				}

				if (process.ProcessName == "cmd") //Is the uppermost window a cmd process?
                {
                    AttachConsole(process.Id);
                }
                else
                {
                    //no console AND we're in console mode ... create a new console.

                    AllocConsole();
                }

                var clRunner = new CommandLineRunner();
				var dosErrorLevel = clRunner.Run(args);

				FreeConsole();
				return dosErrorLevel;
			}
			else
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new MainForm());

				return 0;
			}
		}

		private static Assembly? ResolveEventHandler(object? sender, ResolveEventArgs args)
		{
			var dllName = new AssemblyName(args.Name).Name + ".dll";

			var assembly = Assembly.GetExecutingAssembly();

			var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(rn => rn.EndsWith(dllName));

			if (resourceName == null) return null; // Not found, maybe another handler will find it

            using var stream = assembly.GetManifestResourceStream(resourceName);
			if (stream == null) return null;

            var assemblyData = new byte[stream.Length];

            _ = stream.Read(assemblyData, 0, assemblyData.Length);

            return Assembly.Load(assemblyData);
        }
	}

	public class CommandLineRunner
	{
		public enum DosErrorLevel
		{
			Success = 0,
			FatalError = 1,
			ErrorsInSomeFiles = 2
		};


		private CommandLineOptions? _options;

		public int Run(string[] args)
		{
			DosErrorLevel dosErrorLevel;

			_options = new CommandLineOptions();
			if (!Parser.Default.ParseArguments(args, new []{typeof(CommandLineOptions)}).Errors.Any())
			{
				Console.ReadKey();
				Environment.Exit(1);
			}

			var validationResultList = new List<ValidationResult>();

			validationResultList.Add(ValidationUtils.IsDirValid(_options.Dir, "dir"));
			validationResultList.Add(ValidationUtils.IsNotEmpty(_options.FileMask, "fileMask"));
			validationResultList.Add(ValidationUtils.IsNotEmpty(_options.FindText, "find"));
			validationResultList.Add(ValidationUtils.IsNotEmpty(_options.FindText, "find"));

			if (_options.IsFindTextHasRegEx)
				validationResultList.Add(ValidationUtils.IsValidRegExp(_options.FindText, "find"));

			if (!(string.IsNullOrEmpty(_options.AlwaysUseEncoding)))
				validationResultList.Add(ValidationUtils.IsValidEncoding(_options.AlwaysUseEncoding, "alwaysUseEncoding"));

			if (!(string.IsNullOrEmpty(_options.DefaultEncodingIfNotDetected)))
				validationResultList.Add(ValidationUtils.IsValidEncoding(_options.DefaultEncodingIfNotDetected, "alwaysUseEncoding"));

			if (_options.UseEscapeChars)
			{
				validationResultList.Add(ValidationUtils.IsValidEscapeSequence(_options.FindText, "find"));
				
				if (!string.IsNullOrEmpty(_options.ReplaceText))
					validationResultList.Add(ValidationUtils.IsValidEscapeSequence(_options.ReplaceText, "replace"));
			}


			if (!string.IsNullOrEmpty(_options.LogFile))
			{
				var fs1 = new FileStream(_options.LogFile, FileMode.Append);
				var sw1 = new StreamWriter(fs1);
				Console.SetOut(sw1);
			}

			Console.WriteLine("");

			if (validationResultList.Any(vr => !vr.IsSuccess))
			{
				foreach (var validationResult in validationResultList)
				{
					if (!validationResult.IsSuccess)
						Console.WriteLine(string.Format("{0}: {1}", validationResult.FieldName, validationResult.ErrorMessage));
				}

				Console.WriteLine("");

				dosErrorLevel = DosErrorLevel.FatalError;
			}
			else
			{
				dosErrorLevel = DosErrorLevel.Success;

				var hasRegEx = _options.IsFindTextHasRegEx;

				if (_options.ReplaceText == null)
				{
					var finder = new Finder
                    {
                        Dir = _options.Dir,
                        IncludeSubDirectories = _options.IncludeSubDirectories,
                        FileMask = _options.FileMask,
                        ExcludeFileMask = _options.ExcludeFileMask,
                        ExcludeDir = _options.ExcludeDir,
                        FindText = CommandLineUtils.DecodeText(_options.FindText, false, hasRegEx, _options.UseEscapeChars),
                        IsCaseSensitive = _options.IsCaseSensitive,
                        FindTextHasRegEx = hasRegEx,
                        SkipBinaryFileDetection = _options.SkipBinaryFileDetection,
                        IncludeFilesWithoutMatches = _options.IncludeFilesWithoutMatches,
                        AlwaysUseEncoding = Utils.GetEncodingByName(_options.AlwaysUseEncoding),
                        DefaultEncodingIfNotDetected = Utils.GetEncodingByName(_options.DefaultEncodingIfNotDetected),
                        UseEscapeChars = _options.UseEscapeChars,
                        IsSilent = _options.Silent
                    };

                    finder.FileProcessed += OnFinderFileProcessed;

					var findResult = finder.Find();

					if (_options.SetErrorLevelIfAnyFileErrors)
						if (findResult.Stats.Files.FailedToRead > 0)
							dosErrorLevel = DosErrorLevel.ErrorsInSomeFiles;
				}
				else
				{
					var replacer = new Replacer
                    {
                        Dir = _options.Dir,
                        IncludeSubDirectories = _options.IncludeSubDirectories,
                        FileMask = _options.FileMask,
                        ExcludeFileMask = _options.ExcludeFileMask,
                        ExcludeDir = _options.ExcludeDir,
                        FindText = CommandLineUtils.DecodeText(_options.FindText, false, hasRegEx, _options.UseEscapeChars),
                        IsCaseSensitive = _options.IsCaseSensitive,
                        FindTextHasRegEx = _options.IsFindTextHasRegEx,
                        SkipBinaryFileDetection = _options.SkipBinaryFileDetection,
                        IncludeFilesWithoutMatches = _options.IncludeFilesWithoutMatches,
                        ReplaceText = CommandLineUtils.DecodeText(_options.ReplaceText, true, _options.IsFindTextHasRegEx, _options.UseEscapeChars),
                        AlwaysUseEncoding = Utils.GetEncodingByName(_options.AlwaysUseEncoding),
                        DefaultEncodingIfNotDetected = Utils.GetEncodingByName(_options.DefaultEncodingIfNotDetected),
                        UseEscapeChars = _options.UseEscapeChars,
                        IsSilent = _options.Silent,
                        IsKeepModifiedDate = _options.IsKeepModifiedDate
                    };

                    replacer.FileProcessed += OnReplacerFileProcessed;

                    var replaceResult = replacer.Replace();

					if (_options.SetErrorLevelIfAnyFileErrors)
						if (replaceResult.Stats.Files.FailedToRead > 0 || replaceResult.Stats.Files.FailedToWrite > 0)
							dosErrorLevel = DosErrorLevel.ErrorsInSomeFiles;
				}
			}

			if (!string.IsNullOrEmpty(_options.LogFile))
			{
				Console.Out.Close();
			}

#if (DEBUG)
				Console.ReadLine();
#endif

			return (int) dosErrorLevel;
		}

		private void OnFinderFileProcessed(object sender, FinderEventArgs e)
		{
			if (e.ResultItem.IncludeInResultsList && !e.IsSilent)
				PrintFinderResultRow(e.ResultItem, e.Stats);

			if (e.Stats.Files.Processed == e.Stats.Files.Total && !e.IsSilent)
				PrintStatistics(e.Stats);

		}

		private void OnReplacerFileProcessed(object sender, ReplacerEventArgs e)
		{
			if (e.ResultItem.IncludeInResultsList && !e.IsSilent)
				PrintReplacerResultRow(e.ResultItem, e.Stats);

			if (e.Stats.Files.Processed == e.Stats.Files.Total && !e.IsSilent)
				PrintStatistics(e.Stats, true);
		}


		public void PrintFinderResultRow(Finder.FindResultItem item, Stats stats)
		{
			PrintFileAndEncoding(item);

			if (item.IsSuccess)
				PrintNameValuePair("Matches", item.NumMatches.ToString());
			else
				PrintNameValuePair("Error", item.ErrorMessage);

			Console.WriteLine();
		}

		private void PrintFileAndEncoding(ResultItem item)
		{
			PrintNameValuePair("File", item.FileRelativePath);

			if (_options is { ShowEncoding: true } && item.FileEncoding != null)
				PrintNameValuePair("Encoding", item.FileEncoding.WebName);
		}

		public void PrintReplacerResultRow(Replacer.ReplaceResultItem item, Stats stats)
		{
			PrintFileAndEncoding(item);

			if (!item.FailedToOpen)
				PrintNameValuePair("Matches", item.NumMatches.ToString());

			PrintNameValuePair("Replaced", item.IsReplaced ? "Yes" : "No");

			if (!item.IsSuccess)
				PrintNameValuePair("Error", item.ErrorMessage);

			Console.WriteLine();
		}

		private static void PrintNameValuePair(string name, string value)
		{
			var label = name + ":";
			label = label.PadRight(10);
			Console.WriteLine(label + value);
		}

		public void PrintStatistics(Stats stats, bool isReplacerStats = false)
		{
			Console.WriteLine("");

			Console.WriteLine("====================================");
			Console.WriteLine("Stats");
			Console.WriteLine("");
			Console.WriteLine("Files:");
			Console.WriteLine("- Total: " + stats.Files.Total);
			Console.WriteLine("- Binary: " + stats.Files.Binary + " (skipped)");
			Console.WriteLine("- With Matches: " + stats.Files.WithMatches);
			Console.WriteLine("- Without Matches: " + stats.Files.WithoutMatches);
			Console.WriteLine("- Failed to Open: " + stats.Files.FailedToRead);

			if (isReplacerStats)
				Console.WriteLine("- Failed to Write: " + stats.Files.FailedToWrite);

			Console.WriteLine("");
			Console.WriteLine("Matches:");
			Console.WriteLine("- Found: " + stats.Matches.Found);

			if (isReplacerStats)
				Console.WriteLine("- Replaced: " + stats.Matches.Replaced);

			Console.WriteLine("");
			var secs = Math.Round(stats.Time.Passed.TotalSeconds, 3);
			Console.WriteLine("Duration: " + secs.ToString() + " secs");

			Console.WriteLine("====================================");
		}

	}
}



