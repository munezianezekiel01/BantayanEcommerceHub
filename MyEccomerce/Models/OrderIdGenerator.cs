namespace MyEccomerce.Models
{
    public class OrderIdGenerator
    {

        public static string GeneratedOrderId(int dailySequence, string hubPrefix = "BH")
        {

            string datePart = DateTime.UtcNow.ToString("yyMMdd");


            string sequencePart = dailySequence.ToString("D4");

            string rand = Random("ABCDEFGHIJKLMNOPQRSTUV", "0123456789");


            return $"{hubPrefix}{datePart}{sequencePart}{rand}";

        }

        public static string Random(string randomstring, string number)
        {
            Random rand = new Random();



            
            

            for (int random = 0; random < randomstring.Length; random++)
            {
                randomstring = rand.GetString("ABCDEFGHIGKLMNOPQRSTUVWXYZ", 2);
                number = rand.GetString("012345679", 2);

            }



            return $"{randomstring.ToString()}{number.ToString()}";
        }
    }
}
