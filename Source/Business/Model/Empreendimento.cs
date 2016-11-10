using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business.Model {
    public class Empreendimento : INotifyPropertyChanged {

        private int ordem;
        private string nome;

        public int Ordem {
            get { return ordem; }
            set { SetField(ref ordem, value); }
        }

        public String Nome {
            get { return nome; }
            set { SetField(ref nome, value); }
        }

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
    }
}
