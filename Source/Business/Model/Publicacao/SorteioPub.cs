using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business.Model.Publicacao {
    public class SorteioPub {
        public int? Codigo { get; set; }
        public string Nome { get; set; }
        public virtual ICollection<ListaPub> Listas { get; set; }
    }
}
