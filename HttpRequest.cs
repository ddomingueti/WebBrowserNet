using System.Text;
using System.Text.RegularExpressions;

namespace WebBrowserNet {

    internal class HttpRequest {
        public int Code { get; set; }
        public int ContentLength { get; set; }
        public string ContentType { get; set; }
        public string ContentExtension { get; set; }
        public byte[] Body { get; set; }
        public string RequestType { get; }
        public string UserAgent { get; set; }
        public string Date { get; set; }
        public string FileName { get; set; }

        internal HttpRequest() { }
        internal HttpRequest(string request_type, string name) { 
            Body = null;
            RequestType = request_type;
            FileName = name;
        }

        internal HttpRequest(int code, int length, string content_type, string content_extension, byte[] data, string request_type, string name) {
            Code = code;
            ContentLength = length;
            ContentType = content_type;
            RequestType = request_type;
            ContentExtension = content_extension;
            Body = data;
            FileName = name;
        }

        internal void ParseHeader(byte[] header) {
            string head = Encoding.ASCII.GetString(header);
            int code = 404;
            int content_length = 0;
            string content_type = string.Empty;
            string content_extension = string.Empty;           
            Match rg_match;

            string[] header_lines = head.Split("\n");
            foreach (string hl in header_lines) {
                if (hl.Contains("HTTP")) {
                    rg_match = Regex.Match(hl, @"\d{3}");
                    code = int.Parse(rg_match.Groups[0].Value);
                } else if (hl.Contains("Length")) {
                    rg_match = Regex.Match(hl, @"\d+");
                    content_length = int.Parse(rg_match.Groups[0].Value);
                } else if (hl.Contains("Type")) {
                    rg_match = Regex.Match(hl, @"(?<type>[\w]+/)(?<extension>[\w]+)");
                    content_type = rg_match.Groups["type"].Value;
                    content_type = content_type.Remove(content_type.Length - 1, 1);
                    content_extension = rg_match.Groups["extension"].Value;
                }
            }
            Code = code;
            ContentLength = content_length;
            ContentExtension = content_extension;
            ContentType = content_type;
        }

        public override string ToString() {
            string s = string.Empty;
            string message = string.Empty;
            if (Code == 200)
                message = "OK";
            else if (Code == 404)
                message = "Page or file not found on this server.";
            s += string.Format("HTTP/{0} {1} {2}\r\nDate: {3}\r\nServer: {4}\r\nContent-Length: {5}\r\nConnection: closed\r\nContent-Type: {6}/{7}\r\nUser-Agent: {8}\r\n\r\n", "1.0", Code, message, Date, UserAgent, ContentLength, ContentType, ContentExtension.Length, UserAgent);
            return s;
        }
    }

}