using System.Net;

namespace Program {
    public class Utils {
        public static bool IsValidIpv4(string ip) {
            try {
                IPAddress.Parse(ip);
            } catch (Exception e) {
                Console.WriteLine($"Invalid IP '{ip}'.");
                Console.WriteLine(e);

                return false;
            }

            return true;
        }
    }
}