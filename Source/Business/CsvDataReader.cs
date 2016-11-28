using CsvHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business {
    public class CsvDataReader : AbstractDataReader {

        private string[] fields = new string[] {
            "CPF",
            "NOME",
            "QUANTIDADE_CRITERIOS",
            "LISTA_DEFICIENTES",
            "LISTA_IDOSOS",
            "LISTA_INDICADOS"
        };

        private CsvReader csvReader;

        public CsvDataReader(StreamReader streamReader) {
            csvReader = new CsvReader(streamReader);
            csvReader.Configuration.Delimiter = ";";
            csvReader.Configuration.HasHeaderRecord = false;
        }

        public override void Dispose() {
            csvReader.Dispose();
        }

        public override int FieldCount { get {
            return fields.Length;
        }}

        public override string GetName(int i) {
            return fields[i];
        }

        public override bool Read() {
            return csvReader.Read();
        }

        public override object this[int i] { get {
            switch (i) {
                case 0: return csvReader.GetField<decimal>(i);
                case 1: return csvReader.GetField<string>(i);
                case 2: return csvReader.GetField<int>(i);
                case 3: return csvReader.GetField<bool>(i);
                case 4: return csvReader.GetField<bool>(i);
                case 5: return csvReader.GetField<bool>(i);
                default: throw new Exception($"Campo {i} do CSV não mapeado.");
            }
        }}
    }
}
