// CRAB Standard Library - System.Web.cs
// HTTP client/server and web functionality

using System.IO;
using System.Text;

namespace System.Net
{
    // ===== HTTP WEB REQUEST =====
    
    public class HttpWebRequest
    {
        private string uri;
        private string method;
        private Dictionary<string, string> headers;
        private byte[] requestBody;
        
        internal HttpWebRequest(string uri)
        {
            this.uri = uri;
            method = "GET";
            headers = new Dictionary<string, string>();
            requestBody = null;
        }
        
        public string Method
        {
            get { return method; }
            set { method = value; }
        }
        
        public string ContentType
        {
            get { return headers.ContainsKey("Content-Type") ? headers["Content-Type"] : ""; }
            set { headers["Content-Type"] = value; }
        }
        
        public long ContentLength
        {
            get { return requestBody != null ? requestBody.Length : 0; }
            set { /* Set by writing to request stream */ }
        }
        
        public WebHeaderCollection Headers
        {
            get { return new WebHeaderCollection(headers); }
        }
        
        public Stream GetRequestStream()
        {
            MemoryStream stream = new MemoryStream();
            return stream;
        }
        
        public HttpWebResponse GetResponse()
        {
            // Will use WASM fetch API or HTTP imports
            return new HttpWebResponse();
        }
    }
    
    // ===== HTTP WEB RESPONSE =====
    
    public class HttpWebResponse : IDisposable
    {
        private HttpStatusCode statusCode;
        private string statusDescription;
        private Dictionary<string, string> headers;
        private byte[] responseBody;
        
        internal HttpWebResponse()
        {
            statusCode = HttpStatusCode.OK;
            statusDescription = "OK";
            headers = new Dictionary<string, string>();
            responseBody = new byte[0];
        }
        
        public HttpStatusCode StatusCode
        {
            get { return statusCode; }
        }
        
        public string StatusDescription
        {
            get { return statusDescription; }
        }
        
        public long ContentLength
        {
            get { return responseBody.Length; }
        }
        
        public string ContentType
        {
            get { return headers.ContainsKey("Content-Type") ? headers["Content-Type"] : ""; }
        }
        
        public WebHeaderCollection Headers
        {
            get { return new WebHeaderCollection(headers); }
        }
        
        public Stream GetResponseStream()
        {
            return new MemoryStream(responseBody);
        }
        
