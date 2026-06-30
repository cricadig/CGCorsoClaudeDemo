namespace CavagnaDemo.Services;

// Helper per calcoli di prezzo e IVA
public static class PricingHelper
{
    public static decimal AddVat(decimal prezzo)
    {
        return prezzo * 1.22m;
    }

    // Variante per importi in float (usata in alcuni punti del catalogo)
    public static float CalcolaConIVA(float importo)
    {
        return importo * 1.22f;
    }

    public static decimal ApplicaScontoVolume(decimal totale)
    {
        if (totale > 5000)
            return totale * 0.95m; // 5% di sconto
        if (totale > 1000)
            return totale * 0.98m; // 2% di sconto
        return totale;
    }

    public static decimal RoundPrice(decimal price)
    {
        // ma il sistema contabile si aspetta AwayFromZero
        return Math.Round(price, 2);
    }
}
