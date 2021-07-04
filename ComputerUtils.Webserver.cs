using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ComputerUtils.Webserver
{
    public class HttpServer
    {
        public List<Route> routes = new List<Route>();
        public Func<ServerRequest, bool> accessCheck = new Func<ServerRequest, bool>(s => { return true; });
        public ServerValueObject notFoundPage = new ServerValueObject("404 Not found - The requested item couldn't be found", false, "text/plain", 404);
        public ServerValueObject accessDeniedPage = new ServerValueObject("403 Access denied - You do not have access to view this item", false, "text/plain", 403);
        public void StartServer(int port, bool onlyLocal = true)
        {
            StartServer(new int[] { port }, onlyLocal);
        }

        public void StartServer(int[] ports, bool onlyLocal = true)
        {
            Logger.displayLogInConsole = true;
            HttpListener listener = new HttpListener();
            String hostName = Dns.GetHostName();
            Logger.Log("Host name: " + hostName);
            IPHostEntry host = Dns.GetHostEntry(hostName);
            foreach(int port in ports)
            {
                listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
                foreach (IPAddress ip in host.AddressList)
                {
                    if (onlyLocal && ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                    Logger.Log("Added IP " + ip.ToString() + " to Prefixes");
                    listener.Prefixes.Add("http://" + ip.ToString() + ":" + port + "/");
                }
            }
            
            listener.Start();
            Logger.Log("Server started");
            while(true)
            {
                try
                {
                    ServerRequest request = new ServerRequest(listener.GetContextAsync().Result, this);
                    if (!accessCheck(request))
                    {
                        if (!request.closed) request.Send403();
                        continue;
                    }
                    for(int i = 0; i < routes.Count; i++)
                    {
                        if (routes[i].UseRoute(request)) break;
                    }
                    if(!request.closed) request.Send404();
                } catch (Exception e)
                {
                    Logger.Log("An error occured while handling a request:\n" + e.ToString(), LoggingType.Error);
                }
            }
        }

        public void AddRoute(string method, string path, Func<ServerRequest, bool> action, bool onlyCheckBeginning = false, bool ignoreCase = true, bool ignoreEnd = true)
        {
            routes.Add(new Route(method, path, action, onlyCheckBeginning, ignoreCase, ignoreEnd));
        }

        public void AddRouteFile(string path, string filePath, bool ignoreCase = true, bool ignoreEnd = true)
        {
            string contentType = GetContentTpe(filePath);
            AddRoute("GET", path, new Func<ServerRequest, bool>(ServerRequest =>
            {
                if (File.Exists(filePath)) ServerRequest.SendData(File.ReadAllBytes(filePath), contentType);
                else ServerRequest.Send404();
                return true;
            }), false, ignoreCase, ignoreEnd);
        }

        public void AddRouteFolderWithFiles(string path, string folderPath, bool ignoreCase = true, bool ignoreEnd = true)
        {
            if (!folderPath.EndsWith("\\") && folderPath.Length > 0) folderPath += "\\";
            AddRoute("GET", path, new Func<ServerRequest, bool>(ServerRequest =>
            {
                string file = folderPath + ServerRequest.path.Substring(path.Length + 1).Replace("/", "\\");
                if (File.Exists(file)) ServerRequest.SendData(File.ReadAllBytes(file), GetContentTpe(file));
                else ServerRequest.Send404();
                return true;
            }), true, ignoreCase, ignoreEnd);
        }

        public void SetAccessCheck(Func<ServerRequest, bool> check)
        {
            accessCheck = check;
        }

        public void Set404PageFile(string fileName)
        {
            if (!File.Exists(fileName)) return;
            notFoundPage = new ServerValueObject(fileName, true, "", 404);
        }

        public void Set403PageFile(string fileName)
        {
            if (!File.Exists(fileName)) return;
            accessDeniedPage = new ServerValueObject(fileName, true, "", 403);
        }

        public void Set404PageString(string content)
        {
            notFoundPage = new ServerValueObject(content, false, "", 404);
        }

        public void Set403PageString(string content)
        {
            accessDeniedPage = new ServerValueObject(content, false, "", 403);
        }

        public static string GetContentTpe(String path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".jpg":
                    return "image/jpeg";
                case ".mp4":
                    return "video/mp4";
                case ".js":
                    return "application/javascript";
                case ".html":
                    return "text/html";
                case ".json":
                    return "application/json";
                case ".tiff":
                    return "image/tiff";
                case ".webm":
                    return "video/webm";
                case ".css":
                    return "text/css";
                case ".mp3":
                    return "audio/mpeg";
            }
            return "text/plain";
        }
    }

    public class ServerValueObject
    {
        public string value { get; set; } = "";
        public bool isFile { get; set; } = false;
        public string contentType { get; set; } = "text/html";
        public int status { get; set; } = 200;
        public Encoding encoding { get; set; } = Encoding.UTF8;

        public ServerValueObject(string value, bool isFile, string contentType = "", int status = 200)
        {
            this.value = value;
            this.isFile = isFile;
            if (isFile && contentType == "") contentType = HttpServer.GetContentTpe(value);
            else if(contentType != "") this.contentType = contentType;
            this.status = status;
        }

        public void DoRequest(ServerRequest serverRequest)
        {
            if(isFile)
            {
                if (File.Exists(value)) serverRequest.SendData(File.ReadAllBytes(value), contentType, encoding, status, true);
                else serverRequest.Send404();
            } else
            {
                serverRequest.SendString(value, contentType, status);
            }
        }
    }

    public class Route
    {
        public string method { get; set; } = "GET";
        public string path { get; set; } = "/";
        public bool onlyCheckBeginning { get; set; } = false;
        public bool ignoreCase { get; set; } = true;
        public bool ignoreEnd { get; set; } = true;
        public Func<ServerRequest, bool> action { get; set; } = null;

        public Route(string method, string path, Func<ServerRequest, bool> action, bool onlyCheckBeginning, bool ignoreCase, bool ignoreEnd)
        {
            this.method = method;
            this.path = path;
            this.action = action;
            this.onlyCheckBeginning = onlyCheckBeginning;
            this.ignoreCase = ignoreCase;
            this.ignoreEnd = ignoreEnd;
        }

        public bool UseRoute(ServerRequest request)
        {
            string pathTmp = this.path;
            string requestPathTmp = request.path;
            if(ignoreCase)
            {
                pathTmp = pathTmp.ToLower();
                requestPathTmp = requestPathTmp.ToLower();
            }
            if(ignoreEnd)
            {
                pathTmp = pathTmp.Trim(new char[] { '/' });
                requestPathTmp = requestPathTmp.Trim(new char[] { '/' });
            }
            if((requestPathTmp == pathTmp || onlyCheckBeginning && requestPathTmp.StartsWith(pathTmp)) && request.method == this.method)
            {
                return action(request);
            }
            return false;
        }
    }

    public class ServerRequest
    {
        public HttpListenerContext context { get; set; } = null;
        public string path { get; set; } = "/";
        public string method { get; set; } = "GET";
        public HttpServer server { get; set; } = null;
        public bool closed { get; set; } = false;
        public byte[] bodyBytes { get; set; } = new byte[0];
        public string bodyString { get; set; } = "";
        public string requestBodyContentType { get; set; } = "";
        public object customObject { get; set; } = null;

        public ServerRequest(HttpListenerContext context, HttpServer server)
        {
            this.context = context;
            this.path = HttpUtility.UrlDecode(context.Request.Url.AbsolutePath);
            this.method = context.Request.HttpMethod;
            this.server = server;
            if(context.Request.HasEntityBody && context.Request.InputStream != Stream.Null)
            {
                bodyString = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
                bodyBytes = context.Request.ContentEncoding.GetBytes(bodyString);
                this.requestBodyContentType = context.Request.ContentType;
            }
        }

        public void Send404()
        {
            server.notFoundPage.DoRequest(this);
        }

        public void Send403()
        {
            server.accessDeniedPage.DoRequest(this);
        }

        public void SendString(string str, string contentType = "text/plain", int statusCode = 200, bool closeRequest = true)
        {
            SendData(Encoding.UTF8.GetBytes(str), contentType, Encoding.UTF8, statusCode, closeRequest);
        }

        public void SendData(byte[] data, string contentType = "text/html", int statusCode = 200, bool closeRequest = true)
        {
            SendData(data, contentType, Encoding.UTF8, statusCode, closeRequest);
        }

        public void SendData(byte[] data, string contentType, Encoding contentEncoding, int statusCode, bool closeRequest)
        {
            context.Response.ContentType = contentType;
            context.Response.ContentEncoding = contentEncoding;
            context.Response.ContentLength64 = data.LongLength;
            context.Response.StatusCode = statusCode;
            context.Response.OutputStream.WriteAsync(data, 0, data.Length);
            if (closeRequest) Close();
            closed = closeRequest;
        }

        public void Close()
        {
            context.Response.Close();
        }
    }
}