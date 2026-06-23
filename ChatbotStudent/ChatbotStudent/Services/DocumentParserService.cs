using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace ChatbotStudent.Services;

public class DocumentParserService : IDocumentParserService
{
    private readonly ILogger<DocumentParserService> _logger;

    public DocumentParserService(ILogger<DocumentParserService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".pdf" => ExtractPdfText(fileStream),
            ".docx" => ExtractDocxText(fileStream),
            ".pptx" => ExtractPptxText(fileStream),
            _ => throw new NotSupportedException($"File type '{ext}' is not supported.")
        };
    }

    private string ExtractPdfText(Stream stream)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(stream);

        foreach (var page in document.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
                sb.AppendLine();
            }
        }

        _logger.LogInformation("Extracted {Length} characters from PDF", sb.Length);
        return sb.ToString();
    }

    private string ExtractDocxText(Stream stream)
    {
        var sb = new StringBuilder();

        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            var text = para.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

        _logger.LogInformation("Extracted {Length} characters from DOCX", sb.Length);
        return sb.ToString();
    }

    private string ExtractPptxText(Stream stream)
    {
        var sb = new StringBuilder();

        using var doc = PresentationDocument.Open(stream, false);
        var presentationPart = doc.PresentationPart;
        if (presentationPart?.Presentation?.SlideIdList == null) return string.Empty;

        foreach (var slideId in presentationPart.Presentation.SlideIdList.ChildElements)
        {
            if (slideId is not SlideId slideIdElement) continue;

            var slidePart = presentationPart.GetPartById(slideIdElement.RelationshipId!) as SlidePart;
            if (slidePart?.Slide == null) continue;

            foreach (var shape in slidePart.Slide.Descendants<Shape>())
            {
                var textBody = shape.TextBody;
                if (textBody == null) continue;

                foreach (var para in textBody.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                {
                    var text = para.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.AppendLine(text);
                    }
                }
            }
        }

        _logger.LogInformation("Extracted {Length} characters from PPTX", sb.Length);
        return sb.ToString();
    }
}
