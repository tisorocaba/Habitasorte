using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business.Model {
    public class CandidatoGrupo {
        public int? Classificacao { get; set; }
        public int Sequencia { get; set; }
        public decimal Cpf { get; set; }
        public int IdCandidato { get; set; }
        public string Nome { get; set; }

        public int? QuantidadeCriterios { get; set; }
        public int? QuantidadeCriteriosComposta { get {
            if (QuantidadeCriterios == 5) return 6;
            else if (QuantidadeCriterios == 3) return 4;
            else if (QuantidadeCriterios == 1) return 2;
            else if (QuantidadeCriterios == 0) return 2;
            else return QuantidadeCriterios;
        }}
    }
}
