using Habitasorte.Business;
using Habitasorte.Business.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Habitasorte {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private bool activated = false;
        private bool processing = false;

        private SorteioService Service { get; set; }
        private Sorteio Sorteio => Service.Model;
        private string StatusSorteio => Sorteio.StatusSorteio;

        public MainWindow() {

            InitializeComponent();

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Title += $" v{versionInfo.FileVersion}";

            Service = new SorteioService();
            Service.SorteioChanged += (s) => { DataContext = s; };
            Service.CarregarSorteio();

            EtapaConfiguracao(false);
            EtapaCadastro(false);
            EtapaImportacao(false);
            EtapaQuantidades(false);
            EtapaSorteio(false);
            EtapaFinalizado(false);
        }

        private void Window_Activated(object sender, EventArgs e) {
            if (!activated) {
                activated = true;
                switch (StatusSorteio) {
                    case Status.CADASTRO:
                        EtapaCadastro(true);
                        break;
                    case Status.IMPORTACAO:
                        EtapaImportacao(true);
                        break;
                    case Status.QUANTIDADES:
                        EtapaQuantidades(true);
                        break;
                    case Status.SORTEIO:
                    case Status.SORTEIO_INICIADO:
                        EtapaSorteio(true);
                        break;
                    case Status.FINALIZADO:
                        EtapaFinalizado(true);
                        break;
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            e.Cancel = processing;
        }

        private bool VerificarStatus(params string[] statuses) {
            return statuses.Contains(StatusSorteio);
        }

        private void AlternarTab(TabItem tab, bool ativo) {
            if (ativo) {
                tab.Visibility = Visibility.Visible;
                tab.Focus();
                lblEtapaSorteio.Content = tab.Header;
            }
            (tab.Content as Grid).IsEnabled = ativo;
            tab.Visibility = Visibility.Collapsed;
        }

        private void ShowMessage(string message) {
            MessageBox.Show(
                message,
                "AVISO",
                MessageBoxButton.OK,
                MessageBoxImage.Asterisk
            );
        }

        private void ShowErrorMessage(string message) {
            MessageBox.Show(
                message,
                "ERRO",
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation
            );
        }

        /* Ativação das etapas do sorteio. */

        private void EtapaConfiguracao(bool ativo) {
            AlternarTab(tabConfiguracao, ativo);
            txtCodigoPublicacao.Password = Sorteio.ConfiguracaoPublicacao.CodigoPublicacao;
            txtSenhaPublicacao.Password = Sorteio.ConfiguracaoPublicacao.SenhaPublicacao;
        }

        private void EtapaCadastro(bool ativo) {
            Service.CarregarSorteio();
            btnAvancarCadastro.IsEnabled = !VerificarStatus(Status.CADASTRO);
            grdFormCadastro.IsEnabled = VerificarStatus(Status.CADASTRO, Status.IMPORTACAO);
            AlternarTab(tabCadastro, ativo);
        }

        private void EtapaImportacao(bool ativo) {
            btnRecuarImportacao.IsEnabled = true;
            btnAvancarImportacao.IsEnabled = !VerificarStatus(Status.CADASTRO, Status.IMPORTACAO);
            grdArquivoImportacao.IsEnabled = VerificarStatus(Status.IMPORTACAO);
            grdConfiguracaoImportacao.IsEnabled = true;
            grdImportacaoEmAndamento.IsEnabled = false;
            if (!VerificarStatus(Status.IMPORTACAO)) {
                btnImportarArquivo.IsEnabled = false;
                if (ativo) {
                    lblStatusImportacao.Content = $"{Service.ContagemCandidatos()} candidatos importados.";
                }
            }
            AlternarTab(tabImportacao, ativo);
        }

        private void EtapaQuantidades(bool ativo) {
            Service.CarregarListas();
            lstQuantidades.IsEnabled = VerificarStatus(Status.QUANTIDADES, Status.SORTEIO);
            btnAtualizarQuantidades.IsEnabled = VerificarStatus(Status.QUANTIDADES, Status.SORTEIO);
            btnAvancarQuantidades.IsEnabled = !VerificarStatus(Status.CADASTRO, Status.IMPORTACAO, Status.QUANTIDADES);
            AlternarTab(tabQuantidades, ativo);
        }

        private void EtapaSorteio(bool ativo) {
            Service.CarregarListas();
            Service.CarregarProximaLista();
            txtSementePersonalizada.Text = "";
            btnRecuarSorteio.IsEnabled = true;
            btnAvancarSorteio.IsEnabled = VerificarStatus(Status.FINALIZADO);
            grdIniciarSorteio.IsEnabled = VerificarStatus(Status.SORTEIO, Status.SORTEIO_INICIADO);
            grdSorteioEmAndamento.IsEnabled = false;
            btnPublicarTodasListas.IsEnabled = Sorteio.Listas.Any(s => s.Sorteada);
            lstSorteioListasSorteio.IsEnabled = true;
            lblSorteioListaAtual.Visibility = Visibility.Hidden;
            lblSorteioProximaLista.Visibility = Visibility.Visible;
            AlternarTab(tabSorteio, ativo);
        }

        private void EtapaFinalizado(bool ativo) {
            btnRecuarFinalizado.IsEnabled = true;
            btnExportarListas.IsEnabled = true;
            btnAbrirDiretorioExportacao.IsEnabled = ativo && Service.DiretorioExportacaoCSVExistente;
            AlternarTab(tabFinalizado, ativo);
        }

        /* Transição entre as etapas .*/

        private void btnAvancarConfiguracao_Click(object sender, RoutedEventArgs e) {
            EtapaConfiguracao(false);
            EtapaCadastro(true);
        }

        private void btnRecuarCadastro_Click(object sender, RoutedEventArgs e) {
            EtapaCadastro(false);
            EtapaConfiguracao(true);
        }

        private void buttonAvancarCadastro_Click(object sender, RoutedEventArgs e) {
            EtapaCadastro(false);
            EtapaImportacao(true);
        }

        private void buttonRecuarImportacao_Click(object sender, RoutedEventArgs e) {
            EtapaImportacao(false);
            EtapaCadastro(true);
        }

        private void buttonAvancarImportacao_Click(object sender, RoutedEventArgs e) {
            EtapaImportacao(false);
            EtapaQuantidades(true);
        }

        private void buttonRecuarQuantidades_Click(object sender, RoutedEventArgs e) {
            EtapaQuantidades(false);
            EtapaImportacao(true);
        }

        private void buttonAvancarQuantidades_Click(object sender, RoutedEventArgs e) {
            EtapaQuantidades(false);
            EtapaSorteio(true);
        }

        private void buttonRecuarSorteio_Click(object sender, RoutedEventArgs e) {
            EtapaSorteio(false);
            EtapaQuantidades(true);
        }

        private void buttonAvancarSorteio_Click(object sender, RoutedEventArgs e) {
            EtapaSorteio(false);
            EtapaFinalizado(true);
        }

        private void buttonRecuarFinalizado_Click(object sender, RoutedEventArgs e) {
            EtapaFinalizado(false);
            EtapaSorteio(true);
        }

        /* Etapa de Configuração */

        private void btnExcluirDados_Click(object sender, RoutedEventArgs e) {

            MessageBoxResult result = MessageBox.Show(
                $"Excluir dados e reiniciar aplicação?",
                "Excluir dados?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes) {

                MessageBoxResult confirmResult = MessageBox.Show(
                    $"Tem certeza? A exclusão dos dados é definitiva!",
                    "Excluir dados?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (confirmResult == MessageBoxResult.Yes) {
                    Service.ExcluirBancoReiniciarAplicacao();
                }
            }
        }

        private void btnAtualizarDadosPublicacao_Click(object sender, RoutedEventArgs e) {
            Sorteio.ConfiguracaoPublicacao.CodigoPublicacao = txtCodigoPublicacao.Password;
            Sorteio.ConfiguracaoPublicacao.SenhaPublicacao = txtSenhaPublicacao.Password;
            Service.AtualizarConfiguracaoPublicacao();
            EtapaConfiguracao(true);
            try {
                string message = Service.PublicarLista(null, true);
                ShowMessage($"Sucesso no teste de publicação:\n\n {message}");
            } catch (Exception exception) {
                ShowErrorMessage($"Falha no teste de publicação:\n\n {exception.Message}");
            }
        }

        /* Etapa de Cadastro. */

        private void buttonAtualizarCadastro_Click(object sender, RoutedEventArgs e) {
            if (Sorteio.ErroEmpreendimentos != null) {
                ShowErrorMessage(Sorteio.ErroEmpreendimentos);
            }
            if (Sorteio.IsValid) {
                Service.AtualizarSorteio();
                EtapaCadastro(true);
                ShowMessage("Sorteio Alterado!");
            }
        }

        private void btnAdicionarEmpreendimento_Click(object sender, RoutedEventArgs e) {
            Sorteio.AdicionarEmpreendimento("NOVO EMPREENDIMENTO");
        }

        private void btnRemoverEmpreendimento_Click(object sender, RoutedEventArgs e) {
            Empreendimento empreendimento = ((sender as Button).Parent as Grid).DataContext as Empreendimento;
            Sorteio.RemoverEmpreendimento(empreendimento);                        
        }

        /* Etapa de Importação. */

        private string GetDragEventFile(DragEventArgs e) {

            string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop, false);

            bool validFile = files != null
                && files.Count() == 1
                && (
                    files.First().ToLower().EndsWith(".xls")
                    || files.First().ToLower().EndsWith(".xlsx")
                    || files.First().ToLower().EndsWith(".csv")
                );

            return (validFile) ? files.First() : null;
        }

        private void AtribuirArquivoImportacao(string file) {
            lblNomeArquivo.Content = file.Split(new char[] { '\\', '/' }).Last();
            lblCaminhoArquivo.Content = file;
            imgArquivoSelecionado.Visibility = Visibility.Visible;
            imgSemArquivo.Visibility = Visibility.Hidden;
            btnImportarArquivo.IsEnabled = true;
        }

        private void LimparArquivoImportacao() {
            lblNomeArquivo.Content = "";
            lblCaminhoArquivo.Content = "";
            imgArquivoSelecionado.Visibility = Visibility.Hidden;
            imgSemArquivo.Visibility = Visibility.Visible;
            btnImportarArquivo.IsEnabled = false;
        }
        
        private void gridImportacao_DragEnter(object sender, DragEventArgs e) {
            if (GetDragEventFile(e) != null) {
                imgCerto.Visibility = Visibility.Visible;
            } else {
                imgErrado.Visibility = Visibility.Visible;
            }
        }

        private void gridImportacao_DragLeave(object sender, DragEventArgs e) {
            imgErrado.Visibility = Visibility.Hidden;
            imgCerto.Visibility = Visibility.Hidden;
        }

        private void gridArquivoImportacao_Drop(object sender, DragEventArgs e) {
            imgErrado.Visibility = Visibility.Hidden;
            imgCerto.Visibility = Visibility.Hidden;
            string file = GetDragEventFile(e);
            if (GetDragEventFile(e) != null) {
                AtribuirArquivoImportacao(file);
            } else {
                LimparArquivoImportacao();
            }
        }

        private void gridArquivoImportacao_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".xlsx";
            fileDialog.Filter = "Planilha Excel (*.xlsx)|*.xlsx|Planilha Excel 97-2003 (*.xls)|*.xls";

            bool? result = fileDialog.ShowDialog();
            if (result == true) {
                AtribuirArquivoImportacao(fileDialog.FileName);
            }
        }

        private void buttonImportarArquivo_Click(object sender, RoutedEventArgs e) {

            btnRecuarImportacao.IsEnabled = false;
            btnAvancarImportacao.IsEnabled = false;
            grdConfiguracaoImportacao.IsEnabled = false;
            grdImportacaoEmAndamento.IsEnabled = true;

            string caminhoArquivo = lblCaminhoArquivo.Content as string;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (wSender, wE) => {

                processing = true;

                Action<string> updateStatus = (value) => Dispatcher.Invoke(() => { lblStatusImportacao.Content = value; });
                Action<int> updateProgress = (value) => Dispatcher.Invoke(() => { pgrImportacao.Value = value; });

                try {
                    Service.CriarListasSorteio(caminhoArquivo, updateStatus, updateProgress);
                    updateStatus("Importação finalizada.");
                } catch (Exception exception) {
                    ShowErrorMessage($"Erro na importação: {exception.Message}");
                    updateProgress(0);
                    updateStatus("Erro na importação.");
                }

                Dispatcher.Invoke(() => EtapaImportacao(true));
                processing = false;
            };
            worker.RunWorkerAsync();
        }

        /* Etapa de Quantidades. */

        private void buttonAtualizarQuantidades_Click(object sender, RoutedEventArgs e) {
            if (Sorteio.Listas.All(l => l.IsValid)) {
                Service.AtualizarListas();
                ShowMessage("Quantidades das listas atualizadas!");
                EtapaQuantidades(true);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            (sender as TextBox).SelectAll();
        }

        /* Etapa de Sorteio. */

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            txtSementePersonalizada.IsEnabled = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            txtSementePersonalizada.Clear();
            txtSementePersonalizada.IsEnabled = false;
        }

        private void BloquearEtapaSorteio() {

            btnRecuarSorteio.IsEnabled = false;
            btnAvancarSorteio.IsEnabled = false;
            grdIniciarSorteio.IsEnabled = false;
            grdSorteioEmAndamento.IsEnabled = true;
            btnPublicarTodasListas.IsEnabled = false;
            lstSorteioListasSorteio.IsEnabled = false;

            lblSorteioListaAtual.Visibility = Visibility.Visible;
            lblSorteioProximaLista.Visibility = Visibility.Hidden;

            txtLogSorteio.Clear();
        }

        private void buttonSortearProximaLista_Click(object sender, RoutedEventArgs e) {

            int? sementePersonalizada = null;
            if (chkSementePersonalizada.IsChecked == true) {
                int valorSemente;
                if (!int.TryParse(txtSementePersonalizada.Text.Trim(), out valorSemente)) {
                    ShowErrorMessage("O valor de semente informado é inválido.");
                    return;
                } else {
                    sementePersonalizada = valorSemente;
                }
            }

            BloquearEtapaSorteio();

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (wSender, wE) => {

                processing = true;

                Action<string> updateStatus = (value) => Dispatcher.Invoke(() => { lblStatusSorteio.Content = value; });
                Action<int> updateProgress = (value) => Dispatcher.Invoke(() => { pgrSorteio.Value = value; });
                Action<string> logText = (value) => Dispatcher.Invoke(() => {
                    if (!string.IsNullOrWhiteSpace(txtLogSorteio.Text)) {
                        txtLogSorteio.AppendText(Environment.NewLine);
                    }
                    txtLogSorteio.AppendText(value);
                    txtLogSorteio.ScrollToEnd();
                });

                Service.SortearProximaLista(updateStatus, updateProgress, logText, sementePersonalizada);
                Dispatcher.Invoke(() => EtapaSorteio(true));

                processing = false;
            };
            worker.RunWorkerAsync();
        }

        private void btnPublicarLista_Click(object sender, RoutedEventArgs e) {

            Lista lista = ((sender as Button).Parent as Grid).DataContext as Lista;

            MessageBoxResult result = MessageBox.Show(
                $"Publicar a lista \"{lista.Nome}\"?",
                "Publicar lista?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes) {
                PublicarListas(lista);
            }
        }

        private void btnSalvarLista_Click(object sender, RoutedEventArgs e) {

            Lista lista = ((sender as Button).Parent as Grid).DataContext as Lista;

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.FileName = lista.Nome;
            saveDialog.DefaultExt = ".pdf";
            saveDialog.Filter = "PDF files (.pdf)|*.pdf";

            if (saveDialog.ShowDialog() == true) {
                Service.SalvarLista(lista, saveDialog.FileName);
            }
        }

        /* Etapa de Sorteio Finalizado. */

        private void Button_Click(object sender, RoutedEventArgs e) {

            btnRecuarFinalizado.IsEnabled = false;
            btnExportarListas.IsEnabled = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (wSender, wE) => {

                processing = true;

                Action<string> updateStatus = (value) => Dispatcher.Invoke(() => { lblStatusExportacao.Content = value; });

                try {
                    Service.ExportarListas(updateStatus);
                    updateStatus("Exportação finalizada.");
                } catch (Exception exception) {
                    ShowErrorMessage($"Erro na exportação: {exception.Message}");
                    updateStatus("Erro na exportação.");
                }

                Dispatcher.Invoke(() => EtapaFinalizado(true));
                processing = false;
            };
            worker.RunWorkerAsync();
        }

        /* Publicação */

        private void btnPublicarTodasListas_Click(object sender, RoutedEventArgs e) {

            MessageBoxResult result = MessageBox.Show(
                $"Publicar todas as listas?",
                "Publicar listas?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes) {
                PublicarListas(Sorteio.Listas.Where(l => l.Sorteada == true).ToArray());
            }
        }

        private void PublicarListas(params Lista[] listas) {

            BloquearEtapaSorteio();

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (wSender, wE) => {

                processing = true;
            
                Action<string> updateStatus = (value) => Dispatcher.Invoke(() => { lblStatusSorteio.Content = value; });
                int falhas = 0;

                foreach(var lista in listas) {
                    try {
                        updateStatus($"Publicando lista \"{lista.Nome}\".");
                        Service.PublicarLista(lista.IdLista, false);
                        updateStatus("Publicação finalizada.");
                    } catch (Exception exception) {
                        if (listas.Count() == 1) {
                            if (exception is AggregateException) {
                                exception = (exception as AggregateException).InnerExceptions.First();
                            }
                            ShowErrorMessage($"Falha na publicação:\n\n {exception.Message}");
                            updateStatus($"Falha na publicação da lista \"{lista.Nome}\".");
                        } else {
                            falhas++;
                        }
                    }
                }

                if (falhas > 0) {
                    updateStatus($"Falha na publicação de {falhas} lista(s).");
                }

                Dispatcher.Invoke(() => EtapaSorteio(true));
                processing = false;
            };
            worker.RunWorkerAsync();
        }

        private void btnAbrirDiretorioExportacao_Click(object sender, RoutedEventArgs e) {
            Process.Start(new ProcessStartInfo() {
                FileName = Service.DiretorioExportacaoCSV,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
