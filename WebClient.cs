using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

namespace WebBrowserNet {
    public class WebClient {
        static int packet_size = 128;
        static IDictionary<string, string> ParseInputHost(string input) {
            string path = "", host = null;
            Match m = Regex.Match(input, @"(?<host>[^\s/]*)(?<path>[^\s]*)");
            
            if (m.Success) {
                host = m.Groups["host"].Value;
                path += m.Groups["path"].Value;
                
                
                if (path == "")
                    path = "/";

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
                //Try to connect to host
                Socket sock = null;
                IPHostEntry hostEntry = Dns.GetHostEntry(dict["host"]);
                foreach (IPAddress address in hostEntry.AddressList) {
                    IPEndPoint ipe = new IPEndPoint(address, 80);
                    sock = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    sock.Connect(ipe);
                    if (sock.Connected)
                        break;
                }
                
                string req = "GET " + dict["path"] +  " HTTP/1.1\r\nUser-Agent: DominguetiClient\r\nHost: " + dict["host"] + "\r\n\r\n";
                byte[] data = Encoding.UTF8.GetBytes(req, 0, req.Length);
                sock.Send(data, 0, data.Length, 0);
                Console.WriteLine("Sended req ...");
                byte[] response = new byte[packet_size]; //byte de leitura do socket
                List<byte> response_data_bytes = new List<byte>();
                int amount_read = 0;

                do {
                    amount_read = sock.Receive(response, 0, response.Length, SocketFlags.None);
                    foreach (byte b in response)
                        response_data_bytes.Add(b);
                    Console.WriteLine("Readed " + amount_read);
                } while ( amount_read >= packet_size);
                sock.Close();

                //Split bytes in header data and body data
                response = response_data_bytes.ToArray();
                int q = 0;
                int endHeader = 0;
                int i = 0;
                while (i < response.Length && q < 4) {
                    if (((q == 0 || q == 2) && (response[i] == 13)) || ((q == 1 || q == 3) && response[i] == 10))
                        q++;
                    else
                        q = 0;
                    i = i + 1;
                }

                endHeader = i;
                byte[] header_data = new byte[endHeader];
                Array.Copy(response, 0, header_data, 0, endHeader);
                byte[] body_data = new byte[response.Length - endHeader];
                int size = response.Length - endHeader;
                Console.WriteLine("Header: " + endHeader + " RL-H = " + size);
                Array.Copy(response, endHeader, body_data, 0, size);

                response = null;
                Console.WriteLine("Connection Closed.");

                HttpRequest http_result = new HttpRequest("GET", "Result");
                http_result.ParseHeader(header_data);
                
                if (http_result.Code == 404 && body_data.Length == 0) {
                    string httperror = "<html><h1>Error 404</h1><p>Page not found.</p></html>";
                    http_result.Body = Encoding.ASCII.GetBytes(httperror);
                } else {
                    http_result.Body = body_data;
                }
                
                FileStream fs = File.Create(http_result.FileName + "." + http_result.ContentExtension);
                fs.Write(http_result.Body, 0, http_result.Body.Length);
                fs.Close();
            } catch (ArgumentException e) {
                Console.WriteLine(e.Message);
            } catch (SocketException e) {
                Console.WriteLine(e.Message);
            }
        }
    }
}