        public void Close()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            // Cleanup
        }
    }
    
    public enum HttpStatusCode
    {
        Continue = 100,
        SwitchingProtocols = 101,
        OK = 200,
        Created = 201,
        Accepted = 202,
        NoContent = 204,
        MovedPermanently = 301,
        Found = 302,
        SeeOther = 303,
        NotModified = 304,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503
    }
    
    // ===== WEB REQUEST =====
    
    public abstract class WebRequest
    {
        public abstract string Method { get; set; }
        public abstract WebHeaderCollection Headers { get; }
        public abstract Stream GetRequestStream();
        public abstract WebResponse GetResponse();
        
        public static WebRequest Create(string requestUriString)
        {
            if (requestUriString == null)
                throw new ArgumentNullException("requestUriString");
            
            if (requestUriString.StartsWith("http://") || requestUriString.StartsWith("https://"))
                return new HttpWebRequest(requestUriString);
            
            throw new NotSupportedException("Only HTTP and HTTPS protocols are supported");
        }
        
        public static WebRequest Create(Uri requestUri)
        {
            if (requestUri == null)
                throw new ArgumentNullException("requestUri");
            
            return Create(requestUri.ToString());
        }
    }
    
    public abstract class WebResponse : IDisposable
    {
        public abstract long ContentLength { get; }
        public abstract string ContentType { get; }
        public abstract WebHeaderCollection Headers { get; }
        public abstract Stream GetResponseStream();
        
        public virtual void Close()
        {
            Dispose();
        }
        
        public abstract void Dispose();
    }
    
    // ===== WEB HEADERS =====
    
    public class WebHeaderCollection
    {
        private Dictionary<string, string> headers;
        
        public WebHeaderCollection()
        {
            headers = new Dictionary<string, string>();
        }
        
        internal WebHeaderCollection(Dictionary<string, string> headers)
        {
            this.headers = headers;
        }
        
        public string this[string name]
        {
            get
            {
                if (name == null)
                    throw new ArgumentNullException("name");
                return headers.ContainsKey(name) ? headers[name] : null;
            }
            set
            {
                if (name == null)
                    throw new ArgumentNullException("name");
                headers[name] = value;
            }
        }
        
        public void Add(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (value == null)
                throw new ArgumentNullException("value");
            
            headers[name] = value;
        }
        
        public void Remove(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            headers.Remove(name);
        }
        
        public void Clear()
        {
            headers.Clear();
        }
        
        public string[] AllKeys
        {
            get 
            { 
                var keys = new string[headers.Count];
                int i = 0;
                foreach (var key in headers.Keys)
                {
                    keys[i++] = key;
                }
                return keys;
            }
        }
        
        public int Count
        {
            get { return headers.Count; }
        }
    }
    
    // ===== WEB CLIENT =====
    
    public class WebClient : IDisposable
    {
        private WebHeaderCollection headers;
        private string baseAddress;
        
        public WebClient()
        {
            headers = new WebHeaderCollection();
            baseAddress = "";
        }
        
        public string BaseAddress
        {
            get { return baseAddress; }
            set { baseAddress = value ?? ""; }
        }
        
        public WebHeaderCollection Headers
        {
            get { return headers; }
            set { headers = value ?? new WebHeaderCollection(); }
        }
        
        public byte[] DownloadData(string address)
        {
            if (address == null)
                throw new ArgumentNullException("address");
            
            string fullAddress = CombineUri(baseAddress, address);
            WebRequest request = WebRequest.Create(fullAddress);
            request.Method = "GET";
            
            // Copy headers - cast is safe for HTTP/HTTPS URLs
            if (request is HttpWebRequest httpRequest)
            {
                foreach (string key in headers.AllKeys)
                {
                    httpRequest.Headers[key] = headers[key];
                }
            }
            
            using (WebResponse response = request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
        
        public string DownloadString(string address)
        {
            byte[] data = DownloadData(address);
            return Encoding.UTF8.GetString(data);
        }
        
        public void DownloadFile(string address, string fileName)
        {
            byte[] data = DownloadData(address);
            File.WriteAllBytes(fileName, data);
        }
        
        public byte[] UploadData(string address, byte[] data)
        {
            return UploadData(address, "POST", data);
        }
        
        public byte[] UploadData(string address, string method, byte[] data)
        {
            if (address == null)
                throw new ArgumentNullException("address");
            if (data == null)
                throw new ArgumentNullException("data");
            
            string fullAddress = CombineUri(baseAddress, address);
            WebRequest request = WebRequest.Create(fullAddress);
            request.Method = method ?? "POST";
            
            // Copy headers and set content type - cast is safe for HTTP/HTTPS URLs
            if (request is HttpWebRequest httpRequest)
            {
                httpRequest.ContentType = "application/octet-stream";
                foreach (string key in headers.AllKeys)
                {
                    httpRequest.Headers[key] = headers[key];
                }
            }
            
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            
            using (WebResponse response = request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
        
        public string UploadString(string address, string data)
        {
            return UploadString(address, "POST", data);
        }
        
        public string UploadString(string address, string method, string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] response = UploadData(address, method, bytes);
            return Encoding.UTF8.GetString(response);
        }
        
        public void Dispose()
        {
            // Cleanup
        }
        
        private string CombineUri(string baseUri, string relativeUri)
        {
            if (string.IsNullOrEmpty(baseUri))
                return relativeUri;
            if (string.IsNullOrEmpty(relativeUri))
                return baseUri;
            
            if (baseUri.EndsWith("/"))
                return baseUri + relativeUri;
            
            return baseUri + "/" + relativeUri;
        }
    }
    
    // ===== URI =====
    
    public class Uri
    {
        private string uriString;
        private string scheme;
        private string host;
        private int port;
        private string path;
        private string query;
        
        public Uri(string uriString)
        {
            if (uriString == null)
                throw new ArgumentNullException("uriString");
            
            this.uriString = uriString;
            ParseUri(uriString);
        }
        
        public Uri(Uri baseUri, string relativeUri)
        {
            if (baseUri == null)
                throw new ArgumentNullException("baseUri");
            if (relativeUri == null)
                throw new ArgumentNullException("relativeUri");
            
            // Simplified relative URI resolution
            if (relativeUri.StartsWith("http://") || relativeUri.StartsWith("https://"))
            {
                uriString = relativeUri;
            }
            else
            {
                string baseStr = baseUri.ToString();
                if (!baseStr.EndsWith("/"))
                    baseStr += "/";
                uriString = baseStr + relativeUri;
            }
            
            ParseUri(uriString);
        }
        
        public string Scheme
        {
            get { return scheme; }
        }
        
        public string Host
        {
            get { return host; }
        }
        
        public int Port
        {
            get { return port; }
        }
        
        public string AbsolutePath
        {
            get { return path; }
        }
        
        public string Query
        {
            get { return query; }
        }
        
        public string AbsoluteUri
        {
            get { return uriString; }
        }
        
        public bool IsAbsoluteUri
        {
            get { return !string.IsNullOrEmpty(scheme); }
        }
        
        public override string ToString()
        {
            return uriString;
        }
        
        private void ParseUri(string uri)
        {
            // Simplified URI parsing
            scheme = "";
            host = "";
            port = 80;
            path = "";
            query = "";
            
            int schemeEnd = uri.IndexOf("://");
            if (schemeEnd > 0)
            {
                scheme = uri.Substring(0, schemeEnd);
                uri = uri.Substring(schemeEnd + 3);
                
                if (scheme == "https")
                    port = 443;
            }
            
            int pathStart = uri.IndexOf('/');
            int queryStart = uri.IndexOf('?');
            
            if (pathStart < 0 && queryStart < 0)
            {
                host = uri;
            }
            else if (queryStart >= 0 && (pathStart < 0 || queryStart < pathStart))
            {
                host = uri.Substring(0, queryStart);
                query = uri.Substring(queryStart);
            }
            else
            {
                host = uri.Substring(0, pathStart);
                string remaining = uri.Substring(pathStart);
                
                queryStart = remaining.IndexOf('?');
                if (queryStart >= 0)
                {
                    path = remaining.Substring(0, queryStart);
                    query = remaining.Substring(queryStart);
                }
                else
                {
                    path = remaining;
                }
            }
            
            // Parse port from host
            int portStart = host.IndexOf(':');
            if (portStart > 0)
            {
                string portStr = host.Substring(portStart + 1);
                host = host.Substring(0, portStart);
                int.TryParse(portStr, out port);
            }
        }
    }
    
    // ===== WEB EXCEPTIONS =====
    
    public class WebException : Exception
    {
        private WebExceptionStatus status;
        private WebResponse response;
        
        public WebException() : base("An error occurred while processing the web request")
        {
            status = WebExceptionStatus.UnknownError;
        }
        
        public WebException(string message) : base(message)
        {
            status = WebExceptionStatus.UnknownError;
        }
        
        public WebException(string message, Exception innerException) : base(message, innerException)
        {
            status = WebExceptionStatus.UnknownError;
        }
        
        public WebException(string message, WebExceptionStatus status) : base(message)
        {
            this.status = status;
        }
        
        public WebException(string message, Exception innerException, WebExceptionStatus status, WebResponse response)
            : base(message, innerException)
        {
            this.status = status;
            this.response = response;
        }
        
        public WebExceptionStatus Status
        {
            get { return status; }
        }
        
        public WebResponse Response
        {
            get { return response; }
        }
    }
    
    public enum WebExceptionStatus
    {
        Success = 0,
        NameResolutionFailure = 1,
        ConnectFailure = 2,
        ReceiveFailure = 3,
        SendFailure = 4,
        PipelineFailure = 5,
        RequestCanceled = 6,
        ProtocolError = 7,
        ConnectionClosed = 8,
        TrustFailure = 9,
        SecureChannelFailure = 10,
        ServerProtocolViolation = 11,
        KeepAliveFailure = 12,
        Pending = 13,
        Timeout = 14,
        ProxyNameResolutionFailure = 15,
        UnknownError = 16,
        MessageLengthLimitExceeded = 17,
        CacheEntryNotFound = 18,
        RequestProhibitedByCachePolicy = 19,
        RequestProhibitedByProxy = 20
    }
}

namespace System.Net.Http
{
    // ===== HTTP CLIENT =====
    
    public class HttpClient : IDisposable
    {
        private string baseAddress;
        private Dictionary<string, string> defaultHeaders;
        private TimeSpan timeout;
        
        public HttpClient()
        {
            baseAddress = "";
            defaultHeaders = new Dictionary<string, string>();
            timeout = TimeSpan.FromSeconds(100);
        }
        
        public string BaseAddress
        {
            get { return baseAddress; }
            set { baseAddress = value ?? ""; }
        }
        
        public TimeSpan Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }
        
        public HttpResponseMessage Get(string requestUri)
        {
            return Send(new HttpRequestMessage("GET", requestUri));
        }
        
        public HttpResponseMessage Post(string requestUri, HttpContent content)
        {
            HttpRequestMessage request = new HttpRequestMessage("POST", requestUri);
            request.Content = content;
            return Send(request);
        }
        
        public HttpResponseMessage Put(string requestUri, HttpContent content)
        {
            HttpRequestMessage request = new HttpRequestMessage("PUT", requestUri);
            request.Content = content;
            return Send(request);
        }
        
        public HttpResponseMessage Delete(string requestUri)
        {
            return Send(new HttpRequestMessage("DELETE", requestUri));
        }
        
        public HttpResponseMessage Send(HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            
            // Will use WASM fetch API
            return new HttpResponseMessage();
        }
        
        public void Dispose()
        {
            // Cleanup
        }
    }
    
    // ===== HTTP REQUEST MESSAGE =====
    
    public class HttpRequestMessage : IDisposable
    {
        private string method;
        private string requestUri;
        private HttpContent content;
        private Dictionary<string, string> headers;
        
        public HttpRequestMessage()
        {
            method = "GET";
            headers = new Dictionary<string, string>();
        }
        
        public HttpRequestMessage(string method, string requestUri)
        {
            this.method = method ?? "GET";
            this.requestUri = requestUri;
            headers = new Dictionary<string, string>();
        }
        
        public string Method
        {
            get { return method; }
            set { method = value; }
        }
        
        public string RequestUri
        {
            get { return requestUri; }
            set { requestUri = value; }
        }
        
        public HttpContent Content
        {
            get { return content; }
            set { content = value; }
        }
        
        public Dictionary<string, string> Headers
        {
            get { return headers; }
        }
        
        public void Dispose()
        {
            if (content != null)
                content.Dispose();
        }
    }
    
    // ===== HTTP RESPONSE MESSAGE =====
    
    public class HttpResponseMessage : IDisposable
    {
        private HttpStatusCode statusCode;
        private string reasonPhrase;
        private HttpContent content;
        private Dictionary<string, string> headers;
        
        public HttpResponseMessage()
        {
            statusCode = HttpStatusCode.OK;
            reasonPhrase = "OK";
            headers = new Dictionary<string, string>();
        }
        
        public HttpStatusCode StatusCode
        {
            get { return statusCode; }
            set { statusCode = value; }
        }
        
        public string ReasonPhrase
        {
            get { return reasonPhrase; }
            set { reasonPhrase = value; }
        }
        
        public HttpContent Content
        {
            get { return content; }
            set { content = value; }
        }
        
        public Dictionary<string, string> Headers
        {
            get { return headers; }
        }
        
        public bool IsSuccessStatusCode
        {
            get
            {
                int code = (int)statusCode;
                return code >= 200 && code <= 299;
            }
        }
        
        public HttpResponseMessage EnsureSuccessStatusCode()
        {
            if (!IsSuccessStatusCode)
                throw new HttpRequestException("Response status code does not indicate success: " + (int)statusCode);
            return this;
        }
        
        public void Dispose()
        {
            if (content != null)
                content.Dispose();
        }
    }
    
    // ===== HTTP CONTENT =====
    
    public abstract class HttpContent : IDisposable
    {
        protected Dictionary<string, string> headers;
        
        protected HttpContent()
        {
            headers = new Dictionary<string, string>();
        }
        
        public Dictionary<string, string> Headers
        {
            get { return headers; }
        }
        
        public abstract byte[] ReadAsByteArray();
        public abstract string ReadAsString();
        
        public virtual void Dispose()
        {
        }
    }
    
    public class StringContent : HttpContent
    {
        private string content;
        private Encoding encoding;
        
        public StringContent(string content)
            : this(content, Encoding.UTF8, "text/plain")
        {
        }
        
        public StringContent(string content, Encoding encoding)
            : this(content, encoding, "text/plain")
        {
        }
        
        public StringContent(string content, Encoding encoding, string mediaType)
        {
            this.content = content ?? "";
            this.encoding = encoding ?? Encoding.UTF8;
            headers["Content-Type"] = mediaType;
        }
        
        public override byte[] ReadAsByteArray()
        {
            return encoding.GetBytes(content);
        }
        
        public override string ReadAsString()
        {
            return content;
        }
    }
    
    public class ByteArrayContent : HttpContent
    {
        private byte[] content;
        
        public ByteArrayContent(byte[] content)
        {
            this.content = content ?? new byte[0];
        }
        
        public override byte[] ReadAsByteArray()
        {
            return content;
        }
        
        public override string ReadAsString()
        {
            return Encoding.UTF8.GetString(content);
        }
    }
    
    // ===== HTTP REQUEST EXCEPTION =====
    
    public class HttpRequestException : Exception
    {
        public HttpRequestException() : base("An error occurred while sending the HTTP request")
        {
        }
        
        public HttpRequestException(string message) : base(message)
        {
        }
        
        public HttpRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
