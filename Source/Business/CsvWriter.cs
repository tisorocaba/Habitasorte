using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business {
    public static class CsvWriter {

        private const string QUOTE = "\"";
        private const string ESCAPED_QUOTE = "\"\"";
        private static char[] MUST_QUOTE_CHARACTERS = { ',', '"', '\n' };

        public static string Escape(string value) {

            if (value.Contains(QUOTE)) {
                value = value.Replace(QUOTE, ESCAPED_QUOTE);
            }

            if (value.IndexOfAny(MUST_QUOTE_CHARACTERS) > -1) {
                value = QUOTE + value + QUOTE;
            }

            return value;
        }

        public static void WriteRow(StreamWriter writer, string[] values) {
            writer.WriteLine(string.Join(";", values.Select(Escape)));
            writer.Flush();
        }
    }
}
