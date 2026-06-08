using CG.Web.MegaApiClient;
using Iros.Workshop;
using System.Net;

namespace AppCore
{
    public sealed record LinkValidationResult(bool IsValid, string ValidationTarget, string Error);

    public static class DownloadLinkValidator
    {
        public static async Task<LinkValidationResult> ValidateAsync(string rawLink)
        {
            if (string.IsNullOrWhiteSpace(rawLink))
            {
                return new LinkValidationResult(false, string.Empty, "empty link");
            }

            if (!LocationUtil.TryParse(rawLink, out LocationType type, out string location))
            {
                return new LinkValidationResult(false, string.Empty, "unable to parse iros link");
            }

            switch (type)
            {
                case LocationType.Url:
                    if (!Uri.TryCreate(location, UriKind.Absolute, out Uri? urlUri) ||
                        (urlUri.Scheme != Uri.UriSchemeHttp && urlUri.Scheme != Uri.UriSchemeHttps))
                    {
                        return new LinkValidationResult(false, location, "URL link is not a valid HTTP/HTTPS URL");
                    }

                    return await CheckHttpResourceExistsAsync(location);

                case LocationType.GDrive:
                    string gdriveUrl = $"https://docs.google.com/uc?id={location}&export=download";
                    return await CheckHttpResourceExistsAsync(gdriveUrl);

                case LocationType.ExternalUrl:
                    if (string.IsNullOrWhiteSpace(location))
                    {
                        return new LinkValidationResult(false, location, "ExternalUrl link is empty and cannot be validated directly");
                    }

                    if (!Uri.TryCreate(location, UriKind.Absolute, out Uri? extUri) ||
                        (extUri.Scheme != Uri.UriSchemeHttp && extUri.Scheme != Uri.UriSchemeHttps))
                    {
                        return new LinkValidationResult(false, location, "ExternalUrl is not a valid HTTP/HTTPS URL");
                    }

                    return await CheckHttpResourceExistsAsync(location);

                case LocationType.MegaSharedFolder:
                    return await CheckMegaNodeExistsAsync(location);

                default:
                    return new LinkValidationResult(false, location, $"unsupported link type {type}");
            }
        }

        private static async Task<LinkValidationResult> CheckHttpResourceExistsAsync(string url)
        {
            try
            {
                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage headRequest = new HttpRequestMessage(HttpMethod.Head, url);
                using HttpResponseMessage headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);

                if (headResponse.StatusCode == HttpStatusCode.MethodNotAllowed || headResponse.StatusCode == HttpStatusCode.NotImplemented)
                {
                    using HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                    using HttpResponseMessage getResponse = await client.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead);
                    return getResponse.StatusCode == HttpStatusCode.NotFound
                        ? new LinkValidationResult(false, url, $"HTTP {(int)getResponse.StatusCode} ({getResponse.StatusCode})")
                        : new LinkValidationResult(true, url, string.Empty);
                }

                return headResponse.StatusCode == HttpStatusCode.NotFound
                    ? new LinkValidationResult(false, url, $"HTTP {(int)headResponse.StatusCode} ({headResponse.StatusCode})")
                    : new LinkValidationResult(true, url, string.Empty);
            }
            catch (Exception ex)
            {
                return new LinkValidationResult(false, url, ex.Message);
            }
        }

        private static async Task<LinkValidationResult> CheckMegaNodeExistsAsync(string location)
        {
            string[] parts = location.Split(',', 3);

            if (parts.Length < 2)
            {
                return new LinkValidationResult(false, location, "MegaSharedFolder link must contain folder ID and file ID or file name");
            }

            string folderId = parts[0].Trim();
            string fileId = parts[1].Trim();
            string fileName = parts.Length >= 3 ? parts[2].Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(folderId))
            {
                return new LinkValidationResult(false, location, "MegaSharedFolder folder ID is empty");
            }

            if (string.IsNullOrWhiteSpace(fileId) && string.IsNullOrWhiteSpace(fileName))
            {
                return new LinkValidationResult(false, location, "MegaSharedFolder must specify file ID or file name");
            }

            MegaApiClient client = new MegaApiClient();
            string resolvedTarget = location;

            try
            {
                await client.LoginAnonymousAsync();

                if (!TryBuildMegaFolderShareUri(folderId, out Uri? folderLink, out string folderUriError))
                {
                    return new LinkValidationResult(false, resolvedTarget, folderUriError);
                }

                resolvedTarget = string.IsNullOrWhiteSpace(fileName)
                    ? $"{folderLink} (fileId: {fileId})"
                    : $"{folderLink} (fileId: {fileId}, fileName: {fileName})";

                var nodes = client.GetNodesFromLink(folderLink);

                var fileNode = nodes.FirstOrDefault(x => x.Type == NodeType.File && x.Id == fileId);

                if (fileNode == null && !string.IsNullOrWhiteSpace(fileName))
                {
                    fileNode = nodes.FirstOrDefault(x => x.Type == NodeType.File && x.Name == fileName);
                }

                return fileNode != null
                    ? new LinkValidationResult(true, resolvedTarget, string.Empty)
                    : new LinkValidationResult(false, resolvedTarget, "file not found in MEGA shared folder");
            }
            catch (Exception ex)
            {
                return new LinkValidationResult(false, resolvedTarget, ex.Message);
            }
            finally
            {
                try
                {
                    await client.LogoutAsync();
                }
                catch
                {
                }
            }
        }

        private static bool TryBuildMegaFolderShareUri(string folderId, out Uri? folderUri, out string error)
        {
            folderUri = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(folderId))
            {
                error = "MegaSharedFolder folder ID is empty";
                return false;
            }

            string value = folderId.Trim();

            if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absoluteUri))
            {
                if (absoluteUri.Host.Contains("mega.nz", StringComparison.OrdinalIgnoreCase) &&
                    absoluteUri.AbsolutePath.StartsWith("/folder/", StringComparison.OrdinalIgnoreCase))
                {
                    folderUri = absoluteUri;
                    return true;
                }

                error = "MegaSharedFolder must point to a mega.nz folder share (/folder/...)";
                return false;
            }

            if (value.StartsWith("#F!", StringComparison.OrdinalIgnoreCase) || value.StartsWith("F!", StringComparison.OrdinalIgnoreCase))
            {
                string old = value.StartsWith("#", StringComparison.Ordinal) ? value.Substring(1) : value;
                string[] oldParts = old.Split('!', StringSplitOptions.RemoveEmptyEntries);

                if (oldParts.Length >= 3)
                {
                    folderUri = new Uri($"https://mega.nz/folder/{oldParts[1]}#{oldParts[2]}");
                    return true;
                }
            }

            if (value.StartsWith("/folder/", StringComparison.OrdinalIgnoreCase))
            {
                folderUri = new Uri($"https://mega.nz{value}");
                return true;
            }

            if (value.StartsWith("folder/", StringComparison.OrdinalIgnoreCase))
            {
                folderUri = new Uri($"https://mega.nz/{value}");
                return true;
            }

            folderUri = new Uri($"https://mega.nz/folder/{value}");
            return true;
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:151.0) Gecko/20100101 Firefox/151.0");
            return client;
        }
    }
}
