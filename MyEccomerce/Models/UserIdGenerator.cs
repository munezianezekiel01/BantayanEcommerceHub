namespace MyEccomerce.Models
{
    public class UserIdGenerator
    {
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

        public static string UserIdStringGenerator(int dailySequence, string hubPrefix = "BH")
        {

            string datePart = DateTime.UtcNow.ToString("yyMMdd");


            string sequencePart = dailySequence.ToString("D4");

            string randomString = Random("ABCDEFGHIJKLMNOPQRSTUV", "0123456789");


            return $"{hubPrefix}{datePart}{sequencePart}{randomString}";
        }


    }
}
