using System.Security.Cryptography;
using System.Text;

namespace webapi.Token
{
    public class TokenHasher
    {
        public static string Hash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
