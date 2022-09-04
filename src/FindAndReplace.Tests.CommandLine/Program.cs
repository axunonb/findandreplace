using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using FindAndReplace;

namespace FindAndReplace.Tests.CommandLine
{
	class Program
	{
		static void Main(string[] args)
		{
			var options = new CommandLineOptions();

			string result;
			if (Parser.Default.ParseArguments(args, new []{typeof(CommandLineOptions)}).Errors.Any())
			{
				if (options.SkipDecoding)
					result = options.TestValue;
				else
					result = CommandLineUtils.DecodeText(options.TestValue, false, options.HasRegEx, options.UseEscapeChars);
			}
			else
			{
				result = "Errors in ParseArguments: " + Environment.NewLine;
				if (options.ParserResult.Errors.Count() > 0)
				{
					foreach (var error in options.ParserResult.Errors)
						result += error.Tag + error.ToString();
				}	
			}

			using (var outfile = new StreamWriter("output.log"))
			{
				outfile.Write(result);
			} 
		}
	}
}
