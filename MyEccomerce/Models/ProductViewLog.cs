namespace MyEccomerce.Models
{
    public class ProductViewLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }      // Kinsa nga specific user (Blangko kung guest)
        public int ProductId { get; set; }      // Unsa nga specific product ang gilantaw
        public int SecondsSpent { get; set; }   // Pila ka segundo/oras ang gipuyo sa page
        public DateTime ViewDateTime { get; set; } = DateTime.Now; // Ang saktong adlaw ug oras sa pag-view
    }
}
