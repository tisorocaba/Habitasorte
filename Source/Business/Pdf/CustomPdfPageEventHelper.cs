using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Habitasorte.Business.Pdf {
    public class CustomPdfPageEventHelper : PdfPageEventHelper {

        private string prefix;
        private DateTime printTime = DateTime.Now;
        private PdfContentByte cb;
        private BaseFont bf = null;
        private PdfTemplate template;

        public CustomPdfPageEventHelper(string prefix) {
            this.prefix = prefix;
        }

        public override void OnOpenDocument(PdfWriter writer, Document document) {
            printTime = DateTime.Now;
            cb = writer.DirectContent;
            bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            template = cb.CreateTemplate(50, 50);
        }

        public override void OnEndPage(PdfWriter writer, Document document) {

            base.OnEndPage(writer, document);

            string text = string.Format("{0} - Página {1:00} de ", prefix, writer.PageNumber);
            float len = bf.GetWidthPoint(text, 10);
            Rectangle pageSize = document.PageSize;

            cb.SetRGBColorFill(100, 100, 100);

            cb.BeginText();
            cb.SetFontAndSize(bf, 10);
            cb.SetTextMatrix(pageSize.GetLeft(20), pageSize.GetBottom(20));
            cb.ShowText(text);
            cb.EndText();

            cb.AddTemplate(template, pageSize.GetLeft(20) + len, pageSize.GetBottom(20));
            cb.BeginText();
            cb.SetFontAndSize(bf, 10);
            cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT,printTime.ToString(), pageSize.GetRight(20), pageSize.GetBottom(20), 0);
            cb.EndText();
        }

        public override void OnCloseDocument(PdfWriter writer, Document document) {

            base.OnCloseDocument(writer, document);

            template.BeginText();
            template.SetFontAndSize(bf, 10);
            template.SetTextMatrix(0, 0);
            template.ShowText("" + string.Format("{0:00}", (writer.PageNumber)));
            template.EndText();
        }
    }
}
