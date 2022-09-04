using System;
using System.Linq;
using System.Text;

using href.Utils;

namespace FindAndReplace
{

	public class EncodingDetector
	{

		[Flags]
		public enum Options
		{
			KlerkSoftBom = 1,
			KlerkSoftHeuristics = 2,
			MLang = 4
		}

		public static Encoding Detect(byte[] bytes, Options opts = Options.KlerkSoftBom | Options.MLang, Encoding defaultEncoding = null)
		{
			Encoding encoding = null;

			if ((opts & Options.KlerkSoftBom) == Options.KlerkSoftBom)
			{
				StopWatch.Start("DetectEncoding: UsingKlerksSoftBom");

				encoding = DetectEncodingUsingKlerksSoftBom(bytes);

				StopWatch.Stop("DetectEncoding: UsingKlerksSoftBom");
			}

			if (encoding != null)
				return encoding;

			if ((opts & Options.KlerkSoftHeuristics) == Options.KlerkSoftHeuristics)
			{
				StopWatch.Start("DetectEncoding: UsingKlerksSoftHeuristics");
				encoding = DetectEncodingUsingKlerksSoftHeuristics(bytes);
				StopWatch.Stop("DetectEncoding: UsingKlerksSoftHeuristics");
			}

			if (encoding != null)
				return encoding;

			if ((opts & Options.MLang) == Options.MLang)
			{
				StopWatch.Start("DetectEncoding: UsingMLang");
				encoding = DetectEncodingUsingMLang(bytes);
				StopWatch.Stop("DetectEncoding: UsingMLang");
			}

            if (encoding == null)
				encoding = defaultEncoding;

			return encoding;
		}

		private static Encoding DetectEncodingUsingKlerksSoftBom(byte[] bytes)
		{
			Encoding encoding = null;
			if (bytes.Count() >= 4)
				 encoding = TextFileEncodingDetector.DetectBOMBytes(bytes);

			return encoding;
		}


		private static Encoding DetectEncodingUsingKlerksSoftHeuristics(byte[] bytes)
		{
			var encoding = TextFileEncodingDetector.DetectUnicodeInByteSampleByHeuristics(bytes);

			return encoding;
		}

		private static Encoding DetectEncodingUsingMLang(byte[] bytes)
		{
			try
			{
				var detected = EncodingTools.DetectInputCodepages(bytes, 1);
				if (detected.Length > 0)
				{
					return detected[0];
				}
			}
			catch
			{
                /*
                 * If the following NotSupportedException occurs:
                 * "No data is available for encoding 1252. For information on defining a custom encoding,
                 * see the documentation for the Encoding.RegisterProvider method."
                 * Register it like so: Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
                 * The CTOR of EncodingTools contains this registration currently.
                 */
			}

			return null;
		}
	}
}
