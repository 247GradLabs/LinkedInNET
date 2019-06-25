namespace Sparkle.LinkedInNET.DemoMvc5.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using Ninject;
    using Sparkle.LinkedInNET.DemoMvc5.Domain;
    using Sparkle.LinkedInNET.OAuth2;
    using Sparkle.LinkedInNET.Profiles;
    using System.Threading.Tasks;
    using Sparkle.LinkedInNET.Organizations;
    using System.Net;
    using System.Text.RegularExpressions;

    ////using Sparkle.LinkedInNET.ServiceDefinition;

    public class HomeController : Controller
    {
        private LinkedInApi api;
        private DataService data;
        private LinkedInApiConfiguration apiConfig;

        public HomeController(LinkedInApi api, DataService data, LinkedInApiConfiguration apiConfig)
        {
            this.api = api;
            this.data = data;
            this.apiConfig = apiConfig;
        }

        public async Task<ActionResult> Index(string culture = "en-US")
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // step 1: configuration
            this.ViewBag.Configuration = this.apiConfig;
            
            // step 2: authorize url
            var scope = AuthorizationScope.ReadEmailAddress | AuthorizationScope.ReadWriteCompanyPage | AuthorizationScope.WriteShare;
            var state = Guid.NewGuid().ToString();
            var redirectUrl = this.Request.Compose() + this.Url.Action("OAuth2");
            this.ViewBag.LocalRedirectUrl = redirectUrl;
            if (this.apiConfig != null && !string.IsNullOrEmpty(this.apiConfig.ApiKey))
            {
                var authorizeUrl = this.api.OAuth2.GetAuthorizationUrl(scope, state, redirectUrl);
                this.ViewBag.Url = authorizeUrl;
            }
            else
            {
                this.ViewBag.Url = null;
            }
                       
            // step 3
            if (this.data.HasAccessToken)
            {
                var token = this.data.GetAccessToken();
                this.ViewBag.Token = token;
                var user = new UserAuthorization(token);

                var watch = new Stopwatch();
                watch.Start();
                try
                {
                    ////var profile = this.api.Profiles.GetMyProfile(user);
                    var acceptLanguages = new string[] { culture ?? "en-US", "fr-FR", };
                    var fields = FieldSelector.For<Person>()
                        .WithAllFields();
                    var profile = await this.api.Profiles.GetMyProfileAsync(user, acceptLanguages, fields);



















                    // var profile1 =  this.api.Profiles.GetMyProfile(user, acceptLanguages, fields);


                    //var firstName = profile.FirstName.Localized.First.ToObject<string>();
                    //var firstName1 = profile.FirstName.Localized.First.Last.ToString();
                    //var firstName2 = profile.FirstName.Localized.First.ToObject<string>();

                    //var fieldsOrg = FieldSelector.For<OrganizationalEntityAcls>()
                    //    .WithAllFields();
                    //var userCompanies = this.api.Organizations.GetUserAdminApprOrganizations(user, fieldsOrg);


                    //var statistic = this.api.Shares.GetShareStatistics(user, "18568129", "6386953337324994560");

                    //var orgFollorerStatistic = this.api.Organizations.GetOrgFollowerStatistics(user, "18568129");

                    //var getShares = this.api.Shares.GetShares(user, "urn:li:organization:18568129", 1000, 5, 0);

                    //var postResult = this.api.Shares.Post(user, new Common.PostShare()
                    //{
                    //    Content = new Common.PostShareContent()
                    //    {
                    //        Title = "tttt",
                    //        ContentEntities = new List<Common.PostShareContentEntities>() { new Common.PostShareContentEntities() {
                    //                  //EntityLocation = "https://www.example.com/",
                    //                  //Thumbnails = new List<Common.PostShareContentThumbnails>(){new Common.PostShareContentThumbnails()
                    //                  //{
                    //                  //    ResolvedUrl = "http://wac.2f9ad.chicdn.net/802F9AD/u/joyent.wme/public/wme/assets/ec050984-7b81-11e6-96e0-8905cd656caf.jpg?v=30"
                    //                  //} }
                    //                  Entity = postId.Location
                    //              }
                    //          }
                    //    },
                    //    Distribution = new Common.Distribution()
                    //    {
                    //        LinkedInDistributionTarget = new Common.LinkedInDistributionTarget()
                    //        {
                    //            VisibleToGuest = true
                    //        }
                    //    },
                    //    Subject = "sub",
                    //    Text = new Common.PostShareText()
                    //    {
                    //        Text = "text"
                    //    },
                    //    // Owner = "urn:li:person:" + "123456789"
                    //    Owner = "urn:li:organization:18568129",
                    //}
                    //);

                    // var originalPicture = await this.api.Profiles.GetOriginalProfilePictureAsync(user);
                    // this.ViewBag.Picture = originalPicture;

                    this.ViewBag.Profile = profile;
                }
                catch (LinkedInApiException ex)
                {
                    this.ViewBag.ProfileError = ex.ToString();
                }
                catch (Exception ex)
                {
                    this.ViewBag.ProfileError = ex.ToString();
                }

                watch.Stop();
                this.ViewBag.ProfileDuration = watch.Elapsed;
            }

            return this.View();
        }

        public static byte[] DownladFromUrlToByte(string url)
        {
            HttpWebRequest req;
            HttpWebResponse res = null;

            try
            {
                req = (HttpWebRequest)WebRequest.Create(url);
                res = (HttpWebResponse)req.GetResponse();
                Stream stream = res.GetResponseStream();

                var buffer = new byte[4096];
                using (MemoryStream ms = new MemoryStream())
                {
                    var bytesRead = 0;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        ms.Write(buffer, 0, bytesRead);
                    }
                    return ms.ToArray();
                }
            }
            finally
            {
                if (res != null)
                    res.Close();
            }
        }

        public async Task<ActionResult> OAuth2(string code, string state, string error, string error_description)
        {
            if (!string.IsNullOrEmpty(error))
            {
                this.ViewBag.Error = error;
                this.ViewBag.ErrorDescription = error_description;
                return this.View();
            }

            var redirectUrl = this.Request.Compose() + this.Url.Action("OAuth2");
            var result = await this.api.OAuth2.GetAccessTokenAsync(code, redirectUrl);

            this.ViewBag.Code = code;
            this.ViewBag.Token = result.AccessToken;

            this.data.SaveAccessToken(result.AccessToken);

            var user = new UserAuthorization(result.AccessToken);

            ////var profile = this.api.Profiles.GetMyProfile(user);
            ////this.data.SaveAccessToken();
            return this.View();
        }

        public ActionResult Connections()
        {
            var token = this.data.GetAccessToken();
            var user = new UserAuthorization(token);
            // var connection = this.api.Profiles.GetMyConnections(user, 0, 500);
            return this.View(string.Empty);
        }

        public ActionResult FullProfile(string id, string culture = "en-US")
        {
            var token = this.data.GetAccessToken();
            this.ViewBag.Token = token;
            var user = new UserAuthorization(token);

            Person profile = null;
            var watch = new Stopwatch();
            watch.Start();
            try
            {
                ////var profile = this.api.Profiles.GetMyProfile(user);
                var acceptLanguages = new string[] { culture ?? "en-US", "fr-FR", };
                var fields = FieldSelector.For<Person>()                   
                    .WithAllFields();
                profile = this.api.Profiles.GetMyProfileAsync(user, acceptLanguages, fields).Result;

                this.ViewBag.Profile = profile;
            }
            catch (LinkedInApiException ex)
            {
                this.ViewBag.ProfileError = ex.ToString();
                this.ViewBag.RawResponse = ex.Data["ResponseText"];
            }
            catch (LinkedInNetException ex)
            {
                this.ViewBag.ProfileError = ex.ToString();
                this.ViewBag.RawResponse = ex.Data["ResponseText"];
            }
            catch (Exception ex)
            {
                this.ViewBag.ProfileError = ex.ToString();
            }

            watch.Stop();
            this.ViewBag.ProfileDuration = watch.Elapsed;

            return this.View(profile);
        }

        public ActionResult Play()
        {
            var token = this.data.GetAccessToken();
            this.ViewBag.Token = token;
            return this.View();
        }

        public ActionResult Definition()
        {
            var filePath = Path.Combine(this.Server.MapPath("~"), "..", "LinkedInApiV2.xml");
            var builder = new Sparkle.LinkedInNET.ServiceDefinition.ServiceDefinitionBuilder();
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                builder.AppendServiceDefinition(fileStream);
            }

            var result = new ApiResponse<Sparkle.LinkedInNET.ServiceDefinition.ApisRoot>(builder.Root);

            
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new System.IO.StreamWriter(stream);
            var generator = new ServiceDefinition.CSharpGenerator(writer);
            generator.Run(builder.Definition);
            stream.Seek(0L, SeekOrigin.Begin);            
            var serviceResult = new StreamReader(stream).ReadToEnd();


            return this.Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult LogOff(string ReturnUrl)
        {
            this.data.ClearAccessToken();

            if (this.Url.IsLocalUrl(ReturnUrl))
            {
                return this.Redirect(ReturnUrl);
            }
            else
            {
                return this.RedirectToAction("Index");
            }
        }

        public class ApiResponse<T>
        {
            public ApiResponse()
            {
            }

            public ApiResponse(T data)
            {
                this.Data = data;
            }

            public string Error { get; set; }
            public T Data { get; set; }
        }
    }
}
