using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business.Model.Publicacao {
    public class ListaPub {
        public int? IdLista { get; set; }
        public string Nome { get; set; }
        public string FonteSementeSorteio { get; set; }
        public int? SementeSorteio { get; set; }
        public virtual ICollection<CandidatoPub> Candidatos { get; set; }
    }
}
