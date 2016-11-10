using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business.Model {
    public class Sorteio : IDataErrorInfo, INotifyPropertyChanged {

        private string nome;
        private string statusSorteio;
        private ICollection<Empreendimento> empreendimentos;
        private ICollection<Lista> listas;
        private Lista proximaLista;
        private ConfiguracaoPublicacao configuracaoPublicacao;

        public Sorteio() {
            listas = new List<Lista>();
        }

        public string Nome {
            get { return nome; }
            set { SetField(ref nome, value); }
        }

        public string StatusSorteio {
            get { return statusSorteio; }
            set { SetField(ref statusSorteio, value); }
        }

        public ICollection<Empreendimento> Empreendimentos {
            get { return empreendimentos; }
            set { SetField(ref empreendimentos, value); }
        }

        public void AdicionarEmpreendimento(string nome) {
            empreendimentos.Add(new Empreendimento {
                Ordem = empreendimentos.Count() == 0 ? 1 : empreendimentos.Max(e => e.Ordem) + 1,
                Nome = nome
            });
        }

        public void RemoverEmpreendimento(Empreendimento empreendimento) {
            empreendimentos.Remove(empreendimento);
            int ordem = 1;
            foreach (Empreendimento e in empreendimentos) {
                e.Ordem = ordem;
                ordem++;
            }
        }

        public string ErroEmpreendimentos { get { 
            if (empreendimentos.Count() == 0) {
                return "Pelo menos um empreendimento deve ser informado.";
            } else if(empreendimentos.Any(e => string.IsNullOrWhiteSpace(e.Nome))) {
                return "O nome de todos os empreendimentos devem ser informados.";
            } else {
                return null;
            }
        }}

        public ICollection<Lista> Listas {
            get { return listas; }
            set {
                value.ToList().ForEach(l => l.Sorteio = this);
                SetField(ref listas, value);
                NotifyPropertyChanged("TotalVagasTitulares");
                NotifyPropertyChanged("TotalVagasReserva");
                NotifyPropertyChanged("TotalVagas");
            }
        }

         public Lista ProximaLista {
            get { return proximaLista; }
            set {
                SetField(ref proximaLista, value);
            }
        }

        public int? TotalVagasTitulares => listas.Where(l => !l.Nome.EndsWith("(Reserva)")).Sum(l => l.Quantidade);
        public int? TotalVagasReserva => listas.Where(l => l.Nome.EndsWith("(Reserva)")).Sum(l => l.Quantidade);
        public int? TotalVagas => listas.Sum(l => l.Quantidade);

        public ConfiguracaoPublicacao ConfiguracaoPublicacao {
            get { return configuracaoPublicacao; }
            set { SetField(ref configuracaoPublicacao, value); }
        }

        /* INotifyPropertyChanged */

        #region INotifyPropertyChanged

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) {
                return false;
            }
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        public void NotifyPropertyChanged(string property) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /* IDataErrorInfo */

        #region IDataErrorInfo Members

        string IDataErrorInfo.Error { get { return null; } }

        string IDataErrorInfo.this[string columnName] { get {
            if (columnName == "Nome" && string.IsNullOrWhiteSpace(Nome)) return "Nome inválido!";
            return null;
        }}

        public bool IsValid { get {
            IDataErrorInfo errorInfo = this as IDataErrorInfo;
            return errorInfo["Nome"] == null
                && ErroEmpreendimentos == null;
        }}

        #endregion
    }
}
