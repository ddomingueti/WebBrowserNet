using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WebBrowserNet {
    
    public class WebServer {
        static string localPath;

        static IDictionary<string, string> ParseRequisition(string req) {
            Match m = Regex.Match(req, @"(?<type>^\w*) (?<path>/\S*) HTTP/(?<version>\d\.\d)");
            
            string req_type = m.Groups["type"].Value;
            string path = m.Groups["path"].Value;
            string version_protocol = m.Groups["version"].Value;
            
            if (path == "/")
                path = "/index.html";

            IDictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("type", req_type);
            dict.Add("path", path);
            dict.Add("version", version_protocol);
            return dict;
        }

        static void CreateResponse(IDictionary<string, string> req_in) {
            
            HttpRequest response = new HttpRequest();

            byte[] content_data = null;
            
            if (File.Exists(req_in["path"])) {
                string extension = Path.GetExtension(req_in["path"]);
                string name = Path.GetFileName(req_in["path"]);
                content_data = File.ReadAllBytes(req_in["path"]);
                response.Code = 200;
                response.ContentLength = content_data.Length;
                if (extension == "html" || extension == "php" || extension == "txt" || extension == "")
                    response.ContentType = "text/" + extension;
                else if (extension == "jpg" || extension == "png" || extension == "tiff" || extension == "svg" || extension == "jpeg")
                    response.ContentType = "image/" + extension;
                else if (extension == "mp4" || extension == "avi" || extension == "mov" || extension == "flv" || extension == "wvm" || extension == "wav")
                    response.ContentType = "media/" + extension;
                else
                    response.ContentType = "binary/" + extension;

                string data = "";

            } else {
                
            }

            
        }

        static void Main(string[] args) {
            localPath = System.IO.Directory.GetCurrentDirectory();
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 80);
            
            server.Start();
            try {
                Console.WriteLine("Server running .... ");    
                TcpClient client = server.AcceptTcpClient();
                byte[] bytes = new byte[client.Available];
                Console.WriteLine("Client Connected {0}", client.Available);
                while (client.Available > 0) {
                    client.GetStream().Read(bytes, 0, bytes.Length);
                }
                
                Console.WriteLine(Encoding.ASCII.GetString(bytes));
                Console.WriteLine("Size: {0}", bytes.Length);
                client.Close();

            } catch (SocketException e) {
                Console.WriteLine(e.Message);
            }
        }
    
    
    }
}