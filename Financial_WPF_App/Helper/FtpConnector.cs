using System;
using System.Net;

namespace Financial_WPF_App.Helper
{
    internal class FtpConnector
    {

        public static void WriteFile(string FullName, string LocalName, Tuple<string, string> Credentials)
        {
            WebClient client = new WebClient();
            client.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
            ///Write
            client.UploadFile(FullName, LocalName);
        }

        public static void UploadFileToLocal(string FullName, string RemoteName, Tuple<string, string> Credentials)
        {
            WebClient client = new WebClient();
            client.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
            ///READ 
            client.DownloadFile(FullName, RemoteName);
        }
        public static void DowloadToLocal(string FullName, string OfflinePath, Tuple<string, string> Credentials)
        {
            WebClient client = new WebClient();
            client.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
            ///READ 
            client.DownloadFile(FullName, OfflinePath);
        }
        public static void DeleteFile(string FullName, Tuple<string, string> Credentials)
        {
            ///DELETE
            System.Net.FtpWebRequest request = (FtpWebRequest)WebRequest.Create(FullName);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
            //Set mode
            request.UsePassive = true;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            response.Close();

        }
        public static Boolean ExistFile(string FullName, Tuple<string, string> Credentials)
        {
            var request = (FtpWebRequest)WebRequest.Create(FullName);
            request.Credentials = new NetworkCredential(Credentials.Item1, Credentials.Item2);
            request.UseBinary = true;
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                return true;
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    return false;
            }
            return false;
        }
    }
}
