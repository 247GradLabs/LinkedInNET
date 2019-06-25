using Sparkle.LinkedInNET.Asset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Sparkle.LinkedInNET.Video
{
    public static class VideoUpload
    {
        // 200MB
        private static readonly long VIDEO_MAX_BYTE_SIZE = 209715200;

        #region Video upload
        public static async Task<string> UploadVideoAsync(LinkedInApi api, UserAuthorization user, string ownerURN, byte[] videoData)
        {
            var requestVideoUpload = await RequestVideoUpload(api, user, ownerURN, videoData.Length);

            if (videoData.Length > VIDEO_MAX_BYTE_SIZE)
            {
                var upload = await UploadLongVideoPartsAsync(api, requestVideoUpload, videoData);


                var complete = await CompletMultiPartVideoAsync(user, api, requestVideoUpload, upload);
            }
            else
            {
                var shortVideoUp = await ShortUploadVideoAsync(api, requestVideoUpload, videoData);
            }

            // check video upload status before upload
            var assetMatch = Regex.Match(requestVideoUpload.Value.Asset, @"^.+?:([^:]+?)$");
            var assetId = assetMatch.Groups[1].ToString();
            while (true)
            {
                var video = await api.Asset.GetAssetAsync(user, assetId);
                // no null error check needed, cuz if it would be null we would thow an exception
                if (video.Recipes.FirstOrDefault().Status != "PROCESSING")
                    break;
                await Task.Delay(1000 * 5);
            }

            return requestVideoUpload.Value.Asset;
        }
        #endregion


        #region Request Video upload
        private static async Task<RegisterUploadResult> RequestVideoUpload(LinkedInApi api, UserAuthorization user, string ownerURN, long fileSize)
        {
            var asset = new Asset.RegisterUploadRequest()
            {
                RegisterUploadRequestData = new Asset.RegisterUploadRequestData()
                {
                    SupportedUploadMechanism = new List<string>() { "SINGLE_REQUEST_UPLOAD" },
                    Owner = ownerURN,
                    Recipes = new List<string>() { "urn:li:digitalmediaRecipe:feedshare-video" },
                    ServiceRelationships = new List<Asset.ServiceRelationship>()
                    {
                        new Asset.ServiceRelationship()
                        {
                            Identifier = "urn:li:userGeneratedContent",
                            RelationshipType = "OWNER"
                        }
                    }
                }
            };
            
            if (fileSize > VIDEO_MAX_BYTE_SIZE)
            {
                asset.RegisterUploadRequestData.FileSize = fileSize;
                asset.RegisterUploadRequestData.SupportedUploadMechanism = new List<string>() { "MULTIPART_UPLOAD" };
            }

            var requestAsset = await api.Asset.RegisterUploadAsync(user, asset);

            return requestAsset;
        }
        #endregion
                     

        #region Short video upload (< 200Mb)
        private static async Task<string> ShortUploadVideoAsync(LinkedInApi api, RegisterUploadResult registerUpload, byte[] videoData)
        {
            var postAssetResult = await api.Asset.UploadAssetAsync(registerUpload.Value.UploadMechanism.ComLinkedinDigitalmediaUploadingMediaUploadHttpRequest.UploadUrl, new Asset.UploadAssetRequest()
            {
                RequestHeaders = new Asset.ComLinkedinDigitalmediaUploadingMediaUploadHttpRequest()
                {
                    Headers = registerUpload.Value.UploadMechanism.ComLinkedinDigitalmediaUploadingMediaUploadHttpRequest.Headers,
                    UploadUrl = registerUpload.Value.UploadMechanism.ComLinkedinDigitalmediaUploadingMediaUploadHttpRequest.UploadUrl,
                },
                Data = videoData
            });

            return postAssetResult;
        }

        #endregion


        #region Long video upload (> 200)
        public static async Task<List<string>> UploadLongVideoPartsAsync(LinkedInApi api, RegisterUploadResult registerUpload, byte[] videoData)
        {
            try
            {
                if (registerUpload.Value.UploadMechanism.ComLinkedinDigitalmediaUploadingMultipartUpload.PartUploadRequests == null)
                    throw new NullReferenceException("UploadLongVideoAsync param is null");
            }
            catch (Exception e)
            {
                throw new NullReferenceException("UploadLongVideoAsync param is null", e);
            }
            
            // maybe in a later version we should return the status code as well
            List<string> partsResult = new List<string>();

            foreach (var part in registerUpload.Value.UploadMechanism.ComLinkedinDigitalmediaUploadingMultipartUpload.PartUploadRequests)
            {
                int length = (int)((part.ByteRange.LastByte + 1) - part.ByteRange.FirstByte);

                byte[] destinationArray = new byte[] { };
                Array.Resize(ref destinationArray, length);
                Array.Copy(videoData, part.ByteRange.FirstByte, destinationArray, 0, length);


                var postAssetResult = await api.Asset.UploadAssetAsync(part.Url, new Asset.UploadAssetRequest()
                {
                    RequestHeaders = new Asset.ComLinkedinDigitalmediaUploadingMediaUploadHttpRequest()
                    {
                        Headers = part.Headers,
                        UploadUrl = part.Url,
                    },
                    Data = destinationArray
                });

                partsResult.Add(postAssetResult);
            }

            return partsResult;
        }

        private static async Task<int> CompletMultiPartVideoAsync(UserAuthorization user, LinkedInApi api, RegisterUploadResult registerUploadResult, List<string> uploadResult)
        {
            var partUploadResponses = new List<PartUploadResponse>();
            foreach(var uploadRes in uploadResult)
            {
                partUploadResponses.Add(new PartUploadResponse()
                {
                    HttpStatusCode = 200,
                    Headers = new PartUploadResponseHeaders()
                    {
                        ETag = uploadRes
                    }
                });
            }

            var completeMultiPartUploadRequest = new CompleteMultipartUploadRequest()
            {
                CompleteMultipartUploadRequestData = new CompleteMultipartUploadRequestData()
                {
                    MediaArtifact = registerUploadResult.Value.MediaArtifact,
                    Metadata = registerUploadResult.Value.UploadMechanism.ComLinkedinDigitalmediaUploadingMultipartUpload.Metadata,
                    PartUploadResponses = partUploadResponses
                }
            };

            var complete = await api.Asset.CompleteMultiPartUploadAsync(user, completeMultiPartUploadRequest);


            return complete;
        }
        #endregion
    }
}
