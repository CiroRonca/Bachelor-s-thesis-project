using System.ComponentModel.DataAnnotations;
using Emgu.CV;
using Emgu.CV.Structure;

public class QualityCheck
{

    // Calcola la risoluzione dell'immagine e verifica se soddisfa la soglia minima
    public bool IsResolutionSufficient(Image<Bgr, byte> image, int minWidth = 600, int minHeight = 400, int maxWidth = 1920, int maxHeight = 1080)
    {
        int width = image.Width;
        int height = image.Height;

        Console.WriteLine($"[DEBUG] Resolution: {width}x{height}");

        // Verifica se l'immagine ha almeno la risoluzione minima richiesta
        return width >= minWidth  && height >= minHeight;
    }
    /*&& width <= maxWidth && height <= maxHeight*/

    // Metodo per calcolare la luminosità media
    public static double GetImageBrightness(Image<Bgr, byte> image)
    {
        // Converti l'immagine in scala di grigi
        var grayImage = image.Convert<Gray, byte>();

        // Calcola la luminosità come la media dei valori dei pixel
        double brightness = grayImage.GetAverage().Intensity;

        return brightness;
    }

    // Metodo per calcolare il contrasto (deviazione standard dei pixel)
    public static double GetImageContrast(Image<Bgr, byte> image)
    {
        var grayImage = image.Convert<Gray, byte>();

        // Ottieni i valori dei pixel
        byte[] pixelValues = grayImage.Bytes;

        // Calcola la media dei pixel
        double mean = pixelValues.Average(v => (double)v);

        // Calcola la deviazione standard
        double variance = pixelValues.Average(v => Math.Pow((double)v - mean, 2));
        double stddev = Math.Sqrt(variance);

        return stddev;
    }



    // Restituisce un elenco dei criteri non soddisfatti
    public List<string> GetUnsatisfiedCriteria(Image<Bgr, byte> image, double brightnessThreshold = 50, double contrastThreshold = 10, int minWidth = 600, int minHeight = 400, int maxWidth = 1920, int maxHeight = 1080)
    {
        var unsatisfiedCriteria = new List<string>();

        double brightness = GetImageBrightness(image);
        double contrast = GetImageContrast(image);
        bool resolution = IsResolutionSufficient(image,
                                        minWidth: minWidth,
                                        minHeight: minHeight,
                                        maxWidth: maxWidth,
                                        maxHeight: maxHeight);

        // Verifica la luminosità
        if (brightness < brightnessThreshold)
            unsatisfiedCriteria.Add($"Luminosità troppo bassa (minimo: {brightnessThreshold})");

        // Verifica il contrasto
        if (contrast < contrastThreshold)
            unsatisfiedCriteria.Add($"Contrasto troppo basso (minimo: {contrastThreshold})");

        // Verifica la risoluzione
        if (!resolution)
            unsatisfiedCriteria.Add($"Risoluzione non supportata (minima: {minWidth}x{minHeight}, massima: {maxWidth}x{maxHeight})");


        return unsatisfiedCriteria;
    }

    public bool IsQualitySufficient(Image<Bgr, byte> image, out List<string> unsatisfiedCriteria, double brightnessThreshold = 50, double contrastThreshold = 10, double sharpnessThreshold = 100, int minWidth = 600, int minHeight = 400, int maxWidth = 1920, int maxHeight = 1080)
    {
        unsatisfiedCriteria = GetUnsatisfiedCriteria(image, brightnessThreshold, contrastThreshold, minWidth, minHeight, maxWidth, maxHeight);

        // Se la lista di criteri non soddisfatti è vuota, significa che tutti i criteri sono soddisfatti
        return unsatisfiedCriteria.Count == 0;
    }
}
