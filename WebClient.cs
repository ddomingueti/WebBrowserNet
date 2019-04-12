using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

namespace WebBrowserNet {
    public class WebClient {
        static int packet_size = 1;
        static IDictionary<string, string> ParseInputHost(string input) {
            string path = "", host = null;
            Match m = Regex.Match(input, @"(?<host>[^\s/]*)(?<path>[^\s]*)");
            
            if (m.Success) {
                host = m.Groups["host"].Value;
                path += m.Groups["path"].Value;
                
                if (path == "/" && !path.Contains("/index.html"))
                    path += "index.html";
                
                IDictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("host", host);
                dict.Add("path", path);
                return dict;
            } else {
                throw new ArgumentException("Invalid URL format.");
            }
        }
        
        static void Main(string[] args) {
            try {
                if (args.Length == 0) {
                    throw new ArgumentException("Invalid input argument. Example input: dotnet run 'www.website.com/path/index.html'");
                }
                
                IDictionary<string, string> dict = ParseInputHost(args[0]);

                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(dict["host"], 80);
                
                string req = "GET " + dict["path"] +  " HTTP/1.1\r\nHost:" + dict["host"] + "\r\n\r\n";
                byte[] data = Encoding.UTF8.GetBytes(req, 0, req.Length);
                sock.Send(data, 0, data.Length, 0);
                
                byte[] response = new byte[packet_size]; //byte de leitura do socket
                List<byte> header_data = new List<byte>(); //bytes de leitura do header da requisição
                List<byte> body_data = new List<byte>(); //bytes de leitura do corpo da requisição
                List<byte> response_data_bytes = new List<byte>();
                int q = 0; //estado do automato para reconhecimento do header/corpo da requisicao
                while (sock.Receive(response, SocketFlags.Partial) > 0) {
                    if (q < 4) {
                        header_data.Add(response[0]); // \r\n = 13\10
                        if (((q == 0 || q == 2) && response[0] == 13) || (q == 1 || q == 3) && response[0] == 10)
                            q++;
                        else q = 0;
                    } else {
                        body_data.Add(response[0]);
                    }
                }

                sock.Close();
                HttpRequest http_result = new HttpRequest("GET", "Result");
                http_result.ParseHeader(header_data.ToArray());
                http_result.Body = body_data.ToArray();
                FileStream fs = File.Create(http_result.FileName + "." + http_result.ContentExtension);
                fs.Write(http_result.Body, 0, http_result.Body.Length);
                fs.Close();
                Console.WriteLine("Requisition success.");
            } catch (ArgumentException e) {
                Console.WriteLine(e.Message);
            } catch (SocketException e) {
                Console.WriteLine(e.Message);   
            }
        }
    }
}
