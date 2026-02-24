namespace ExamReader.Core.Ocr;

public class OcrRegion
{
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public BoundingBox BoundingBox { get; set; } = new();
    public int LineNumber { get; set; }
}

public class BoundingBox
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
