using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Habitasorte.Business.Model;
using Habitasorte.Business.Model.Publicacao;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Habitasorte.Business.Pdf {
    public class PdfFileWriter {
        internal static void WriteToPdf(string caminhoArquivo, Sorteio sorteio, ListaPub lista) {
            using (FileStream fileStream = new FileStream(caminhoArquivo, FileMode.Create)) {
                PdfWriter writer = null;
                using (Document document = new Document(PageSize.A4)) {

                    document.SetMargins(20, 20, 20, 40);
                    document.AddCreationDate();
                    writer = PdfWriter.GetInstance(document, fileStream);
                    writer.PageEvent = new CustomPdfPageEventHelper(lista.Nome);
                    document.Open();

                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    Paragraph p = new Paragraph();
                    p.Alignment = Element.ALIGN_CENTER;
                    p.Font = headerFont;
                    p.Add("PREFEITURA DE SOROCABA \n");
                    p.Add(string.Format("{0}\n", sorteio.Nome.ToUpper()));
                    p.Add(string.Format("{0:00} - {1}\n\n", lista.IdLista, lista.Nome.ToUpper()));
                    document.Add(p);

                    PdfPTable table = new PdfPTable(3);
                    table.WidthPercentage = 100f;
                    table.DefaultCell.HorizontalAlignment = 1;
                    table.SetWidths(new float[] { 1f, 2f, 8f });
                    table.HeaderRows = 1;

                    table.AddCell(new PdfPCell(new Phrase("Nº", headerFont)) {
                        HorizontalAlignment = 1,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });

                    table.AddCell(new PdfPCell(new Phrase("CPF", headerFont)) {
                        HorizontalAlignment = 1,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });

                    table.AddCell(new PdfPCell(new Phrase("NOME", headerFont)) {
                        HorizontalAlignment = 1,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });

                    foreach (CandidatoPub candidato in lista.Candidatos) {
                        table.AddCell(string.Format("{0:000}", candidato.IdCandidato));
                        table.AddCell(string.Format("{0:000'.'000'.'000-00}", candidato.Cpf));
                        table.AddCell(candidato.Nome.ToUpper());
                    }

                    document.Add(table);
                }
                writer.Close();
            }
        }
    }
}
