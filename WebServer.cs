using System;
using System.Threading;
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
            Match m = Regex.Match(req, @"(?<type>^\w*) /(?<path>\S*) HTTP/(?<version>\d\.\d)");
            string req_type = m.Groups["type"].Value;

            
            string path = localPath + m.Groups["path"].Value;
            string version_protocol = m.Groups["version"].Value;
 
            IDictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("type", req_type);
            dict.Add("path", path);
            dict.Add("version", version_protocol);
            return dict;
        }

        static byte[] CreateResponse(IDictionary<string, string> req_in) {
            HttpRequest response = new HttpRequest();
            FileAttributes attr = File.GetAttributes(req_in["path"]);
            if (File.Exists(req_in["path"])) {
                string ext = Path.GetExtension(req_in["path"]);
                string name = Path.GetFileNameWithoutExtension(req_in["path"]);
                string extension = ext.Substring(1, ext.Length - 1);

                response.Body = File.ReadAllBytes(req_in["path"]);
                response.Code = 200;
                response.ContentLength = response.Body.Length;
                response.ContentExtension = extension;
                response.Server = "dominguetiWebServer";
                if (extension == "html" || extension == "php" || extension == "txt")
                    response.ContentType = "text";
                else if (extension == "jpg" || extension == "png" || extension == "tiff" || extension == "svg" || extension == "jpeg")
                    response.ContentType = "image";
                else if (extension == "mp4" || extension == "avi" || extension == "mov" || extension == "flv" || extension == "wvm" || extension == "wav")
                    response.ContentType = "media";
                else
                    response.ContentType = "binary";
            } else if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                string extension = "html";
                response.ContentType = "text";
                response.Body = CreateIndex(req_in["path"]);
                response.ContentLength = response.Body.Length;
                response.ContentExtension = extension;
                response.Server = "dominguetiWebServer";
                response.Code = 200;
            } else {
                response.Code = 404;
                response.ContentType = "text";
                response.ContentExtension = "html";
                byte[] read = null;
                if (req_in["path"].Contains("www")) {
                    read = File.ReadAllBytes("404.html");
                } else {
                    response.Body = File.ReadAllBytes("www/404.html");
                }
                response.ContentLength = response.Body.Length;
            }
            response.Date = DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss GMT");

            string send_req = response.ToString();
            Console.WriteLine(send_req);
            byte[] data = Encoding.ASCII.GetBytes(send_req);
            byte[] full_data = new byte[data.Length + response.Body.Length];
            System.Buffer.BlockCopy(data, 0, full_data, 0, data.Length);
            System.Buffer.BlockCopy(response.Body, 0, full_data, data.Length, response.Body.Length);
            return full_data;
        }
        
        static byte[] CreateIndex(string path) {
            string htmlPage = "<html><body><h1>Localhost</h1><div style='pading-left: 25px'><ul>";
            string[] files = Directory.GetFiles(path);
            foreach (string s in files) {
                htmlPage += "<li><a href='" + Path.GetFileName(s) + "'>" + Path.GetFileName(s) + "</a></li>";
            }
            htmlPage += "</div></body></html>";
            return Encoding.ASCII.GetBytes(htmlPage);
        }

        static void ClientThread(TcpClient client) {
                byte[] bytes = new byte[client.Available];
                Console.WriteLine("Client Connected {0}", client.Available);
                while (client.Available > 0) {
                    client.GetStream().Read(bytes, 0, bytes.Length);
                }

                IDictionary<string, string> req_metadata = ParseRequisition(Encoding.ASCII.GetString(bytes));
                byte[] req_data = CreateResponse(req_metadata);
                
                client.GetStream().Write(req_data, 0, req_data.Length);
                client.GetStream().Flush();
                client.Close();
        }
        static void Main(string[] args) {
            localPath = System.IO.Directory.GetCurrentDirectory() + "/www/";
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 80);
            
            server.Start();
            while (true) {
                Console.WriteLine("Server running on port 80");    
                TcpClient client = server.AcceptTcpClient();
                Thread thread = new Thread(() => ClientThread(client));
                thread.Start();
            }
        }
    }
}