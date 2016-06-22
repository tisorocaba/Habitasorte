using Excel;
using Habitasorte.Business.Model;
using Habitasorte.Business.Model.Publicacao;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business {

    public delegate void SorteioChangedEventHandler(Sorteio s);

    public class SorteioService {

        public event SorteioChangedEventHandler SorteioChanged;

        private Sorteio model;
        public Sorteio Model {
            get { return model; }
            set { model = value; SorteioChanged(model); }
        }

        public SorteioService() {
            Database.Initialize();
        }

        private void Execute(Action<Database> action) {
            using (SqlCeConnection connection = Database.CreateConnection()) {
                using (SqlCeTransaction tx = connection.BeginTransaction()) {
                    Database database = new Database(connection, tx);
                    try {
                        action(database);
                        tx.Commit();
                    } catch {
                        try { tx.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        private void AtualizarStatusSorteio(Database database, string status) {
            Model.StatusSorteio = status;
            database.AtualizarStatusSorteio(status);
        }

        /* Configuração */

        public void ExcluirBancoReiniciarAplicacao() {
            Database.ExcluirBanco();
            System.Windows.Application.Current.Shutdown();
        }

        /* Ações */

        public void CarregarConfiguracaoPublicacao() {
            Execute(d => {
                Model.ConfiguracaoPublicacao = d.CarregarConfiguracaoPublicacao();
            });
        }

        public void AtualizarConfiguracaoPublicacao() {
            Execute(d => {
                d.AtualizarConfiguracaoPublicacao(Model.ConfiguracaoPublicacao);
            });
        }

        public void CarregarSorteio() {
            Execute(d => {
                Model = d.CarregarSorteio();
            });
        }

        public void AtualizarSorteio() {
            Execute(d => {
                d.AtualizarSorteio(Model);
                AtualizarStatusSorteio(d, Status.IMPORTACAO);
            });
        }

        public void CarregarListas() {
            Execute(d => {
                Model.Listas = d.CarregarListas();
            });
            
        }

        public void CarregarProximaLista() {
            Execute(d => {
                Model.ProximaLista = d.CarregarProximaLista();
            });
        }

        public int ContagemCandidatos() {
            int contagemCandidatos = 0;
            Execute(d => {
                contagemCandidatos = d.ContagemCandidatos();
            });
            return contagemCandidatos;
        }

        public void AtualizarListas() {
            Execute(d => {
                d.AtualizarListas(Model.Listas);
                AtualizarStatusSorteio(d, Status.SORTEIO);
            });
        }

        public void CriarListasSorteio(string arquivoImportacao, Action<string> updateStatus, Action<int> updateProgress) {

            Execute(d => {
                using (Stream stream = File.OpenRead(arquivoImportacao)) {
                    using (IExcelDataReader excelReader = CreateExcelReader(arquivoImportacao, stream)) {

                        List<string> empreendimentos = new List<string>();
                        empreendimentos.Add(Model.Empreendimento1);
                        if (Model.Empreendimento2Ativo) {
                            empreendimentos.Add(Model.Empreendimento2);
                        }
                        if (Model.Empreendimento3Ativo) {
                            empreendimentos.Add(Model.Empreendimento3);
                        }
                        if (Model.Empreendimento4Ativo) {
                            empreendimentos.Add(Model.Empreendimento4);
                        }

                        d.CriarListasSorteio(empreendimentos, excelReader, updateStatus, updateProgress);
                    }
                }
                AtualizarStatusSorteio(d, Status.QUANTIDADES);
            });
        }

        private IExcelDataReader CreateExcelReader(string arquivoImportacao, Stream stream) {
            return (arquivoImportacao.ToLower().EndsWith(".xlsx")) ?
                ExcelReaderFactory.CreateOpenXmlReader(stream) : ExcelReaderFactory.CreateBinaryReader(stream);
        }

        public void SortearProximaLista(Action<string> updateStatus, Action<int> updateProgress, Action<string> logText, int? sementePersonalizada = null) {
            Execute(d => {
                d.SortearProximaLista(updateStatus, updateProgress, logText, sementePersonalizada);
                if (Model.StatusSorteio == Status.SORTEIO) {
                    AtualizarStatusSorteio(d, Status.SORTEIO_INICIADO);
                }
                if (d.CarregarProximaLista() == null) {
                    AtualizarStatusSorteio(d, Status.FINALIZADO);
                }
            });
        }

        public string DiretorioExportacaoCSV => Database.DiretorioExportacaoCSV;
        public bool DiretorioExportacaoCSVExistente => Directory.Exists(Database.DiretorioExportacaoCSV);

        public void ExportarListas(Action<string> updateStatus) {
            Execute(d => {
                d.ExportarListas(updateStatus);
            });
        }

        public string PublicarLista(int? idLista, bool teste = false) {

            string url = Model.ConfiguracaoPublicacao.UrlPublicacao;
            string codigo = Model.ConfiguracaoPublicacao.CodigoPublicacao;

            SorteioPub sorteioPublicacao = new SorteioPub {
                Codigo = int.Parse(codigo),
                Nome = Model.Nome
            };

            if (teste) {
                url += "?teste=true";
            } else {
                Execute(d => {
                    sorteioPublicacao.Listas = new List<ListaPub> {
                        d.CarregarListaPublicacao((int) idLista)
                    };
                });
            }

            string data = JsonConvert.SerializeObject(sorteioPublicacao);
            HttpContent responseContent = HttpPost(url, new StringContent(data, Encoding.UTF8, "application/json"), true);

            if (!teste) {
                Execute(d => {
                    d.PublicarLista((int) idLista);
                });
            }

            return responseContent.ReadAsStringAsync().Result;
        }

        private HttpContent HttpPost(string url, HttpContent requestContent, bool jsonContent = false) {

            string usuario = Model.ConfiguracaoPublicacao.UsuarioPublicacao;
            string senha = Model.ConfiguracaoPublicacao.SenhaPublicacao;

            using (HttpClient client = new HttpClient()) {

                string serviceToken = string.Format("{0}:{1}", usuario, senha);
                string encodedServiceToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(serviceToken));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedServiceToken);

                if (jsonContent) {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }

                HttpResponseMessage response = client.PostAsync(url, requestContent).Result;

                if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    throw new Exception($"{(int) response.StatusCode} - {response.ReasonPhrase} \n {responseContent}");
                } else {
                    return response.Content;
                }
            }
        }
    }
}
