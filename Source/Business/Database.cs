using ErikEJ.SqlCe;
using Habitasorte.Business.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Habitasorte.Business.Model.Publicacao;

namespace Habitasorte.Business {
    public class Database {

        private static string ConnectionString { get; set; }

        private SqlCeConnection Connection { get; set; }
        private SqlCeTransaction Transaction { get; set; }

        public static void Initialize() {

            string dbFile = ConfigurationManager.AppSettings["ARQUIVO_BANCO"];
            string dbDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = $"{dbDirectory}{dbFile}";
            ConnectionString = $"DataSource=\"{dbPath}\";Max Database Size=4091;Case Sensitive=False;Locale Identifier=1046";

            if (!File.Exists(dbPath)) {
                using (SqlCeEngine engine = new SqlCeEngine(ConnectionString)) {
                    engine.CreateDatabase();
                }
                string scriptFile = ConfigurationManager.AppSettings["ARQUIVO_SCRIPT"];
                string scriptPath = $"{dbDirectory}{scriptFile}";
                string scriptText;
                using (StreamReader streamReader = new StreamReader(scriptPath, Encoding.UTF8)) {
                    scriptText = streamReader.ReadToEnd();
                }
                using (SqlCeConnection connection = CreateConnection()) {
                    foreach (string commandText in scriptText.Split(';')) {
                        if (!string.IsNullOrWhiteSpace(commandText)) {
                            using (SqlCeCommand command = new SqlCeCommand()) {
                                command.Connection = connection;
                                command.CommandType = CommandType.Text;
                                command.CommandText = commandText;
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        public static void ExcluirBanco() {
            string dbFile = ConfigurationManager.AppSettings["ARQUIVO_BANCO"];
            string dbDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = $"{dbDirectory}{dbFile}";
            if (File.Exists(dbPath)) {
                File.Delete(dbPath);
            }
        }

        public static SqlCeConnection CreateConnection() {
            SqlCeConnection connection = new SqlCeConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public Database(SqlCeConnection connection, SqlCeTransaction transaction) {
            Connection = connection;
            Transaction = transaction;
        }

        private SqlCeCommand CreateCommand(string commandText, params SqlCeParameter[] parameters) {
            SqlCeCommand command = new SqlCeCommand {
                Connection = Connection,
                Transaction = Transaction,
                CommandType = CommandType.Text,
                CommandText = commandText
            };
            command.Parameters.AddRange(parameters);
            return command;
        }

        private void ExecuteNonQuery(string commandText, params SqlCeParameter[] parameters) {
            using (SqlCeCommand command = CreateCommand(commandText, parameters)) {
                command.ExecuteNonQuery();
            }
        }

        private T ExecuteScalar<T>(string commandText, params SqlCeParameter[] parameters) {
            using (SqlCeCommand command = CreateCommand(commandText, parameters)) {
                return (T) command.ExecuteScalar();
            }
        }

        private string CarregarParametro(string nome) {
            using (SqlCeCommand command = CreateCommand("SELECT VALOR FROM PARAMETRO WHERE NOME = @NOME")) {
                command.Parameters.AddWithValue("@NOME", nome);
                return command.ExecuteScalar() as string;
            }
        }

        private void AtualizarParametro(string nome, string valor) {
            ExecuteNonQuery(
                "UPDATE PARAMETRO SET VALOR = @VALOR WHERE NOME = @NOME",
                new SqlCeParameter("NOME", nome),
                new SqlCeParameter("VALOR", (object) valor ?? DBNull.Value)
            );
        }

        /* Ações */

        public ConfiguracaoPublicacao CarregarConfiguracaoPublicacao() {
            return new ConfiguracaoPublicacao {
                UrlPublicacao = CarregarParametro("PUBLICACAO_URL"),
                CodigoPublicacao = CarregarParametro("PUBLICACAO_CODIGO"),
                UsuarioPublicacao = CarregarParametro("PUBLICACAO_USUARIO"),
                SenhaPublicacao = CarregarParametro("PUBLICACAO_SENHA")
            };
        }

        public void AtualizarConfiguracaoPublicacao(ConfiguracaoPublicacao configuracao) {
            AtualizarParametro("PUBLICACAO_URL", configuracao.UrlPublicacao);
            AtualizarParametro("PUBLICACAO_CODIGO", configuracao.CodigoPublicacao);
            AtualizarParametro("PUBLICACAO_USUARIO", configuracao.UsuarioPublicacao);
            AtualizarParametro("PUBLICACAO_SENHA", configuracao.SenhaPublicacao);
        }

        public Sorteio CarregarSorteio() {

            string empreendimento2 = CarregarParametro("EMPREENDIMENTO_2");
            string empreendimento3 = CarregarParametro("EMPREENDIMENTO_3");
            string empreendimento4 = CarregarParametro("EMPREENDIMENTO_4");

            return new Sorteio {
                Nome = CarregarParametro("NOME_SORTEIO"),
                StatusSorteio = CarregarParametro("STATUS_SORTEIO"),
                Empreendimento1 = CarregarParametro("EMPREENDIMENTO_1"),
                Empreendimento2Ativo = !String.IsNullOrWhiteSpace(empreendimento2),
                Empreendimento2 = empreendimento2,
                Empreendimento3Ativo = !String.IsNullOrWhiteSpace(empreendimento3),
                Empreendimento3 = empreendimento3,
                Empreendimento4Ativo = !String.IsNullOrWhiteSpace(empreendimento4),
                Empreendimento4 = empreendimento4
            };
        }

        public void AtualizarSorteio(Sorteio sorteio) {
            AtualizarParametro("NOME_SORTEIO", sorteio.Nome);
            AtualizarParametro("EMPREENDIMENTO_1", sorteio.Empreendimento1);
            AtualizarParametro("EMPREENDIMENTO_2", sorteio.Empreendimento2Ativo ? sorteio.Empreendimento2 : null);
            AtualizarParametro("EMPREENDIMENTO_3", sorteio.Empreendimento3Ativo ? sorteio.Empreendimento3 : null);
            AtualizarParametro("EMPREENDIMENTO_4", sorteio.Empreendimento4Ativo ? sorteio.Empreendimento4 : null);
        }

        public void AtualizarStatusSorteio(string status) {
            AtualizarParametro("STATUS_SORTEIO", status);
        }

        public ICollection<Lista> CarregarListas() {
            List<Lista> listas = new List<Lista>();
            using (SqlCeCommand command = CreateCommand($"SELECT * FROM LISTA ORDER BY ORDEM_SORTEIO")) {
                using (SqlCeResultSet resultSet = command.ExecuteResultSet(ResultSetOptions.None)) {
                    while (resultSet.Read()) {
                        listas.Add(new Lista {
                            IdLista = resultSet.GetInt32(resultSet.GetOrdinal("ID_LISTA")),
                            OrdemSorteio = resultSet.GetInt32(resultSet.GetOrdinal("ORDEM_SORTEIO")),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                            Quantidade = resultSet.GetInt32(resultSet.GetOrdinal("QUANTIDADE")),
                            Sorteada = resultSet.GetBoolean(resultSet.GetOrdinal("SORTEADA")),
                            Publicada = resultSet.GetBoolean(resultSet.GetOrdinal("PUBLICADA"))
                        });
                    }
                }
            }
            return listas;
        }

        public Lista CarregarProximaLista() {
            using (SqlCeCommand command = CreateCommand("SELECT TOP(1) * FROM LISTA WHERE SORTEADA = 0 ORDER BY ORDEM_SORTEIO")) {
                using (SqlCeResultSet resultSet = command.ExecuteResultSet(ResultSetOptions.None)) {
                    if (resultSet.Read()) {
                        int idLista = resultSet.GetInt32(resultSet.GetOrdinal("ID_LISTA"));
                        return new Lista {
                            IdLista = idLista,
                            OrdemSorteio = resultSet.GetInt32(resultSet.GetOrdinal("ORDEM_SORTEIO")),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                            Quantidade = resultSet.GetInt32(resultSet.GetOrdinal("QUANTIDADE")),
                            CandidatosDisponiveis = CandidatosDisponiveisLista(idLista)
                        };
                    } else {
                        return null;
                    }
                }
            }
        }

        public int ContagemCandidatos() {
            return ExecuteScalar<int>("SELECT COUNT(*) FROM CANDIDATO");
        }

        private int CandidatosDisponiveisLista(int idLista) {
            return ExecuteScalar<int>($"SELECT COUNT(*) FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO WHERE CANDIDATO.CONTEMPLADO = 0 AND CANDIDATO_LISTA.ID_LISTA = {idLista}");
        }

        public void AtualizarListas(ICollection<Lista> listas) {
            foreach (Lista lista in listas) {
                ExecuteNonQuery(
                    "UPDATE LISTA SET QUANTIDADE = @QUANTIDADE WHERE ID_LISTA = @ID_LISTA",
                    new SqlCeParameter("ID_LISTA", lista.IdLista),
                    new SqlCeParameter("QUANTIDADE", lista.Quantidade)
                );
            }
        }

        public void CriarListasSorteio(List<string> empreendimentos, IDataReader dataReader, Action<string> updateStatus, Action<int> updateProgress) {

            int listaAtual = 0;
            int totalListas = 9 * empreendimentos.Count();
            updateStatus("Iniciando importação...");

            /* Exclui os dados anteriores. */

            ExecuteNonQuery("DELETE FROM CANDIDATO_LISTA");
            ExecuteNonQuery("DELETE FROM CANDIDATO");
            ExecuteNonQuery("DELETE FROM LISTA");

            /* Copia os candidatos da lista de importação. */

            updateStatus("Importando candidatos.");
            SqlCeBulkCopy bulkCopy = new SqlCeBulkCopy(Connection, Transaction);
            bulkCopy.DestinationTableName = "CANDIDATO";
            bulkCopy.ColumnMappings.Add(new SqlCeBulkCopyColumnMapping(0, "CPF"));
            bulkCopy.ColumnMappings.Add(new SqlCeBulkCopyColumnMapping(1, "NOME"));
            bulkCopy.ColumnMappings.Add(new SqlCeBulkCopyColumnMapping(2, "QUANTIDADE_CRITERIOS"));
            bulkCopy.ColumnMappings.Add(new SqlCeBulkCopyColumnMapping(3, "LISTA_DEFICIENTES"));
            bulkCopy.ColumnMappings.Add(new SqlCeBulkCopyColumnMapping(4, "LISTA_IDOSOS"));
            bulkCopy.ColumnMappings.Add(new SqlCeBulkCopyColumnMapping(5, "LISTA_INDICADOS"));
            bulkCopy.WriteToServer(dataReader);

            /* Gera as listas de sorteio para os empreendimentos. */

            int qtdEmpreendimentos = empreendimentos.Count();
            int incrementoOrdem = 1;
            int idUltimaLista;

            foreach (string emp in empreendimentos) {

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Deficientes", 0, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE LISTA_DEFICIENTES = 1");
                ClassificarListaSorteioSimples(idUltimaLista);

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Idosos", 1, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE LISTA_IDOSOS = 1");
                ClassificarListaSorteioSimples(idUltimaLista);

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Indicados", 2, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE LISTA_INDICADOS = 1");
                ClassificarListaSorteioSimples(idUltimaLista);

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Geral I", 3, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO");
                ClassificarListaSorteioComposto(idUltimaLista);

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Geral II", 4, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE QUANTIDADE_CRITERIOS < 5");
                ClassificarListaSorteioConstante(idUltimaLista);

                incrementoOrdem++;
            }

            /* Gera as listas de sorteio de reserva para os empreendimentos. */

            incrementoOrdem = 1;

            foreach (string emp in empreendimentos) {

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Deficientes (Reserva)", 5, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE LISTA_DEFICIENTES = 1");
                ClassificarListaSorteioConstante(idUltimaLista);

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Idosos (Reserva)", 6, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE LISTA_IDOSOS = 1");
                ClassificarListaSorteioConstante(idUltimaLista);

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Indicados (Reserva)", 7, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($@"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE LISTA_INDICADOS = 1");
                ClassificarListaSorteioConstante(idUltimaLista);

                updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
                updateProgress((int) ((listaAtual / (double) totalListas) * 100));

                idUltimaLista = CriarListaSorteio(emp, "Geral (Reserva)", 8, incrementoOrdem, qtdEmpreendimentos);
                ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA(ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO");
                ClassificarListaSorteioConstante(idUltimaLista);

                incrementoOrdem++;
            }

            updateStatus("Finalizando importação.");
        }

        private int CriarListaSorteio(string empreendimento, string nomeLista, int fatorLista, int incremento, int qtdEmpreendimentos) {
            ExecuteNonQuery(
                $"INSERT INTO LISTA(NOME, ORDEM_SORTEIO, QUANTIDADE, SORTEADA, PUBLICADA) VALUES(@EMPREENDIMENTO + ' - {nomeLista}', {fatorLista} * @QTD_EMPREENDIMENTOS + @INCREMENTO_ORDEM, 1, 0, 0);",
                new SqlCeParameter("EMPREENDIMENTO", empreendimento) { DbType = DbType.String },
                new SqlCeParameter("QTD_EMPREENDIMENTOS", qtdEmpreendimentos),
                new SqlCeParameter("INCREMENTO_ORDEM", incremento)
            );
            return (int) ExecuteScalar<decimal>("SELECT @@IDENTITY");
        }

        private void ClassificarListaSorteioSimples(int idUltimaLista) {
            ClassificarListaSorteio(idUltimaLista, "SIMPLES");
        }

        private void ClassificarListaSorteioComposto(int idUltimaLista) {
            ClassificarListaSorteio(idUltimaLista, "COMPOSTO");
        }

        private void ClassificarListaSorteioConstante(int idUltimaLista) {
            ClassificarListaSorteio(idUltimaLista, "CONSTANTE");
        }

        private void ClassificarListaSorteio(int idUltimaLista, string tipoOrdenacao) {

            List<CandidatoGrupo> candidatosLista = new List<CandidatoGrupo>();

            using (SqlCeCommand command = CreateCommand("SELECT * FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO WHERE CANDIDATO_LISTA.ID_LISTA = @ID_LISTA")) {
                command.Parameters.AddWithValue("ID_LISTA", idUltimaLista);
                using (SqlCeResultSet resultSet = command.ExecuteResultSet(ResultSetOptions.None)) {
                    while (resultSet.Read()) {
                        candidatosLista.Add(new CandidatoGrupo {
                            IdCandidato = resultSet.GetInt32(resultSet.GetOrdinal("ID_CANDIDATO")),
                            Cpf = resultSet.GetDecimal(resultSet.GetOrdinal("CPF")),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")).ToUpper().TrimEnd(),
                            QuantidadeCriterios = resultSet.GetInt32(resultSet.GetOrdinal("QUANTIDADE_CRITERIOS"))
                        });
                    }
                }
            }

            CandidatoGrupo[] candidatosOrdenados;

            if (tipoOrdenacao == "SIMPLES") {
                candidatosOrdenados = candidatosLista
                    .OrderByDescending(c => c.QuantidadeCriterios)
                    .ThenBy(c => c.Nome)
                    .ThenByDescending(c => c.Cpf)
                    .ToArray();
            }
            
            else if (tipoOrdenacao == "COMPOSTO") {
                candidatosOrdenados = candidatosLista
                    .OrderByDescending(c => c.QuantidadeCriteriosComposta)
                    .ThenBy(c => c.Nome)
                    .ThenByDescending(c => c.Cpf)
                    .ToArray();
            }
            
            else {
                candidatosOrdenados = candidatosLista
                    .OrderBy(c => c.Nome)
                    .ThenByDescending(c => c.Cpf)
                    .ToArray();
            }

            CandidatoGrupo candidatoAnterior = null;
            int sequencia = 1;
            int classificacao = 1;

            SqlCeCommand updateCommand = CreateCommand(
                "UPDATE CANDIDATO_LISTA SET SEQUENCIA = @SEQUENCIA, CLASSIFICACAO = @CLASSIFICACAO WHERE ID_LISTA = @ID_LISTA AND ID_CANDIDATO = @ID_CANDIDATO",
                new SqlCeParameter("SEQUENCIA", -1),
                new SqlCeParameter("CLASSIFICACAO", -1),
                new SqlCeParameter("ID_LISTA", idUltimaLista),
                new SqlCeParameter("ID_CANDIDATO", -1)
            );
            updateCommand.Prepare();

            foreach (CandidatoGrupo candidato in candidatosOrdenados) {

                if (candidatoAnterior != null) {
                    if (tipoOrdenacao == "SIMPLES" && candidato.QuantidadeCriterios != candidatoAnterior.QuantidadeCriterios) {
                        classificacao++;
                    } else if (tipoOrdenacao == "COMPOSTO" && candidato.QuantidadeCriteriosComposta != candidatoAnterior.QuantidadeCriteriosComposta) {
                        classificacao++;
                    }
                }

                updateCommand.Parameters["SEQUENCIA"].Value = sequencia;
                updateCommand.Parameters["CLASSIFICACAO"].Value = classificacao;
                updateCommand.Parameters["ID_CANDIDATO"].Value = candidato.IdCandidato;
                updateCommand.ExecuteNonQuery();

                sequencia++;
                candidatoAnterior = candidato;
            }
        }

        public void SortearProximaLista(Action<string> updateStatus, Action<int> updateProgress, Action<string> logText, int? sementePersonalizada = null) {

            updateStatus("Iniciando sorteio...");

            Lista proximaLista = CarregarProximaLista();
            if (proximaLista == null) {
                throw new Exception("Não existem listas disponíveis para sorteio.");
            }

            double quantidadeAtual = 0;
            double quantidadeTotal = Math.Min(proximaLista.Quantidade, (int) proximaLista.CandidatosDisponiveis);

            string fonteSemente = "PERSONALIZADA";
            int semente = (sementePersonalizada == null) ? ObterSemente(ref fonteSemente) : (int) sementePersonalizada;
            ExecuteNonQuery(
                "UPDATE LISTA SET SORTEADA = 1, SEMENTE_SORTEIO = @SEMENTE_SORTEIO, FONTE_SEMENTE = @FONTE_SEMENTE WHERE ID_LISTA = @ID_LISTA",
                new SqlCeParameter("SEMENTE_SORTEIO", semente),
                new SqlCeParameter("FONTE_SEMENTE", fonteSemente),
                new SqlCeParameter("ID_LISTA", proximaLista.IdLista)
            );
            Random random = new Random(semente);

            string queryGrupoSorteio = @"
                SELECT TOP(1) CANDIDATO_LISTA.CLASSIFICACAO AS CLASSIFICACAO, COUNT(*) AS QUANTIDADE
                FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO
                WHERE CANDIDATO_LISTA.ID_LISTA = @ID_LISTA AND CANDIDATO_LISTA.DATA_CONTEMPLACAO IS NULL AND CANDIDATO.CONTEMPLADO = 0
                GROUP BY CANDIDATO_LISTA.CLASSIFICACAO
                ORDER BY CANDIDATO_LISTA.CLASSIFICACAO
            ";
            SqlCeCommand commandGrupoSorteio = CreateCommand(queryGrupoSorteio);
            commandGrupoSorteio.Parameters.AddWithValue("ID_LISTA", proximaLista.IdLista);
            commandGrupoSorteio.Prepare();

            string queryCandidatosGrupo = @"
                SELECT CANDIDATO_LISTA.SEQUENCIA, CANDIDATO.ID_CANDIDATO, CANDIDATO.CPF, CANDIDATO.NOME
                FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO
                WHERE CANDIDATO_LISTA.ID_LISTA = @ID_LISTA AND CANDIDATO_LISTA.DATA_CONTEMPLACAO IS NULL AND CANDIDATO.CONTEMPLADO = 0 AND CANDIDATO_LISTA.CLASSIFICACAO = @CLASSIFICACAO
                ORDER BY CANDIDATO_LISTA.SEQUENCIA
            ";
            SqlCeCommand commandCandidatosGrupo = CreateCommand(queryCandidatosGrupo);
            commandCandidatosGrupo.Parameters.AddWithValue("ID_LISTA", proximaLista.IdLista);
            commandCandidatosGrupo.Parameters.AddWithValue("CLASSIFICACAO", -1);
            commandCandidatosGrupo.Prepare();

            GrupoSorteio grupoSorteio = null;

            for (int i = 1; i <= proximaLista.Quantidade; i++) {

                if (grupoSorteio == null || grupoSorteio.Quantidade < 1) {
                    updateStatus("Carregando próximo grupo de sorteio.");
                    using (SqlCeResultSet resultSet = commandGrupoSorteio.ExecuteResultSet(ResultSetOptions.None)) {
                        if (resultSet.Read()) {
                            grupoSorteio = new GrupoSorteio {
                                Classificacao = resultSet.GetInt32(resultSet.GetOrdinal("CLASSIFICACAO")),
                                Quantidade = resultSet.GetInt32(resultSet.GetOrdinal("QUANTIDADE"))
                            };
                        } else {
                            grupoSorteio = null;
                        }
                    }
                    if (grupoSorteio != null) {
                        commandCandidatosGrupo.Parameters["CLASSIFICACAO"].Value = grupoSorteio.Classificacao;
                        using (SqlCeResultSet resultSet = commandCandidatosGrupo.ExecuteResultSet(ResultSetOptions.None)) {
                            while (resultSet.Read()) {
                                CandidatoGrupo candidato = new CandidatoGrupo {
                                    Sequencia = resultSet.GetInt32(resultSet.GetOrdinal("SEQUENCIA")),
                                    IdCandidato = resultSet.GetInt32(resultSet.GetOrdinal("ID_CANDIDATO")),
                                    Cpf = resultSet.GetDecimal(resultSet.GetOrdinal("CPF")),
                                    Nome = resultSet.GetString(resultSet.GetOrdinal("NOME"))
                                };
                                grupoSorteio.Candidatos.Add(candidato.Sequencia, candidato);
                            }
                        }
                    }
                }
                
                if (grupoSorteio == null) {
                    break;
                } else {
                    updateStatus($"Sorteando entre o grupo de classificação \"{grupoSorteio.Classificacao}\": {quantidadeTotal - quantidadeAtual} vagas restantes.");
                }

                int indiceSorteado = (grupoSorteio.Quantidade == 1) ? 0 : random.Next(0, grupoSorteio.Quantidade);
                CandidatoGrupo candidatoSorteado = grupoSorteio.Candidatos.Skip(indiceSorteado).Take(1).First().Value;
                grupoSorteio.Candidatos.Remove(candidatoSorteado.Sequencia);

                ExecuteNonQuery(
                    "UPDATE CANDIDATO SET CONTEMPLADO = 1 WHERE ID_CANDIDATO = @ID_CANDIDATO",
                    new SqlCeParameter("ID_CANDIDATO", candidatoSorteado.IdCandidato)
                );

                ExecuteNonQuery(
                    @"
                        UPDATE CANDIDATO_LISTA
                        SET SEQUENCIA_CONTEMPLACAO = @SEQUENCIA_CONTEMPLACAO, DATA_CONTEMPLACAO = @DATA_CONTEMPLACAO
                        WHERE ID_CANDIDATO = @ID_CANDIDATO AND ID_LISTA = @ID_LISTA
                    ",
                    new SqlCeParameter("SEQUENCIA_CONTEMPLACAO", i),
                    new SqlCeParameter("DATA_CONTEMPLACAO", DateTime.Now),
                    new SqlCeParameter("ID_CANDIDATO", candidatoSorteado.IdCandidato),
                    new SqlCeParameter("ID_LISTA", proximaLista.IdLista)
                );

                grupoSorteio.Quantidade--;
                quantidadeAtual++;

                updateProgress((int) ((quantidadeAtual / quantidadeTotal) * 100));
                logText(string.Format("{0:0000} - {1:000'.'000'.'000-00} - {2}", i, candidatoSorteado.Cpf, candidatoSorteado.Nome.ToUpper()));
            }

            updateStatus("Sorteio da lista finalizado!");
        }

        private int ObterSemente(ref string fonteSemente) {

            int? semente = null;

            try {
                using (HttpClient client = new HttpClient()) {
                    HttpResponseMessage response = client.GetAsync(@"https://www.random.org/cgi-bin/randbyte?nbytes=4&format=h").Result;
                    if (response.StatusCode == HttpStatusCode.OK) {
                        string content = response.Content.ReadAsStringAsync().Result;
                        semente = Convert.ToInt32(content.Replace(" ", ""), 16);
                        fonteSemente = "RANDOM.ORG";
                    }
                }
            } catch {}

            if (semente == null) {
                fonteSemente = "SISTEMA";
                return (int) DateTime.Now.Ticks;
            } else {
                return (int) semente;
            }
        }

        public static string DiretorioExportacaoCSV => $"{AppDomain.CurrentDomain.BaseDirectory}CSV";

        public void ExportarListas(Action<string> updateStatus) {

            updateStatus("Iniciando exportação...");

            string directoryPath = DiretorioExportacaoCSV;
            if (Directory.Exists(directoryPath)) {
                updateStatus("Excluindo arquivos anteriores.");
                Directory.Delete(directoryPath, true);
            }
            Directory.CreateDirectory(directoryPath);

            string[] tabelas = new string[] { "CANDIDATO", "LISTA", "CANDIDATO_LISTA" };
            foreach (string tabela in tabelas) {
                WriteTable(directoryPath, tabela, updateStatus);
            }

            updateStatus("Finalizando exportação...");
        }

        private void WriteTable(string directoryPath, string tableName, Action<string> updateStatus) {

            int count = 0;
            int total = ExecuteScalar<int>($"SELECT COUNT(*) FROM {tableName}");

            using (StreamWriter writter = new StreamWriter($"{directoryPath}/{tableName}.CSV")) {
                using (SqlCeCommand command = CreateCommand($"SELECT * FROM {tableName}")) {
                    using (SqlCeDataReader dataReader = command.ExecuteReader()) {
                        IEnumerable<int> fieldRange = Enumerable.Range(0, dataReader.FieldCount);
                        CsvWriter.WriteRow(writter, fieldRange.Select(i => dataReader.GetName(i).ToLower()).ToArray());
                        while (dataReader.Read()) {
                            updateStatus($"Exportando tabela \"{tableName}\" - linha {++count} de {total}.");
                            CsvWriter.WriteRow(
                                writter,
                                fieldRange.Select(i => dataReader.GetValue(i))
                                    .Select(i => {
                                        if (i is bool) {
                                            return ((bool) i) ? "1" : "0";
                                        } else {
                                            return i.ToString();
                                        }
                                    })
                                    .ToArray()
                            );
                        }
                    }
                };
            }
        }

        public ListaPub CarregarListaPublicacao(int idLista) {

            ListaPub lista;

            using (SqlCeCommand command = CreateCommand("SELECT * FROM LISTA WHERE ID_LISTA = @ID_LISTA")) {
                command.Parameters.AddWithValue("ID_LISTA", idLista);
                using (SqlCeResultSet resultSet = command.ExecuteResultSet(ResultSetOptions.None)) {
                    resultSet.Read();
                    lista = new ListaPub() {
                        IdLista = resultSet.GetInt32(resultSet.GetOrdinal("ORDEM_SORTEIO")),
                        Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                        FonteSementeSorteio = resultSet.GetString(resultSet.GetOrdinal("FONTE_SEMENTE")),
                        SementeSorteio = resultSet.GetInt32(resultSet.GetOrdinal("SEMENTE_SORTEIO")),
                        Candidatos = new List<CandidatoPub>()
                    };
                }
            }

            string queryCandidatos = @"
                SELECT
                    CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO, CANDIDATO.CPF, CANDIDATO.NOME, QUANTIDADE_CRITERIOS
                FROM
                    CANDIDATO_LISTA
                    INNER JOIN LISTA ON CANDIDATO_LISTA.ID_LISTA = LISTA.ID_LISTA
                    INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO
                WHERE LISTA.ID_LISTA = @ID_LISTA AND CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO IS NOT NULL
                ORDER BY CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO
            ";

            using (SqlCeCommand command = CreateCommand(queryCandidatos)) {
                command.Parameters.AddWithValue("ID_LISTA", idLista);
                using (SqlCeResultSet resultSet = command.ExecuteResultSet(ResultSetOptions.None)) {
                    while (resultSet.Read()) {
                        lista.Candidatos.Add(new CandidatoPub {
                            IdCandidato = resultSet.GetInt32(resultSet.GetOrdinal("SEQUENCIA_CONTEMPLACAO")),
                            Cpf = resultSet.GetDecimal(resultSet.GetOrdinal("CPF")),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                            QuantidadeCriterios = resultSet.GetInt32(resultSet.GetOrdinal("QUANTIDADE_CRITERIOS"))
                        });
                    }
                }
            }

            return lista;
        }

        public void PublicarLista(int idLista) {
            ExecuteNonQuery(
                "UPDATE LISTA SET PUBLICADA = 1 WHERE ID_LISTA = @ID_LISTA",
                new SqlCeParameter("ID_LISTA", idLista)
            );
        }
    }
}
