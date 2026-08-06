#if GENHASH
using System;
using System.Security.Cryptography;
using System.Text;

class Gen
{
    static void Main()
    {
        const string Salt = "p2C$7wZq-9Vf3K@xR#Lm!u84^N1azYTQ|H6jDgEe";
        while (true)
        {
            Console.Write("Code> ");
            var c = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(c)) break;
            using (var sha = SHA256.Create())
            {
                var h = sha.ComputeHash(Encoding.UTF8.GetBytes(Salt + c.Trim()));
                var sb = new StringBuilder(h.Length * 2);
                foreach (var b in h) sb.Append(b.ToString("x2"));
                Console.WriteLine(h: sb.ToString());
            }
        }
    }
}
#endif
