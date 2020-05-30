﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SimplySeniors.DAL;
using SimplySeniors.Models;
using SimplySeniors.Models.ViewModel;
using Microsoft.AspNet.Identity;
using System.Globalization;
using SimplySeniors.Attributes;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;


namespace SimplySeniors.Controllers
{
    public class UserHomePageController : Controller
    {
        private ProfileContext profile = new ProfileContext();
        private PostContext db = new PostContext();
        private FollowContext db2 = new FollowContext();
        private CommentContext db3 = new CommentContext();

        // GET: UserHomePage
        [CustomAuthorize]
        public ActionResult HomePage()
        {
            // Get the ASP.NET Identity Id of the currently authorized user
            var id = User.Identity.GetUserId();
            var profiledb = new ProfileContext();

            // Get all profile info for current logged in user where the ASPNET ID = profile ID
            var profile = profiledb.Profiles.FirstOrDefault(u => u.USERID == id);
            var followed = db2.FollowLists.Where(x => x.UserID == profile.ID).Select(y => y.FollowProfile).ToList();
            var IdList = db2.FollowLists.Where(x => x.UserID == profile.ID).Select(y => y.FollowedUserID).ToList();
            IdList.Add(profile.ID);
            var postlist = db.Posts.Where(x => IdList.Contains(x.ProfileID)).OrderByDescending(x => x.PostDate).ToList();
            var state = profile.STATE;
            var city = profile.CITY;
            var address = "+" + city + "," + "+" + state + "," + "+USA";
            var appID = System.Web.Configuration.WebConfigurationManager.AppSettings["mapApiKey"];
            var requestUri = $"https://maps.googleapis.com/maps/api/geocode/json?address={address}&key={appID}";
            var request = WebRequest.Create(requestUri);
            var response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            var json = reader.ReadToEnd();
            dynamic information = JObject.Parse(json);
            double lat = Convert.ToDouble(information.results[0].geometry.location.lat, CultureInfo.InvariantCulture);
            var lng= Convert.ToDouble(information.results[0].geometry.location.lng, CultureInfo.InvariantCulture);
            var location = new Double[2];
            location[0] = lat;
            location[1] = lng;
            List<PostComment> comments = new List<PostComment>();
            foreach (Post post in postlist)
            {
                comments.AddRange(db3.PostComments.Where(x => x.PostID == post.ID).OrderByDescending(x=> x.CommentDate).ToList());
            }
            var viewModel = new UserHomeViewModel(profile, postlist, location, followed, comments);
            return View(viewModel);
        }
        
        [HttpPost]   // FUNCTION TO OBTAIN the current user for message feature. 

        public JsonResult GetCurrentUser()
        {
            var id = User.Identity.GetUserId();
            var usersProfile = profile.Profiles.FirstOrDefault(u => u.USERID == id);
            return Json(usersProfile, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetAllProfiles()
        {
            //var listOfAllProfiles = profile.Profiles.Select(u => u.FIRSTNAME).ToList();
            //return Json(listOfAllProfiles, JsonRequestBehavior.AllowGet);
            var Members = profile.Profiles.Select(r => r.FIRSTNAME).Distinct();
            return Json(Members, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetSpecificMember(int id)
        {
            var person = profile.Profiles.FirstOrDefault(u => u.ID == id);
            return Json(person, JsonRequestBehavior.AllowGet);
        }

        public ActionResult NewsFeed()
        {
            var appID = System.Web.Configuration.WebConfigurationManager.AppSettings["newsApiKey"];
            string requestURL = string.Format("http://newsapi.org/v2/top-headlines?country=us&apiKey={0}", appID);
            string json = new WebClient().DownloadString(requestURL);
            var jsonObj = JObject.Parse(json);
            JArray jsonarray = (JArray)jsonObj.SelectToken("articles");
            List<NewsAPI> newsLists = new List<NewsAPI>();
            for( int i = 0; i < jsonarray.Count; i++)
            {
                JObject NewsObj = JObject.Parse(jsonarray[i].ToString());
                NewsAPI NewsItem = new NewsAPI
                {
                    SourceID = (string)NewsObj.SelectToken("source.id"),
                    SourceName = (string)NewsObj.SelectToken("source.name"),
                    Author = (string)NewsObj.SelectToken("author"),
                    Title = (string)NewsObj.SelectToken("title"),
                    Description = (string)NewsObj.SelectToken("description"),
                    URL = (string)NewsObj.SelectToken("url"),
                    URLImage = (string)NewsObj.SelectToken("urlToImage"),
                    PublishTime = (DateTime)NewsObj.SelectToken("publishedAt"),
                };
                newsLists.Add(NewsItem);

            }
            return View(newsLists);
        }
    }
}