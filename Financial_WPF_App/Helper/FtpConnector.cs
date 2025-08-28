using System;
using System.IO;
using System.Linq;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Security;
using Renci.SshNet.Sftp;

namespace Financial_WPF_App.Helper
{
    internal class FtpConnector
    {
        // Upload local file -> remote path
        public static void WriteFile(string FullName, string LocalName, Tuple<string, string> Credentials)
        {
            var ctx = CreateClientAndPath(FullName, Credentials);
            var client = ctx.client;
            var remotePath = ctx.remotePath;

            try
            {
                client.Connect();
                var remoteDir = Path.GetDirectoryName(remotePath);
                if (!string.IsNullOrEmpty(remoteDir))
                    EnsureRemoteDirectory(client, remoteDir.Replace('\\', '/'));

                using (var fs = File.OpenRead(LocalName))
                {
                    client.UploadFile(fs, remotePath, true);
                }
            }
            finally
            {
                if (client.IsConnected) client.Disconnect();
                client.Dispose();
            }
        }

        // Download remote file -> local path (kept method name)
        public static void UploadFileToLocal(string FullName, string RemoteName, Tuple<string, string> Credentials)
        {
            var ctx = CreateClientAndPath(FullName, Credentials);
            var client = ctx.client;
            var remotePath = ctx.remotePath;

            try
            {
                client.Connect();

                var localDir = Path.GetDirectoryName(RemoteName);
                if (!string.IsNullOrEmpty(localDir))
                    Directory.CreateDirectory(localDir);

                using (var fs = File.Open(RemoteName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    client.DownloadFile(remotePath, fs);
                }
            }
            finally
            {
                if (client.IsConnected) client.Disconnect();
                client.Dispose();
            }
        }

        // Download remote file -> local path
        public static void DowloadToLocal(string FullName, string OfflinePath, Tuple<string, string> Credentials)
        {
            var ctx = CreateClientAndPath(FullName, Credentials);
            var client = ctx.client;
            var remotePath = ctx.remotePath;

            try
            {
                client.Connect();

                var localDir = Path.GetDirectoryName(OfflinePath);
                if (!string.IsNullOrEmpty(localDir))
                    Directory.CreateDirectory(localDir);

                using (var fs = File.Open(OfflinePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    client.DownloadFile(remotePath, fs);
                }
            }
            finally
            {
                if (client.IsConnected) client.Disconnect();
                client.Dispose();
            }
        }

        // Delete remote file
        public static void DeleteFile(string FullName, Tuple<string, string> Credentials)
        {
            var ctx = CreateClientAndPath(FullName, Credentials);
            var client = ctx.client;
            var remotePath = ctx.remotePath;

            try
            {
                client.Connect();
                if (client.Exists(remotePath) && !IsDirectory(client, remotePath))
                {
                    client.DeleteFile(remotePath);
                }
            }
            finally
            {
                if (client.IsConnected) client.Disconnect();
                client.Dispose();
            }
        }

        // Check if remote file exists
        public static bool ExistFile(string FullName, Tuple<string, string> Credentials)
        {
            var ctx = CreateClientAndPath(FullName, Credentials);
            var client = ctx.client;
            var remotePath = ctx.remotePath;

            try
            {
                client.Connect();
                return client.Exists(remotePath) && !IsDirectory(client, remotePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("General error: " + ex.Message);
                throw;
            }
            finally
            {
                if (client.IsConnected) client.Disconnect();
                client.Dispose();
            }
        }

        // ----------------- helpers -----------------

        private static bool IsDirectory(SftpClient client, string path)
        {
            try
            {
                var attrs = client.GetAttributes(path);
                return attrs.IsDirectory;
            }
            catch (SftpPathNotFoundException)
            {
                return false;
            }
        }

        private static void EnsureRemoteDirectory(SftpClient client, string remoteDir)
        {
            if (string.IsNullOrEmpty(remoteDir) || remoteDir == "/") return;

            var path = remoteDir.Replace('\\', '/').Trim('/');
            if (string.IsNullOrEmpty(path)) return;

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var cur = "/";

            for (int i = 0; i < parts.Length; i++)
            {
                cur = cur.EndsWith("/") ? cur + parts[i] : cur + "/" + parts[i];
                if (!client.Exists(cur))
                    client.CreateDirectory(cur);
            }
        }

        // Create an SftpClient configured like FileZilla (password only)
        private static (SftpClient client, string remotePath) CreateClientAndPath(string fullName, Tuple<string, string> credentials)
        {
            if (string.IsNullOrEmpty(fullName) || !fullName.StartsWith("sftp://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("FullName must be an sftp:// URI, e.g., sftp://example.com/path/file.txt", nameof(fullName));

            var uri = new Uri(fullName);
            if (!string.Equals(uri.Scheme, "sftp", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("URI scheme must be sftp://", nameof(fullName));

            var host = uri.Host;
            var port = uri.IsDefaultPort ? 22 : uri.Port;
            var remotePath = Uri.UnescapeDataString(uri.AbsolutePath);

            var username = credentials.Item1;   // e.g. "f_user"
            var password = credentials.Item2;   // same as FileZilla

            // Password-only
            var connInfo = new PasswordConnectionInfo(host, port, username, password)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // ---- Prefer widely supported modern algorithms ----
            // (Keeps compatibility with common OpenSSH defaults)
            // If your SSH.NET version doesn’t have some of these, remove the missing ones.
             
            var client = new SftpClient(connInfo)
            {
                OperationTimeout = TimeSpan.FromSeconds(60),
                KeepAliveInterval = TimeSpan.FromSeconds(15)
            };

            // Optional: trust-on-first-use (or pin fingerprint via HostKeyReceived)
            // client.HostKeyReceived += (s, e) => { e.CanTrust = true; };

            return (client, remotePath);
        }
    }
}

//////////////////////////////////////////////////////
/// OLD FTP connection
//////////////////////////////////////////////////////
//public static void WriteFile(string FullName, string LocalName, Tuple<string, string> Credentials)
//{
//    WebClient client = new WebClient();
//    client.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
//    ///Write
//    client.UploadFile(FullName, LocalName);
//} 
//public static void UploadFileToLocal(string FullName, string RemoteName, Tuple<string, string> Credentials)
//{
//    WebClient client = new WebClient();
//    client.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
//    ///READ 
//    client.DownloadFile(FullName, RemoteName);
//}
//public static void DowloadToLocal(string FullName, string OfflinePath, Tuple<string, string> Credentials)
//{
//    WebClient client = new WebClient();
//    client.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
//    ///READ 
//    client.DownloadFile(FullName, OfflinePath);
//}
//public static void DeleteFile(string FullName, Tuple<string, string> Credentials)
//{
//    ///DELETE
//    System.Net.FtpWebRequest request = (FtpWebRequest)WebRequest.Create(FullName);
//    request.Method = WebRequestMethods.Ftp.DeleteFile;
//    request.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
//    //Set mode
//    request.UsePassive = true;

//    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
//    response.Close();

//}
//public static Boolean ExistFile(string FullName, Tuple<string, string> Credentials)
//{
//    var request = (FtpWebRequest)WebRequest.Create(FullName);
//    request.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
//    request.UseBinary = true;
//    request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
//    try
//    {
//        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
//        return true;
//    }
//    catch (WebException ex)
//    {
//        FtpWebResponse response = (FtpWebResponse)ex.Response;
//        if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
//            return false;
//    }
//    return false;
//}

