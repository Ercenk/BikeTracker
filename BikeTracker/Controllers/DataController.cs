namespace BikeTracker.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Policy;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;

    using Facebook;

    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Table;

    using WebApplication2.Controllers;

    public class DataController : ApiController
    {

        [HttpGet]
        [Route("api/data/authorize/{token}")]
        public async Task<ApplicationUser> Authorize(string token)
        {
            var fbClient = new FacebookClient(token);
            dynamic me = await fbClient.GetTaskAsync("me");

            var adminEmail = CloudConfigurationManager.GetSetting("email");

            try
            {
                var userStore = new ApplicationUserStore();

                var user = await userStore.FindByIdAsync(me.id);
                if (user == null)
                {
                    var newUser = new ApplicationUser()
                                      {
                                          Email = me.email,
                                          Id = me.id,
                                          UserName = me.email,
                                          Role = adminEmail == me.email ? "Admin" : "User",
                                          FirstName = me.first_name,
                                          LastName = me.last_name
                                      };

                    await userStore.CreateAsync(newUser);

                    return newUser;
                }

                return user;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        //[HttpPost]
        //[Route("api/data/upload")]
        //public async Task<HttpResponseMessage> Upload()
        //{
        //    if (!Request.Content.IsMimeMultipartContent("form-data"))
        //    {
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }

        //    var streamProvider = new MultipartFormDataStreamProvider(HttpContext.Current.Server.MapPath("~/App_Data/"));

        //    // Read the MIME multipart content using the stream provider we just created.
        //    try
        //    {
        //        var bodyparts = await Request.Content.ReadAsMultipartAsync(streamProvider);
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.WriteLine(e);
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }

        //    foreach (var file in streamProvider.FileData)
        //    {
        //        var fileProcessor = new FileProcessor(
        //            file.LocalFileName,
        //            CloudConfigurationManager.GetSetting("storageAccountName"),
        //            CloudConfigurationManager.GetSetting("storageAccountKey"));

        //        await fileProcessor.Process();
        //    }

        //    return Request.CreateResponse(HttpStatusCode.Created);
        //}

        [HttpPost]
        [Route("api/data/upload")]
        public async Task<HttpResponseMessage> Upload()
        {
            if (!Request.Content.IsMimeMultipartContent("form-data"))
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var httpRequest = HttpContext.Current.Request;
            if (httpRequest.Files.Count <= 0)
            {
                throw new HttpResponseException(HttpStatusCode.NoContent);
            }

            try
            {
                foreach (var postedFile in from string file in httpRequest.Files select httpRequest.Files[file])
                {
                    using (var reader = new StreamReader(postedFile.InputStream))
                    {
                        var fileContents = reader.ReadToEnd();
                        var fileProcessor =
                            new FileProcessor(
                                CloudConfigurationManager.GetSetting("storageAccountName"),
                                CloudConfigurationManager.GetSetting("storageAccountKey"));

                        await fileProcessor.Process(fileContents);
                    }
                }
            }
            catch (Exception e)
            {
                throw new HttpResponseException(HttpStatusCode.NoContent);
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        [HttpGet]
        [Route("api/data/datapointsurl")]
        public Url DataPointsUrl()
        {
            var policy = new SharedAccessTablePolicy
                             {
                                 Permissions = SharedAccessTablePermissions.Query,
                                 SharedAccessExpiryTime = DateTime.UtcNow.AddDays(2)
                             };

            var table =
                new CloudStorageAccount(
                    new StorageCredentials(
                        CloudConfigurationManager.GetSetting("storageAccountName"),
                        CloudConfigurationManager.GetSetting("storageAccountKey")),
                    true).CreateCloudTableClient().GetTableReference(FileProcessor.RawDataTableName);

            var token = table.GetSharedAccessSignature(policy);

            return new Url(table.Uri.AbsoluteUri + token);
        }

        [HttpGet]
        [Route("api/data/segmentsurl")]
        public Url SegmentsUrl()
        {
            var policy = new SharedAccessTablePolicy
            {
                Permissions = SharedAccessTablePermissions.Query,
                SharedAccessExpiryTime = DateTime.UtcNow.AddDays(2)
            };

            var table =
                new CloudStorageAccount(
                    new StorageCredentials(
                        CloudConfigurationManager.GetSetting("storageAccountName"),
                        CloudConfigurationManager.GetSetting("storageAccountKey")),
                    true).CreateCloudTableClient().GetTableReference(FileProcessor.SegmentsTableName);

            var token = table.GetSharedAccessSignature(policy);

            return new Url(table.Uri.AbsoluteUri + token);
        }

        [HttpGet]
        [Route("api/data/usersurl")]
        public Url UsersUrl()
        {
            var policy = new SharedAccessTablePolicy
            {
                Permissions = SharedAccessTablePermissions.Query,
                SharedAccessExpiryTime = DateTime.UtcNow.AddDays(2)
            };

            var table =
                new CloudStorageAccount(
                    new StorageCredentials(
                        CloudConfigurationManager.GetSetting("storageAccountName"),
                        CloudConfigurationManager.GetSetting("storageAccountKey")),
                    true).CreateCloudTableClient().GetTableReference(ApplicationUserStore.UsersTableName);

            var token = table.GetSharedAccessSignature(policy);

            return new Url(table.Uri.AbsoluteUri + token);
        }

    }

    public class MultipartFormDataMemoryStreamProvider : MultipartMemoryStreamProvider
    {
        private readonly Collection<bool> _isFormData = new Collection<bool>();
        private readonly NameValueCollection _formData = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Stream> _fileStreams = new Dictionary<string, Stream>();

        public NameValueCollection FormData
        {
            get { return _formData; }
        }

        public Dictionary<string, Stream> FileStreams
        {
            get { return _fileStreams; }
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            var contentDisposition = headers.ContentDisposition;
            if (contentDisposition == null)
            {
                throw new InvalidOperationException("Did not find required 'Content-Disposition' header field in MIME multipart body part.");
            }

            _isFormData.Add(String.IsNullOrEmpty(contentDisposition.FileName));
            return base.GetStream(parent, headers);
        }

        public override async Task ExecutePostProcessingAsync()
        {
            for (var index = 0; index < Contents.Count; index++)
            {
                HttpContent formContent = Contents[index];
                if (_isFormData[index])
                {
                    // Field
                    string formFieldName = UnquoteToken(formContent.Headers.ContentDisposition.Name) ?? string.Empty;
                    string formFieldValue = await formContent.ReadAsStringAsync();
                    FormData.Add(formFieldName, formFieldValue);
                }
                else
                {
                    // File
                    string fileName = UnquoteToken(formContent.Headers.ContentDisposition.FileName);
                    Stream stream = await formContent.ReadAsStreamAsync();
                    FileStreams.Add(fileName, stream);
                }
            }
        }

        private static string UnquoteToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal) && token.Length > 1)
            {
                return token.Substring(1, token.Length - 2);
            }

            return token;
        }
    }
}
