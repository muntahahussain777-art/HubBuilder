using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HubBuilders
{
    /// <summary>
    /// Plain API (api_key) client for SendPK.
    /// </summary>
    public static class SendPkClient
    {
        // ---- REQUIRED ----
        public static string ApiKey = "923323661295-55c0e287-c2ee-42b9-941a-20685c9a302c"; // <- tumhara API KEY
        public static string Sender = "BrandName"; // <- PTA approved sender ID (mask)

        private const string BaseUrl = "https://sendpk.com/api/sms.php";

        /// <summary>
        /// Send a single or CSV mobile numbers (e.g. "92333...,92331...") SMS.
        /// Returns true if response starts with "OK".
        /// </summary>
        public static bool TrySendSms(
            string toMobileE164Csv,
            string message,
            bool unicodeIfNeeded,
            out string rawResponse,
            out string humanMessage,
            string network = null,      // "Jazz","Zong","Ufone","Telenor" (optional)
            string format = null,       // "json" | "xml" (optional)
            string scheduleDate = null, // dd-mm-YYYY (optional)
            string scheduleTime = null  // hh:mm:ss (optional)
        )
        {
            rawResponse = null;
            humanMessage = null;

            try
            {
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;

                    // POST preferred to avoid URL length issues
                    var data = new NameValueCollection
                    {
                        ["api_key"] = ApiKey,
                        ["sender"] = Sender,
                        ["mobile"] = toMobileE164Csv,
                        ["message"] = message
                    };

                    if (unicodeIfNeeded) data["type"] = "unicode";
                    if (!string.IsNullOrWhiteSpace(network)) data["network"] = network;
                    if (!string.IsNullOrWhiteSpace(format)) data["format"] = format;
                    if (!string.IsNullOrWhiteSpace(scheduleDate)) data["date"] = scheduleDate;
                    if (!string.IsNullOrWhiteSpace(scheduleTime)) data["time"] = scheduleTime;

                    var bytes = wc.UploadValues(BaseUrl, "POST", data);
                    rawResponse = Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>()).Trim();
                }

                // success sample: "OK ID:6502124"
                if (!string.IsNullOrEmpty(rawResponse) &&
                    rawResponse.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                {
                    humanMessage = "Message accepted for delivery.";
                    return true;
                }

                // error codes mapping
                var code = ExtractFirstToken(rawResponse);
                switch (code)
                {
                    case "1": humanMessage = "Invalid Username Or Password"; break;
                    case "2": humanMessage = "Please Type Username"; break;
                    case "3": humanMessage = "Please Type Password"; break;
                    case "4": humanMessage = "Please Type Sender ID"; break;
                    case "-4": humanMessage = "Invalid/Unapproved Sender ID (PTA)"; break;
                    case "5": humanMessage = "Please Type Receiver Mobile Number"; break;
                    case "6": humanMessage = "Please Type Your Message"; break;
                    case "-6": humanMessage = "Message Length Exceeded"; break;
                    case "7": humanMessage = "Invalid Recipient Mobile Number"; break;
                    case "8": humanMessage = "Low Credit"; break;
                    case "9": humanMessage = "SMS Rejected"; break;
                    case "10": humanMessage = "Sending Failed"; break;
                    case "11": humanMessage = "Invalid Scheduled Date"; break;
                    default: humanMessage = "Unexpected response"; break;
                }
                return false;
            }
            catch (WebException ex)
            {
                var http = ex.Response as HttpWebResponse;
                if (http != null)
                {
                    humanMessage = $"HTTP {(int)http.StatusCode} {http.StatusCode}";
                    return false;
                }
                humanMessage = "Network error";
                return false;
            }
            catch (Exception)
            {
                humanMessage = "Unexpected error";
                return false;
            }
        }

        private static string ExtractFirstToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var parts = s.Split(new[] { ' ', '\t', '\r', '\n', ':' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : "";
        }
    }
}