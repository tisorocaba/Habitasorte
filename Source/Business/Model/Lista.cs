using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Habitasorte.Business.Model {
    public class Lista : IDataErrorInfo {

        public Sorteio Sorteio { get; set; }

        public int IdLista { get; set; }
        public int OrdemSorteio { get; set; }
        public string Nome { get; set; }
        public bool Sorteada { get; set; }
        public bool Publicada { get; set; }

        private int quantidade;
        public int Quantidade {
            get { return quantidade; }
            set {
                quantidade = value;
                QuantidadeString = quantidade.ToString();
            }
        }

        private string quantidadeString;
        public string QuantidadeString
        {
            get { return quantidadeString; }
            set
            {
                quantidadeString = value;
                if (!String.IsNullOrWhiteSpace(QuantidadeString) && Regex.IsMatch(QuantidadeString, @"^\d+$")) {
                    quantidade = int.Parse(value);
                    if (Sorteio != null) {
                        Sorteio.NotifyPropertyChanged("TotalVagasTitulares");
                        Sorteio.NotifyPropertyChanged("TotalVagasReserva");
                        Sorteio.NotifyPropertyChanged("TotalVagas");
                    }
                }
            }
        }

        public int? CandidatosDisponiveis { get; set; }

        /* Exibição */

        public string VagasText => $"{Quantidade} vagas.";
        public string CandidatosText => $"{CandidatosDisponiveis} candidatos.";
        public string NomeFormatado => String.Format("{0:00} - {1}", OrdemSorteio, Nome);

        /* IDataErrorInfo */

        #region IDataErrorInfo Members

        string IDataErrorInfo.Error { get { return null; } }

        string IDataErrorInfo.this[string columnName] { get {
            if (columnName == "QuantidadeString") {
                if (String.IsNullOrWhiteSpace(QuantidadeString) || !Regex.IsMatch(QuantidadeString, @"^\d+$")) {
                    return "Quantidade inválida.";
                }
            }
            return null;
        }}

        public bool IsValid { get {
            IDataErrorInfo errorInfo = this as IDataErrorInfo;
            return errorInfo["QuantidadeString"] == null;
        }}

        #endregion
    }
}
