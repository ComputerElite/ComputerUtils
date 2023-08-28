﻿using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using MimeTypes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
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
        public List<WebsocketRoute> wsRoutes = new List<WebsocketRoute>();
        public Func<ServerRequest, bool> accessCheck = new Func<ServerRequest, bool>(s => { return true; });
        public ServerValueObject notFoundPage = new ServerValueObject("404 Not found - The requested item couldn't be found", false, "text/plain", 404);
        public ServerValueObject accessDeniedPage = new ServerValueObject("403 Access denied - You do not have access to view this item", false, "text/plain", 403);
        public Dictionary<string, string> defaultResponseHeaders = new Dictionary<string, string>() {
            { "Access-Control-Allow-Origin", "*" },
            { "charset", "UTF-8" }
        };
        public List<CacheResponse> cache = new List<CacheResponse>();
        public int DefaultCacheValidityInSeconds = 3600;
        public long maxRamUsage = 1073741824; // 1TB
        public int[] ports = new int[0];
        public bool setupHttps = false;
        public bool logRequests = true;
        public string[] otherPrefixes = new string[0];
        public Thread serverThread = null;
        
        public void StartServer(int port, bool setupHttps = false, string[] otherPrefixes = null)
        {
            StartServer(new int[] { port }, setupHttps, otherPrefixes);
        }

        public void StartServer(int[] ports, bool setupHttps = false, string[] otherPrefixes = null)
        {
            Logger.displayLogInConsole = true;
            this.ports = ports;
            this.setupHttps = setupHttps;
            this.otherPrefixes = otherPrefixes == null ? new string[0] : otherPrefixes;
            HttpListener listener = new HttpListener();
            String hostName = Dns.GetHostName();
            Logger.Log("Host name: " + hostName, LoggingType.WebServer);
            foreach(string prefix in GetPrefixes())
            {
                Logger.Log("Server will listen on " + prefix, LoggingType.WebServer);
                try
                {
                    listener.Prefixes.Add(prefix);
                } catch(Exception e)
                {
                    Logger.Log("Actually nvm that. It won't listen on " + prefix + " :\n" + e.ToString(), LoggingType.WebServer);
                }
                
            }
            listener.TimeoutManager.IdleConnection = new TimeSpan(0, 1, 0);
            listener.Start();
			serverThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        HttpListenerContext context = listener.GetContextAsync().Result;
                        ThreadPool.QueueUserWorkItem(HandleRequest, context);
                    }
                    catch (Exception e)
                    {
                        Logger.Log("An error occured while handling a request:\n" + e.ToString(), LoggingType.Error);
                    }
                }
            });
            serverThread.Start();
        }

        public void HandleRequest(object c)
        {
            HttpListenerContext context = (HttpListenerContext)c;
            try
            {
                if (context.Request.IsWebSocketRequest || context.Request.Headers["Sec-WebSocket-Version"] != null)
                {
                    if (logRequests)
                        Logger.Log("Websocket connected from " + (context.Request.Headers["X-Forwarded-For"] ??
                                                                  context.Request.RemoteEndPoint.Address
                                                                      .ToString()), LoggingType.WebServer);
                    context.Request.Headers["Upgrade"] = "websocket";
                    context.Request.Headers["Connection"] = "Upgrade";
                    string uRL = HttpUtility.UrlDecode(context.Request.Url.AbsolutePath);
                    bool found = false;
                    for (int i = 0; i < wsRoutes.Count; i++)
                    {
                        if (wsRoutes[i].UseRoute(uRL))
                        {
                            SocketHandler handler = new SocketHandler(context, this, wsRoutes[i]);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                }
                else
                {
                    ServerRequest request = new ServerRequest(context, this);
                    if (logRequests) Logger.Log(request.ToString(), LoggingType.WebServer);
                    if (!accessCheck(request))
                    {
                        if (!request.closed) request.Send403();
                        return;
                    }

                    for (int i = 0; i < routes.Count; i++)
                    {
                        if (routes[i].UseRoute(request)) break;
                    }

                    if (!request.closed) request.Send404();
                    request.Dispose();
                }
            }
            catch (Exception e)
            {
                context.Response.Close();
                Logger.Log("An error occured while handling a request:\n" + e, LoggingType.Error);
            }
        }

        private DateTime lastFullClean = DateTime.MinValue;
        private TimeSpan timeBetweenFullCleans = new TimeSpan(0, 0, 30);
        public CacheResponse GetCacheResponse(ServerRequest request)
        {
            DateTime now = DateTime.Now;
            CacheResponse r = null;
            bool match = false;
            for (int i = 0; i < cache.Count; i++)
            {
                if (cache[i] == null)
                {
                    cache.RemoveAt(i);
                    i--;
                    continue;
                }
                if (cache[i].origin == request.origin && cache[i].method == request.method && cache[i].path == request.path && cache[i].uA == request.context.Request.UserAgent)
                {
                    if (request.queryString.Count != cache[i].queryStrings.Count) continue;
                    match = true;
                    foreach(string key in request.queryString.Keys)
                    {
                        if (request.queryString[key] != cache[i].queryStrings[key]) match = false;
                    }
                    if(!match) continue;
                    if (DateTime.Now - lastFullClean >= timeBetweenFullCleans)
                    {
                        // Clean cache, aka finish looping through all entries, remove all invalid ones
                        r = cache[i];
                    }
                    else return cache[i];
                }
                if(cache[i].validilityTime < now)
                {
                    cache.Remove(cache[i]);
                }
            }

            if (r != null) lastFullClean = DateTime.Now;
            return r;
        }

        public Process currentProcess = Process.GetCurrentProcess();
        public void AddCacheResponse(ServerRequest request, int cacheValidityInSeconds)
        {
            CacheResponse res = new CacheResponse();
            res.path = request.path;
            res.method = request.method;
            res.queryStrings = request.queryString;
            res.uA = request.context.Request.UserAgent;
            res.details= request.serverRequestDetails;
            res.origin = request.origin;
            res.validilityTime = DateTime.Now.AddSeconds(cacheValidityInSeconds == 0 ? DefaultCacheValidityInSeconds : cacheValidityInSeconds);
            cache.Add(res);

            // remove cache response if too much ram
            while (currentProcess.WorkingSet64 >= maxRamUsage && cache.Count > 0)
            {
                cache.RemoveAt(0);
            }
        }

        public void RemoveCacheResponse(CacheResponse res)
        {
            for(int i = 0; i < cache.Count; i++)
            {
                if (cache[i].method == res.method && cache[i].path == res.path)
                {
                    cache.RemoveAt(i);
                    break;
                }
            }
        }

        public List<string> GetPrefixes()
        {
            List<string> prefixes = new List<string>();
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (int port in ports)
            {
                prefixes.Add("http://*:" + port + "/");
            }
            if (otherPrefixes != null)
            {
                foreach (string p in otherPrefixes)
                {
                    //prefixes.Add(p + "/");
                    foreach (int port in ports)
                    {
                        prefixes.Add(p + ":" + port + "/");
                    }
                }
            }
            return prefixes;
        }

        public void SetDefaultResponseHeaders(Dictionary<string, string> headers)
        {
            defaultResponseHeaders = headers;
        }

        public void AddRoute(string method, string path, Func<ServerRequest, bool> action, bool onlyCheckBeginning = false, bool ignoreCase = true, bool ignoreEnd = true, bool cache = false, int cacheValidityInSeconds = 0, bool clientCache = false, int clientCacheValidityInSeconds = 0)
        {
            routes.Add(new Route(method, path, action, onlyCheckBeginning, ignoreCase, ignoreEnd, cache, cacheValidityInSeconds, clientCache, clientCacheValidityInSeconds));
        }

        public void AddRouteRedirect(string method, string path, string target, bool onlyCheckBeginning = false, bool ignoreCase = true, bool ignoreEnd = true)
        {
            routes.Add(new Route(method, path, new Func<ServerRequest, bool>(request =>
            {
                string queryString = "?";
                foreach(string n in request.queryString.AllKeys)
                {
                    queryString += n + "=" + request.queryString[n] + "&";
                }
                if(queryString.EndsWith("&")) queryString = queryString.Substring(0, queryString.Length - 1);
                if(!target.EndsWith("/")) target += "/";
                request.Redirect(target + request.pathDiff + queryString);
                return true;
            }), onlyCheckBeginning, ignoreCase, ignoreEnd, false, 0, false, 0));
        }

        public void RemoveRoute(string method, string path)
        {
            Route match = routes.FirstOrDefault(x => x.method == method && x.path == path);
            if (match != null) routes.Remove(match);
        }

        public void AddRouteFile(string path, string filePath, bool ignoreCase = true, bool ignoreEnd = true, bool cache = false)
        {
            string contentType = GetContentTpe(filePath);
            AddRoute("GET", path, new Func<ServerRequest, bool>(ServerRequest =>
            {
                ServerRequest.SendFile(filePath);
                return true;
            }), false, ignoreCase, ignoreEnd, cache, 0, true);
        }

        public void AddRouteFile(string path, string filePath, Dictionary<string, string> dictionary)
        {
            AddRouteFile(path, filePath, dictionary, true, true, false, null);
        }

        public void AddRouteFile(string path, string filePath, Dictionary<string, string> replace, bool ignoreCase, bool ignoreEnd = true, bool cache = false, int cacheValidityInSeconds = 0, bool clientCache = false, int clientCacheValidityInSeconds = 0)
        {
            string contentType = GetContentTpe(filePath);
            AddRoute("GET", path, new Func<ServerRequest, bool>(ServerRequest =>
            {
                ServerRequest.SendFile(filePath, replace);
                return true;
            }), false, ignoreCase, ignoreEnd, cache, cacheValidityInSeconds, clientCache, clientCacheValidityInSeconds);
        }

        public void AddRouteFile(string path, string filePath, Dictionary<string, string> replace, bool ignoreCase = true, bool ignoreEnd = true, bool cache = false, Func<ServerRequest, bool> accessCheck = null)
        {
            if (accessCheck == null)
            {
                AddRouteFile(path, filePath, replace, ignoreCase, ignoreEnd, cache, 0, cache, 0);
                return;
            }
            string contentType = GetContentTpe(filePath);
            AddRoute("GET", path, new Func<ServerRequest, bool>(ServerRequest =>
            {
                if (!accessCheck.Invoke(ServerRequest)) return true;
                ServerRequest.SendFile(filePath, replace);
                return true;
            }), false, ignoreCase, ignoreEnd, false, 0, false);
        }

        public void AddRouteFile(string path, string filePath, bool ignoreCase = true, bool ignoreEnd = true, bool cache = false, Func<ServerRequest, bool> accessCheck = null)
        {
            AddRouteFile(path, filePath, null, ignoreCase, ignoreEnd, cache, accessCheck);
        }

        public void AddRouteFolderWithFiles(string path, string folderPath, bool ignoreCase = true, bool ignoreEnd = true, bool cache = false)
        {
            if (!folderPath.EndsWith(Path.DirectorySeparatorChar) && folderPath.Length > 0) folderPath += Path.DirectorySeparatorChar;
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
            AddRoute("GET", path, new Func<ServerRequest, bool>(ServerRequest =>
            {
                string file = folderPath + ServerRequest.path.Substring(path.Length + 1).Replace('/', Path.DirectorySeparatorChar);
                if (File.Exists(file)) ServerRequest.SendFile(file);
                else ServerRequest.Send404();
                return true;
            }), true, ignoreCase, ignoreEnd, cache);
        }

        public void AddWSRoute(string path, Action<SocketServerRequest> action, bool onlyCheckBeginning = false, bool ignoreCase = true, bool ignoreEnd = true)
        {
            wsRoutes.Add(new WebsocketRoute(path, action, onlyCheckBeginning, ignoreCase, ignoreEnd));
        }

        public void RemoveWSRoute(string method, string path)
        {
            Route match = routes.FirstOrDefault(x => x.method == method && x.path == path);
            if (match != null) routes.Remove(match);
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

        public static string GetContentTpe(string path)
        {
            return MimeTypeMap.GetMimeType(Path.GetExtension(path).ToLower());
        }

        public static string GetFileExtension(string mimeType)
        {
            return MimeTypeMap.GetExtension(mimeType.ToLower());
        }
    }


    public class ServerValueObject : IDisposable
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

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class CacheResponse
    {
        public string path = "";
        public string method = "GET";
        public string origin = "";
        public NameValueCollection queryStrings = new NameValueCollection();
        public ServerRequestDetails details = new ServerRequestDetails();
        public DateTime validilityTime = DateTime.MinValue;
        internal string uA;

        public bool UseRouteCache(ServerRequest request, Func<ServerRequest, bool> action, int cacheValidityInSeconds)
        {
            if(validilityTime < DateTime.Now)
            {
                //Logger.Log("Requesting data from action. Cache length: " + request.server.cache.Count, LoggingType.Debug);
                action(request);
                request.server.RemoveCacheResponse(this);
                request.server.AddCacheResponse(request, cacheValidityInSeconds);
            } else
            {
                //Logger.Log("Sending data from cache. Cache length: " + request.server.cache.Count, LoggingType.Debug);
                request.SendData(details.sentData, details.sentContentType, details.sentStatusCode, details.sentCloseRequest, details.sentHeaders);
            }
            
            return true;
        }

        public override string ToString()
        {
            return method + " " + path + " - " + origin + " - " + uA + " - valid until " + (validilityTime);
        }
    }

    public class Route : IDisposable
    {
        public string method { get; set; } = "GET";
        public string path { get; set; } = "/";
        public bool onlyCheckBeginning { get; set; } = false;
        public bool ignoreCase { get; set; } = true;
        public bool ignoreEnd { get; set; } = true;
        public bool cache { get; set; } = false;
        public bool clientCache { get; set; } = false;
        public int cacheValidityInSeconds { get; set; } = 0;
        public int clientCacheValidityInSeconds { get; set; } = 0;
        public Func<ServerRequest, bool> action { get; set; } = null;

        public Route(string method, string path, Func<ServerRequest, bool> action, bool onlyCheckBeginning, bool ignoreCase, bool ignoreEnd, bool cache, int cacheValidityInSeconds, bool clientCache, int clientCacheValidityInSeconds)
        {
            this.method = method;
            this.path = path;
            if (!this.path.EndsWith("/")) this.path += "/";
            if (!this.path.StartsWith("/")) this.path = "/" + this.path;
            this.action = action;
            this.onlyCheckBeginning = onlyCheckBeginning;
            this.ignoreCase = ignoreCase;
            this.ignoreEnd = ignoreEnd;
            this.cache = cache;
            this.cacheValidityInSeconds = cacheValidityInSeconds;
            this.clientCache = clientCache;
            this.clientCacheValidityInSeconds = clientCacheValidityInSeconds;
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
                if(request.path.Length >= path.Length) request.pathDiff = request.path.Substring(path.Length);
                return UseRouteWithCache(request);
            }
            return false;
        }

        public bool UseRouteWithCache(ServerRequest request)
        {
            if (clientCache)
            {
                if (clientCacheValidityInSeconds != 0) request.automaticHeaders.Add("Cache-Control", "max-age=" + clientCacheValidityInSeconds);
                else if (cacheValidityInSeconds != 0) request.automaticHeaders.Add("Cache-Control", "max-age=" + cacheValidityInSeconds);
                else request.automaticHeaders.Add("Cache-Control", "max-age=" + request.server.DefaultCacheValidityInSeconds);
            }
            if (!cache || request.server.DefaultCacheValidityInSeconds == 0) return action(request);
            CacheResponse c = request.server.GetCacheResponse(request);
            if(c == null)
            {
                c = new CacheResponse();
            }
            c.UseRouteCache(request, action, cacheValidityInSeconds);
            return true;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class WebsocketRoute : IDisposable
    {
        public string path { get; set; } = "/";
        public bool onlyCheckBeginning { get; set; } = false;
        public bool ignoreCase { get; set; } = true;
        public bool ignoreEnd { get; set; } = true;
        public Action<SocketServerRequest> action { get; set; } = null;

        public WebsocketRoute(string path, Action<SocketServerRequest> action, bool onlyCheckBeginning, bool ignoreCase, bool ignoreEnd)
        {
            this.path = path;
            if (!this.path.EndsWith("/")) this.path += "/";
            if (!this.path.StartsWith("/")) this.path = "/" + this.path;
            this.action = action;
            this.onlyCheckBeginning = onlyCheckBeginning;
            this.ignoreCase = ignoreCase;
            this.ignoreEnd = ignoreEnd;
        }

        public bool UseRoute(string path)
        {
            string pathTmp = this.path;
            string requestPathTmp = path;
            if (ignoreCase)
            {
                pathTmp = pathTmp.ToLower();
                requestPathTmp = requestPathTmp.ToLower();
            }
            if (ignoreEnd)
            {
                pathTmp = pathTmp.Trim(new char[] { '/' });
                requestPathTmp = requestPathTmp.Trim(new char[] { '/' });
            }
            if (requestPathTmp == pathTmp || onlyCheckBeginning && requestPathTmp.StartsWith(pathTmp))
            {
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class ServerRequestDetails
    {
        public byte[] sentData { get; set; } = new byte[0];
        public string sentContentType = "";
        public string origin = "";
        public Encoding sentContentEncoding = Encoding.UTF8;
        public int sentStatusCode = 200;
        public bool sentCloseRequest = true;
        public Dictionary<string, string> sentHeaders = null;
    }

    public class ServerRequest : IDisposable
    {
        public HttpListenerContext context { get; set; } = null;
        public string path { get; set; } = "/";
        public string pathDiff { get; set; } = "";
        public string origin { get; set; } = "";
        public string method { get; set; } = "GET";
        public HttpServer server { get; set; } = null;
        public bool closed { get; set; } = false;
        public byte[] bodyBytes { get; set; } = new byte[0];
        public string bodyString { get; set; } = "";
        public string requestBodyContentType { get; set; } = "";
        public object customObject { get; set; } = null;
        public string remote { get; set; } = "";
        public CookieCollection cookies { get; set; } = null;
        public NameValueCollection queryString { get; set; } = null;
        public Dictionary<string, string> automaticHeaders = new Dictionary<string, string>();

        public ServerRequestDetails serverRequestDetails { get; set; } = new ServerRequestDetails();

        public ServerRequest(HttpListenerContext context, HttpServer server)
        {
            this.context = context;
            this.remote = context.Request.Headers["X-Forwarded-For"] ?? context.Request.RemoteEndPoint.Address.ToString();
            this.cookies = context.Request.Cookies;
            this.path = HttpUtility.UrlDecode(context.Request.Url.AbsolutePath);
            this.method = context.Request.HttpMethod;
            this.origin = context.Request.Headers.Get("origin");
            this.server = server;
            this.queryString = context.Request.QueryString;
            if(context.Request.HasEntityBody && context.Request.InputStream != Stream.Null)
            {
                byte[] buffer = new byte[16 * 1024];
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = context.Request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    bodyBytes = ms.ToArray();
                }
                this.requestBodyContentType = context.Request.ContentType;
                this.bodyString = context.Request.ContentEncoding.GetString(bodyBytes);
            }
        }

        public override string ToString()
        {
            return method + " " + path + " from " + remote + " with body: " + (bodyString.Length > 500 ? bodyString.Substring(0, 500) + " [...]" : bodyString);
        }

        public void Send404()
        {
            if (!closed) server.notFoundPage.DoRequest(this);
        }

        public void Send403()
        {
            if(!closed) server.accessDeniedPage.DoRequest(this);
        }

        public void SendString(string str, string contentType = "text/plain", int statusCode = 200, bool closeRequest = true, Dictionary<string, string> headers = null)
        {
            SendData(Encoding.UTF8.GetBytes(str), contentType, Encoding.UTF8, statusCode, closeRequest, headers);
        }

        public void SendStringReplace(string str, string contentType = "text/plain", int statusCode = 200, Dictionary<string, string> replace = null, bool closeRequest = true, Dictionary<string, string> headers = null)
        {
            if(replace != null)
            {
                foreach(KeyValuePair<string, string> pair in replace)
                {
                    str = str.Replace(pair.Key, pair.Value);
                }
            }
            SendData(Encoding.UTF8.GetBytes(str), contentType, Encoding.UTF8, statusCode, closeRequest, headers);
        }

        public void SendFile(string file, string contentType = "", int statusCode = 200, bool closeRequest = true, Dictionary<string, string> headers = null)
        {
            if(!File.Exists(file))
            {
                Send404();
                return;
            }
            SendData(File.ReadAllBytes(file), contentType == "" ? HttpServer.GetContentTpe(file) : contentType, Encoding.UTF8, statusCode, closeRequest, headers);
        }

        public void SendFile(string file, Dictionary<string, string> replace, string contentType = "", int statusCode = 200, bool closeRequest = true, Dictionary<string, string> headers = null)
        {
            if (replace == null)
            {
                SendFile(file, contentType, statusCode, closeRequest, headers);
                return;
            }
            if (!File.Exists(file))
            {
                Send404();
                return;
            }
            string toSend = File.ReadAllText(file);
            foreach (KeyValuePair<string, string> key in replace) toSend = toSend.Replace(key.Key, key.Value);
            SendString(toSend, contentType == "" ? HttpServer.GetContentTpe(file) : contentType, statusCode, closeRequest, headers);
        }

        public void Redirect(string target)
        {
            context.Response.Redirect(target);
            Close();
            if (server.logRequests) Logger.Log("    Redirecting " + (context.Request.Headers["X-Forwarded-For"] ?? context.Request.RemoteEndPoint.Address.ToString()) + " to " + target + " from " + path, LoggingType.WebServer);
        }

        public void SendData(byte[] data, string contentType = "text/html", int statusCode = 200, bool closeRequest = true, Dictionary<string, string> headers = null)
        {
            SendData(data, contentType, Encoding.UTF8, statusCode, closeRequest, headers);
        }

        public void SendData(byte[] data, string contentType, Encoding contentEncoding, int statusCode, bool closeRequest, Dictionary<string, string> headers = null)
        {
            if(contentType != "") context.Response.ContentType = contentType;
            context.Response.ContentEncoding = contentEncoding;
            context.Response.ContentLength64 = data.LongLength;
            context.Response.StatusCode = statusCode;
            if(server.defaultResponseHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in server.defaultResponseHeaders) context.Response.Headers[header.Key] = header.Value;
            }
            if (automaticHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in automaticHeaders) context.Response.Headers[header.Key] = header.Value;
            }
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers) context.Response.Headers[header.Key] = header.Value;
            }
            if (server.logRequests) Logger.Log("    Sending " + data.LongLength + " bytes of data to " + (context.Request.Headers["X-Forwarded-For"] ?? context.Request.RemoteEndPoint.Address.ToString()) + " from " + path, LoggingType.WebServer);
            context.Response.OutputStream.Write(data, 0, data.Length);
            serverRequestDetails.sentData = data;
            serverRequestDetails.sentContentType = contentType;
            serverRequestDetails.sentContentEncoding = contentEncoding;
            serverRequestDetails.sentStatusCode = statusCode;
            serverRequestDetails.sentCloseRequest = closeRequest;
            serverRequestDetails.sentHeaders = headers;
            serverRequestDetails.origin = origin;
            if (closeRequest) Close();
            closed = closeRequest;
        }

        public void Close()
        {
            closed = true;
            context.Response.Close();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class SocketHandler : IDisposable
    {
        public HttpListenerContext context { get; set; } = null;
        public string path { get; set; } = "/";
        public string pathDiff { get; set; } = "/";
        public HttpServer server { get; set; } = null;
        public bool closed { get; set; } = false;
        public object customObject { get; set; } = null;
        public WebSocket socket { get; set; } = null;
        public WebsocketRoute route { get; set; } = null;

        public SocketHandler(HttpListenerContext context, HttpServer server, WebsocketRoute route)
        {
            this.context = context;
            this.path = HttpUtility.UrlDecode(context.Request.Url.AbsolutePath);
            this.server = server;
            this.route = route;
            if(path.Length >= route.path.Length) pathDiff = path.Substring(route.path.Length);
            try
            {
                socket = context.AcceptWebSocketAsync(null).Result.WebSocket;
            }
            catch (Exception e)
            {
                Logger.Log("Websocket failed to get accepted:\n" + e.ToString(), LoggingType.WebServer);
                context.Response.StatusCode = 500;
                context.Response.Close();
                return;
            }
            Thread t = new Thread(() =>
            {
                while (!closed)
                {
                    byte[] buffer = new byte[4096];
                    if (socket.State != WebSocketState.Open)
                    {
                        closed = true;
                        Dispose();
                        return;
                    }
                    WebSocketReceiveResult result;
                    try
                    {
                        result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
                    } catch (Exception e)
                    {
                        Logger.Log("Unable to recieve Websocket. Terminating connection:" + e.ToString(), LoggingType.Warning);
                        break;
                    }
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (server.logRequests) Logger.Log("Websocket closed by client: " + (context.Request.Headers["X-Forwarded-For"] ?? context.Request.RemoteEndPoint.Address.ToString()), LoggingType.WebServer);
                        closed = true;
                        socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    } else
                    {
                        buffer = buffer.TakeWhile((v, index) => buffer.Skip(index).Any(w => w != 0x00)).ToArray();
                        SocketServerRequest socketRequest = new SocketServerRequest(context, server, this, result, buffer, pathDiff);
                        if (server.logRequests) Logger.Log("Websocket from " + (context.Request.Headers["X-Forwarded-For"] ?? context.Request.RemoteEndPoint.Address.ToString()) + " sent " + (socketRequest.bodyString.Length > 500 ? socketRequest.bodyString.Substring(0, 500) + " [...]" : socketRequest.bodyString), LoggingType.WebServer);
                        route.action(socketRequest);
                    }
                }
                Dispose();
            });
            t.Start();
        }

        public void CloseRequest()
        {
            closed = true;
            if (server.logRequests) Logger.Log("Websocket closed by server from " + (context.Request.Headers["X-Forwarded-For"] ?? context.Request.RemoteEndPoint.Address.ToString()), LoggingType.WebServer);
            socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class SocketServerRequest : IDisposable
    {
        public HttpListenerContext context { get; set; } = null;
        public string path { get; set; } = "/";
        public string pathDiff { get; set; } = "";
        public HttpServer server { get; set; } = null;
        public byte[] bodyBytes { get; set; } = new byte[0];
        public string bodyString { get; set; } = "";
        public object customObject { get; set; } = null;
        public SocketHandler handler { get; set; } = null;
        public NameValueCollection queryString { get; set; } = null;
        public WebSocketReceiveResult receiveResult { get; set; } = null;

        public SocketServerRequest(HttpListenerContext context, HttpServer server, SocketHandler handler, WebSocketReceiveResult receiveResult, byte[] bytes, string pathDiff)
        {
            this.queryString = context.Request.QueryString;
            this.context = context;
            this.path = HttpUtility.UrlDecode(context.Request.Url.AbsolutePath);
            this.pathDiff = pathDiff;
            this.server = server;
            this.handler = handler;
            this.bodyString = Encoding.UTF8.GetString(bytes);
            this.bodyBytes = bytes;
            this.receiveResult = receiveResult;
        }

        public void SendString(string str, WebSocketMessageType msgType = WebSocketMessageType.Text, bool closeRequest = false)
        {
            SendData(Encoding.UTF8.GetBytes(str), msgType, closeRequest);
        }

        public void SendData(byte[] data, WebSocketMessageType msgType = WebSocketMessageType.Binary, bool closeRequest = false)
        {
            if (handler.socket.CloseStatus.HasValue)
            {
                handler.closed = true;
                handler.Dispose();
                return;
            }
            if (server.logRequests) Logger.Log("    Sending " + data.LongLength + " bytes of data to " + (context.Request.Headers["X-Forwarded-For"] ?? context.Request.RemoteEndPoint.Address.ToString()) + " via websocket at " + path, LoggingType.WebServer);
            handler.socket.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, receiveResult.EndOfMessage, CancellationToken.None);
            if (closeRequest) Close();
        }

        public void Close()
        {
            handler.CloseRequest();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}