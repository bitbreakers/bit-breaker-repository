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
using PusherServer;

namespace SimplySeniors.Controllers
{
    public class UserHomePageController : Controller
    {
        private ProfileContext profile = new ProfileContext();
        private PostContext db = new PostContext();
        private FollowContext db2 = new FollowContext();
        // GET: UserHomePage
        [CustomAuthorize]
        public ActionResult HomePage()
        {
            // Get the ASP.NET Identity Id of the currently authorized user
            string id = User.Identity.GetUserId();
            ProfileContext profiledb = new ProfileContext();

            // Get all profile info for current logged in user where the ASPNET ID = profile ID
            Profile profile = profiledb.Profiles.Where(u => u.USERID == id).FirstOrDefault();
            List<Profile> followed = db2.FollowLists.Where(x => x.UserID == profile.ID).Select(y => y.FollowProfile).ToList();
            List<int> IdList = db2.FollowLists.Where(x => x.UserID == profile.ID).Select(y => y.FollowedUserID).ToList();
            IdList.Add(profile.ID);
            List<Post> postlist = db.Posts.Where(x => IdList.Contains(x.ProfileID)).ToList();
            string state = profile.STATE;
            string city = profile.CITY;

            var address = "+" + city + "," + "+" + state + "," + "+USA";

            string requestUri = string.Format("https://maps.googleapis.com/maps/api/geocode/json?address={0}&key=AIzaSyAvdkMhKjOodZKxdR-ZBj1ImZd6NE_1bCU", address);
            WebRequest request = WebRequest.Create(requestUri);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException());
            string json = reader.ReadToEnd();
            dynamic information = JObject.Parse(json);
            Double lat = Convert.ToDouble(information.results[0].geometry.location.lat, CultureInfo.InvariantCulture);
            Double lng= Convert.ToDouble(information.results[0].geometry.location.lng, CultureInfo.InvariantCulture);
            Double[] Location = new Double[2];
            Location[0] = lat;
            Location[1] = lng;
            UserHomeViewModel viewModel = new UserHomeViewModel(profile, postlist, Location, followed);
            if (profile == null)
            {
                return HttpNotFound();
            }
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
            var listOfAllProfiles = profile.Profiles.Select(u => u.FIRSTNAME).Distinct().ToList();
            return Json(listOfAllProfiles, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public JsonResult GetSpecificMember(int id)
        {
            var person = profile.Profiles.FirstOrDefault(u => u.ID == id);
            return Json(person, JsonRequestBehavior.AllowGet);
        }



        //[HttpPost]
        //public async Task<ActionResult> HelloWorld()
        //{
        //    var options = new PusherOptions
        //    {
        //        Cluster = "us3",
        //        Encrypted = true
        //    };

        //    var pusher = new Pusher(
        //        "988305",
        //        "fa4d7066737eec81ca0a",
        //        "f87324a6080b01730d2a",
        //        options);

        //    var result = await pusher.TriggerAsync(
        //        "my-channel",
        //        "my-event",
        //        new { message = "hello world" });

        //    return new HttpStatusCodeResult((int)HttpStatusCode.OK);
        //}

    }
}