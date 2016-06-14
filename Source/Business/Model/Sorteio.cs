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
        private string empreendimento1;
        private bool empreendimento2Ativo;
        private string empreendimento2;
        private bool empreendimento3Ativo;
        private string empreendimento3;
        private bool empreendimento4Ativo;
        private string empreendimento4;
        private ICollection<Lista> listas;
        private Lista proximaLista;

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

        public string Empreendimento1 {
            get { return empreendimento1; }
            set { SetField(ref empreendimento1, value); }
        }

        public bool Empreendimento2Ativo {
            get { return empreendimento2Ativo; }
            set {
                SetField(ref empreendimento2Ativo, value);
                if (!value) {
                    empreendimento2 = null;
                    Empreendimento3Ativo = false;
                }
                NotifyPropertyChanged("Empreendimento2");
            }
        }

        public string Empreendimento2 {
            get { return empreendimento2; }
            set { SetField(ref empreendimento2, value); }
        }

        public bool Empreendimento3Ativo {
            get { return empreendimento3Ativo; }
            set {
                SetField(ref empreendimento3Ativo, value);
                if (!value) {
                    empreendimento3 = null;
                    Empreendimento4Ativo = false;
                }
                NotifyPropertyChanged("Empreendimento3");
            }
        }

        public string Empreendimento3 {
            get { return empreendimento3; }
            set { SetField(ref empreendimento3, value); }
        }

        public bool Empreendimento4Ativo {
            get { return empreendimento4Ativo; }
            set {
                SetField(ref empreendimento4Ativo, value);
                if (!value) {
                    empreendimento4 = null;
                }
                NotifyPropertyChanged("Empreendimento4");
            }
        }

        public string Empreendimento4 {
            get { return empreendimento4; }
            set { SetField(ref empreendimento4, value); }
        }

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
            if (columnName == "Empreendimento1" && string.IsNullOrWhiteSpace(Empreendimento1)) return "Empreendimento 1 inválido!";
            if (columnName == "Empreendimento2" && Empreendimento2Ativo && string.IsNullOrWhiteSpace(Empreendimento2)) return "Empreendimento 2 inválido!";
            if (columnName == "Empreendimento3" && Empreendimento3Ativo && string.IsNullOrWhiteSpace(Empreendimento3)) return "Empreendimento 3 inválido!";
            if (columnName == "Empreendimento4" && Empreendimento4Ativo && string.IsNullOrWhiteSpace(Empreendimento4)) return "Empreendimento 4 inválido!";
            return null;
        }}

        public bool IsValid { get {
            IDataErrorInfo errorInfo = this as IDataErrorInfo;
            return errorInfo["Nome"] == null
                && errorInfo["Empreendimento1"] == null
                && errorInfo["Empreendimento2"] == null
                && errorInfo["Empreendimento3"] == null
                && errorInfo["Empreendimento4"] == null;
        }}

        #endregion
    }
}
