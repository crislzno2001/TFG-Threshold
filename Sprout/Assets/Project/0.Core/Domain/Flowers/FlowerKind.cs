namespace Sprout.Domain.Flowers
{
    /// <summary>The seven emotional flowers. Spanish names kept as the in-world IDs.</summary>
    public enum FlowerKind
    {
        None = 0,
        Acuariana, // calm
        Brasa,     // passion
        Velada,    // sadness
        Sol,       // joy
        Inquieta,  // unease
        Crisalida, // secret  (Crisálida)
        Anima      // honesty (Ánima)
    }

    /// <summary>The eight bouquets craftable from two flowers.</summary>
    public enum BouquetKind
    {
        None = 0,
        Peace,         // Sol + Acuariana
        HiddenDesire,  // Brasa + Crisalida
        Comfort,       // Velada + Acuariana
        Obsession,     // Brasa + Inquieta
        Promise,       // Sol + Anima
        Confession,    // Crisalida + Anima
        Farewell,      // Velada + Brasa
        Suspicion      // Inquieta + Crisalida
    }
}
