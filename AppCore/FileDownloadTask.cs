using Ae.Dns.Client;
using Ae.Dns.Protocol;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Iros.Workshop;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AppCore
{
    public enum FileDownloadTaskMode
    {
        HTTP = 0,
        GDRIVE = 1
    }

    /// <summary>
    /// Downloads a file using <see cref="WebRequest"/> which allows for pausing and resuming capability.
    /// </summary>
    /// <remarks>
    /// The class keeps track of how many bytes written so just call <see cref="Pause"/> to pause the download and <see cref="Start"/> to resume it.
    /// reference: https://stackoverflow.com/questions/15995705/adding-pause-and-continue-ability-in-my-downloader
    /// </remarks>
    public class FileDownloadTask
    {
        public event ProgressChangedEventHandler DownloadProgressChanged;
        public event AsyncCompletedEventHandler DownloadFileCompleted;

        private bool _isCanceled;
        private bool _isStarted;
        private bool _allowedToRun;
        private string _sourceUrl;
        private string _destination;
        private int _chunkSize;
        private long? _contentLength;
        private bool _checkedContentRange;
        private FileDownloadTaskMode _fdtMode;
        private Uri _responseUri;

        private object _userState;
        private object _lock = new object();

        private CookieContainer _cookies;

        public WebHeaderCollection Headers { get; set; }

        public long BytesWritten { get; private set; }
        public long ContentLength
        {
            get
            {
                return _contentLength.GetValueOrDefault(-1);
            }
        }

        public bool Done { get { return ContentLength == BytesWritten; } }

        public bool IsPaused { get => !AllowedToRun; }

        public bool AllowedToRun
        {
            get
            {
                lock (_lock)
                {
                    return _allowedToRun;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _allowedToRun = value;
                }
            }
        }

        public bool IsCanceled
        {
            get
            {
                lock (_lock)
                {
                    return _isCanceled;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _isCanceled = value;
                }
            }
        }

        public bool IsStarted { get => _isStarted; }

        public DownloadItem downloadItem { get; set; }

        public FileDownloadTask(string source, string destination, object userState = null, CookieContainer cookies = null, FileDownloadTaskMode fdtMode = default, int chunkSizeInBytes = 10000 /*Default to 0.01 mb*/)
        {
            AllowedToRun = true;

            _sourceUrl = source;
            _destination = destination;
            _chunkSize = chunkSizeInBytes;
            _checkedContentRange = false;
            _contentLength = null;
            _userState = userState;
            _isStarted = false;
            _fdtMode = fdtMode;

            _cookies = cookies;

            BytesWritten = 0;

            switch (_fdtMode)
            {
                case FileDownloadTaskMode.GDRIVE:
                    // Examples:
                    // - https://docs.google.com/uc?id=0B-Q_AObuWRSXNW9rb3FxS0F1Qk0&export=download
                    // - https://docs.google.com/uc?export=download&confirm=_bQe&id=0B-Q_AObuWRSXNW9rb3FxS0F1Qk0
                    _sourceUrl = String.Format("https://docs.google.com/uc?id={0}&export=download", source); ;
                    break;
            }
        }

        private async Task Start(long range)
        {
            if (IsPaused || IsCanceled)
                return;

            try
            {
                Sys.Message(new WMessage() { Text = $"Starting download using URL: {_sourceUrl}", LogLevel = WMessageLogLevel.Info });

                using (HttpClient client = CreateHttpClient())
                {
                    await SendAndDownloadAsync(client, range);
                }
            }
            catch (Exception primaryEx) when (!IsCanceled)
            {
                Sys.Message(new WMessage()
                {
                    Text = $"Primary download failed for {_sourceUrl}, retrying with DoH fallback (Cloudflare + Google): {primaryEx.Message}",
                    LogLevel = WMessageLogLevel.LogOnly
                });

                try
                {
                    // If the first attempt wrote data before failing, restart the file for a clean fallback attempt.
                    if (BytesWritten > range && File.Exists(_destination))
                    {
                        File.Delete(_destination);
                        BytesWritten = 0;
                        _isStarted = false;
                        range = 0;
                    }

                    using (HttpClient dohClient = CreateDohFallbackHttpClient())
                    {
                        await SendAndDownloadAsync(dohClient, range);
                    }
                }
                catch (Exception dohEx)
                {
                    downloadItem.OnCancel?.Invoke();
                    throw new Exception("Failed to download - Please report this in the Tsunamods Discord", new AggregateException(primaryEx, dohEx));
                }
            }

            if (AllowedToRun && !IsCanceled && (BytesWritten == ContentLength) || (ContentLength == -1)) // -1 is returned when response doesnt have the content-length
            {
                DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, false, _userState));
            }
            else if (IsCanceled)
            {
                DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, cancelled: true, _userState));

                DownloadProgressChanged?.Invoke(this, new ProgressChangedEventArgs((int)(0), _userState));
                File.Delete(_destination); // delete temp file just downloaded
                BytesWritten = 0;
            }
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler()
            {
                UseCookies = _cookies != null,
                CookieContainer = _cookies != null ? _cookies : new CookieContainer(),
            };

            return CreateHttpClientWithDefaults(handler);
        }

        private HttpClient CreateDohFallbackHttpClient()
        {
            var cloudflareClient = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://cloudflare-dns.com/") });
            var googleClient = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });
            var racerClient = new DnsRacerClient(cloudflareClient, googleClient);

            var dnsHandler = new DnsDelegatingHandler(racerClient)
            {
                InnerHandler = new HttpClientHandler()
                {
                    UseCookies = _cookies != null,
                    CookieContainer = _cookies != null ? _cookies : new CookieContainer(),
                }
            };

            return CreateHttpClientWithDefaults(dnsHandler);
        }

        private HttpClient CreateHttpClientWithDefaults(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:151.0) Gecko/20100101 Firefox/151.0");

            if (Headers != null)
            {
                client.DefaultRequestHeaders.Add("Referer", Headers["Referer"]);
            }

            return client;
        }

        private async Task SendAndDownloadAsync(HttpClient client, long range)
        {
            using (var request = new HttpRequestMessage { RequestUri = new Uri(_sourceUrl) })
            {
                if (range > 0)
                {
                    request.Headers.Range = new RangeHeaderValue(0, range);
                }

                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (ContentLength == -1 && !_checkedContentRange)
                {
                    _checkedContentRange = true;
                    _contentLength = response.Content.Headers.ContentLength;
                    _responseUri = response.RequestMessage.RequestUri;
                }

                Stream responseStream = response.Content.ReadAsStream();

                FileMode fileMode = FileMode.Append;

                if (!IsStarted)
                {
                    fileMode = FileMode.Create;
                }

                FileStream fs = new FileStream(_destination, fileMode, FileAccess.Write, FileShare.ReadWrite);

                while (AllowedToRun && !IsCanceled)
                {
                    _isStarted = true;
                    byte[] buffer = new byte[_chunkSize];
                    int bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                    if (bytesRead == 0) break;

                    await fs.WriteAsync(buffer, 0, bytesRead);
                    BytesWritten += bytesRead;

                    float prog = (float)BytesWritten / (float)ContentLength;
                    DownloadProgressChanged?.Invoke(this, new ProgressChangedEventArgs((int)(prog * 100), _userState));

                    if (IsCanceled) break;
                }

                await fs.FlushAsync();
                fs.Close();
                responseStream.Close();

                switch (_fdtMode)
                {
                    case FileDownloadTaskMode.GDRIVE:
                        await EnsureGDriveFile(_destination, response.Content.Headers.ContentType.MediaType);
                        break;
                }
            }
        }

        /// <summary>
        /// Reads length of <see cref="_destination"/> file if it exists and sets <see cref="BytesWritten"/> so partial downloaded file can be resumed
        /// </summary>
        public void SetBytesWrittenFromExistingFile()
        {
            if (File.Exists(_destination))
            {
                BytesWritten = new FileInfo(_destination).Length;
                _isStarted = true; // set to true so downloaded file will be appended
            }
            else
            {
                BytesWritten = 0;
            }
        }

        public Task Start()
        {
            AllowedToRun = true;
            try
            {
                Task downloadTask = Start(BytesWritten);

                // wire up async task to handle exceptions that may occurr does not work because using bloc
                downloadTask.ContinueWith((result) =>
                {
                    if (result.IsFaulted)
                    {
                        DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(result.Exception.GetBaseException(), false, _userState));
                    }
                });

                return downloadTask;
            }
            catch (Exception ex) {
                throw new Exception("Failed to download", ex);
            }
        }

        public void Pause()
        {
            AllowedToRun = false;
        }

        public void CancelAsync()
        {
            if (IsPaused)
            {
                // if the download is paused when cancelling download then invoke the event here since async Start() task is not running to invoke the event
                DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, cancelled: true, _userState));
            }

            IsCanceled = true;
            AllowedToRun = false;
        }

        private async Task EnsureGDriveFile(string file, string contentType)
        {
            if (contentType.Contains("html"))
            {
                if (new FileInfo(file).Length < 100 * 1024)
                {
                    try
                    {
                        string html = File.ReadAllText(file);
                        var document = await BrowsingContext.New().OpenAsync(m => m.Content(html));

                        if (document.Title.Contains("Quota exceeded")) throw new Exception(document.Title);

                        var url = _responseUri + String.Format("&confirm={0}&uuid={1}", document.QuerySelector("input[name=\"confirm\"]").Attributes["value"].Value, document.QuerySelector("input[name=\"uuid\"]").Attributes["value"].Value);

                        DownloadProgressChanged?.Invoke(this, new ProgressChangedEventArgs((int)(0), _userState));
                        File.Delete(_destination); // delete temp html file just downloaded
                        BytesWritten = 0;

                        HttpClient client = CreateHttpClient();

                        if (client.DefaultRequestHeaders.Contains("Referer"))
                        {
                            client.DefaultRequestHeaders.Remove("Referer");
                        }

                        client.DefaultRequestHeaders.Add("Referer", _sourceUrl);

                        HttpResponseMessage response;

                        try
                        {
                            response = await client.SendAsync(new HttpRequestMessage { RequestUri = new Uri(url) }, HttpCompletionOption.ResponseHeadersRead);
                        }
                        catch (Exception primaryEx) when (!IsCanceled)
                        {
                            Sys.Message(new WMessage()
                            {
                                Text = $"Primary GDrive confirmation download failed for {_sourceUrl}, retrying with DoH fallback (Cloudflare + Google): {primaryEx.Message}",
                                LogLevel = WMessageLogLevel.LogOnly
                            });

                            if (File.Exists(_destination))
                            {
                                File.Delete(_destination);
                            }

                            BytesWritten = 0;
                            _isStarted = false;

                            client = CreateDohFallbackHttpClient();

                            if (client.DefaultRequestHeaders.Contains("Referer"))
                            {
                                client.DefaultRequestHeaders.Remove("Referer");
                            }

                            client.DefaultRequestHeaders.Add("Referer", _sourceUrl);
                            response = await client.SendAsync(new HttpRequestMessage { RequestUri = new Uri(url) }, HttpCompletionOption.ResponseHeadersRead);
                        }

                        _contentLength = response.Content.Headers.ContentLength;

                        Stream responseStream = response.Content.ReadAsStream();

                        FileMode fileMode = FileMode.Append;

                        if (!IsStarted)
                        {
                            fileMode = FileMode.Create;
                        }

                        FileStream fs = new FileStream(_destination, fileMode, FileAccess.Write, FileShare.ReadWrite);

                        while (AllowedToRun && !IsCanceled)
                        {
                            _isStarted = true;
                            byte[] buffer = new byte[_chunkSize];
                            int bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                            if (bytesRead == 0) break;

                            await fs.WriteAsync(buffer, 0, bytesRead);
                            BytesWritten += bytesRead;

                            float prog = (float)BytesWritten / (float)ContentLength;
                            DownloadProgressChanged?.Invoke(this, new ProgressChangedEventArgs((int)(prog * 100), _userState));

                            if (IsCanceled) break;
                        }

                        await fs.FlushAsync();
                        fs.Close();
                        responseStream.Close();
                        client.Dispose();

                        if (AllowedToRun && !IsCanceled && (BytesWritten == ContentLength) || (ContentLength == -1)) // -1 is returned when response doesnt have the content-length
                        {
                            DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, false, _userState));
                        }
                        else if (IsCanceled)
                        {
                            DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, cancelled: true, _userState));
                        }
                    }
                    catch (Exception ex)
                    {
                        DownloadProgressChanged?.Invoke(this, new ProgressChangedEventArgs((int)(0), _userState));
                        File.Delete(_destination); // delete temp html file just downloaded
                        BytesWritten = 0;

                        throw new Exception($"Failed to download", ex);
                    }
                }
                else
                {
                    DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, false, _userState));
                }
            }
            else
            {
                // actual file being downloaded has finished successfully at this point
                DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, false, _userState));
            }
        }
    }
}
