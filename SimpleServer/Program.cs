namespace SimpleServer
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net;

    class Program
    {
        static void Main(string[] args)
        {
            string baseUrl = "http://localhost:8080/";
            string rootPath = Environment.CurrentDirectory;

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(baseUrl); // port range may cause permission issues
            listener.Start();

            Console.WriteLine($"Server running at {baseUrl}");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();

                string requestedFile = context.Request.RawUrl;
                if (string.IsNullOrWhiteSpace(requestedFile) || requestedFile == "/")
                {
                    requestedFile = "/index.html";
                }
                string filePath = Path.Combine(rootPath, requestedFile.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        // Check if client accepts Brotli or GZip compression
                        bool useBrotli = context.Request.Headers["Accept-Encoding"].Contains("br");
                        bool useGZip = context.Request.Headers["Accept-Encoding"].Contains("gzip");

                        // Set appropriate content encoding header
                        if (useBrotli)
                        {
                            context.Response.Headers.Add("Content-Encoding", "br");
                        }
                        else if (useGZip)
                        {
                            context.Response.Headers.Add("Content-Encoding", "gzip");
                        }

                        // Set appropriate MIME type
                        string extension = Path.GetExtension(filePath);
                        string mimeType = GetMimeType(extension);
                        context.Response.ContentType = mimeType;

                        // Compress the response stream
                        Stream responseStream = context.Response.OutputStream;
                        if (useBrotli)
                        {
                            responseStream = new BrotliStream(responseStream, CompressionMode.Compress);
                        }
                        else if (useGZip)
                        {
                            responseStream = new GZipStream(responseStream, CompressionMode.Compress);
                        }

                        // Copy the file stream to the response stream
                        fileStream.CopyTo(responseStream);
                        responseStream.Flush();
                        responseStream.Close();
                    }
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
        }

        static string GetMimeType(string extension)
        {
            switch (extension)
            {
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "text/javascript";
                case ".jpg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
