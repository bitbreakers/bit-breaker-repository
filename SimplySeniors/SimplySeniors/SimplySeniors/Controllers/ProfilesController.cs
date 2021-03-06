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
using Newtonsoft.Json;


namespace SimplySeniors.Controllers
{
    
    public class ProfilesController : Controller
    {
        private ProfileContext db = new ProfileContext();
        private HobbiesContext db2 = new HobbiesContext();
        private PostContext db3 = new PostContext();
        private FollowContext db4 = new FollowContext();
        private CommentContext db5 = new CommentContext();

        // GET: Profiles
        [CustomAuthorize]
        public ActionResult Index()
        {
            return View();
        }

        // HTTP POST method was created for searching profiles in the Profiles/index. 
        [HttpPost]
        [CustomAuthorize]
        public ActionResult Index(string searchString)
        {
            {
                IQueryable<Profile> products = db.Profiles;
                ViewBag.Message = "Sorry your product is not found";         // Insert message if item is not found. 

                if (!String.IsNullOrEmpty(searchString))
                {
                    products = products.Where(s => s.LASTNAME.Contains(searchString) || s.FIRSTNAME.Contains(searchString) || (s.FIRSTNAME + " " + s.LASTNAME).Contains(searchString) || (s.LASTNAME + " " + s.FIRSTNAME).Contains(searchString) || (s.LASTNAME + ", " + s.FIRSTNAME).Contains(searchString));   // Searching for matches through last name. 
                }
                return View(products.ToList());
            }
        }

        [CustomAuthorize]
        [HttpGet]
        public ActionResult MyProfile()
        {
            var userid = User.Identity.GetUserId();
            int id = db.Profiles.Where(x => x.USERID == userid).Select(x => x.ID).FirstOrDefault();
            Profile profile = db.Profiles.Find(id);
            IQueryable<string> bridges = db2.HobbyBridges.Where(x => x.ProfileID.Value == id).Select(y => y.Hobby.NAME).Distinct();
            string hobbies = string.Join(", ", bridges.ToList());
            List<Profile> followed = db4.FollowLists.Where(x => x.UserID == id).Select(y => y.FollowProfile).ToList();
            //List<int> IdList = db4.FollowLists.Where(x => x.UserID == id).Select(y => y.FollowedUserID).ToList();
            List<Post> postlist = db3.Posts.Where(x => x.ProfileID == profile.ID).OrderByDescending(x => x.PostDate).ToList();
            List<PostComment> comments = new List<PostComment>();
            foreach(Post post in postlist)
            {
                comments.AddRange(db5.PostComments.Where(x => x.PostID == post.ID).OrderByDescending(x => x.CommentDate).ToList());
            }
            PDViewModel viewModel = new PDViewModel(profile, hobbies, postlist, followed, comments);
            if (profile == null)
            {
                return HttpNotFound();
            }
            return View(viewModel);


        }

        // GET: Profiles/Details/5
        [CustomAuthorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Profile profile = db.Profiles.Find(id);

            IQueryable<string> bridges = db2.HobbyBridges.Where(x => x.ProfileID.Value == id).Select(y => y.Hobby.NAME).Distinct();
            string hobbies = string.Join(", ", bridges.ToList());
            List<Profile> followed = db4.FollowLists.Where(x => x.UserID == id).Select(y => y.FollowProfile).ToList();
            //List<int> IdList = db4.FollowLists.Where(x => x.UserID == id).Select(y => y.FollowedUserID).ToList();
            List<Post> postlist = db3.Posts.Where(x => x.ProfileID == profile.ID).ToList();
            List<PostComment> comments = new List<PostComment>();
            PDViewModel viewModel = new PDViewModel(profile, hobbies, postlist, followed, comments);
            if (profile == null)
            {
                return HttpNotFound();
            }
            return View(viewModel);
        }

        // GET: Profiles/Create
        [AllowAnonymous]
        public ActionResult Create()
        {
            string value = User.Identity.GetUserId();
            bool? profile = db.Profiles.Where(x => x.USERID == value).Select(x => x.PROFILECREATED).FirstOrDefault();
            if(profile == true) //profile has been created don't allow multiple profile creations
            {
                return RedirectToAction("HomePage", "UserHomePage");
            }

            return View();
        }

        // POST: Profiles/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //only logged in users can make a profile
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Exclude = "USERID", Include = "FIRSTNAME,LASTNAME,BIRTHDAY,CITY,STATE,VETSTATUS,OCCUPATION,FAMILY,BIO")] Profile profile)
        {


            ModelState.Remove("USERID"); // user doesn't input a key so we need to get the key of the current user logged in who created the profile.
            profile.USERID = User.Identity.GetUserId(); //get user id of current user
            var errors = ModelState.Values.SelectMany(v => v.Errors); // debugging for errors
            DateTime currentTime, userbirthday = new DateTime();
            currentTime = DateTime.Now; //get the current date
            currentTime = currentTime.AddYears(-65); // -65 years to determine if the user meets our standards.
            var data = ViewData.ModelState["BIRTHDAY"].Value; // get model data
            string BirthdayString = data.AttemptedValue; //get string date from data, must be converted to datetime for comparison. 
            

            string[] validformats = new[] { "MM/dd/yyyy", "yyyy/MM/dd", "MM/dd/yyyy HH:mm:ss",
                                        "MM/dd/yyyy hh:mm tt", "yyyy-MM-dd HH:mm:ss,fff" }; //Accepted user input formats

            CultureInfo provider = new CultureInfo("en-US"); //US time standard
            if (!DateTime.TryParseExact(BirthdayString, validformats, provider, DateTimeStyles.None, out userbirthday)) //attempt to convert, check to see if it fails
            {
                ModelState.AddModelError("BIRTHDAY", "Invalid Formatting, try something like this 01/23/1932"); //if failed inform of invalid format
            }

            else if (userbirthday >= currentTime) // dont continueif failed, check if user is old enough to register, return an error if they are not
            {
                ModelState.AddModelError("BIRTHDAY", "I'm sorry, but you must be 65 Years or older to make a profile");
            }


            if (ModelState.IsValid)
            {
                profile.BIRTHDAY = userbirthday; //Correct formatting for DB
                profile.PROFILECREATED = true; //user created profile
                db.Profiles.Add(profile);
                db.SaveChanges();
                return RedirectToAction("Create", "HobbyBridges"); //edit this line maddy
            }

            return View(profile);
        }

        // GET: Profiles/Edit/5
        [CustomAuthorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Profile profile = db.Profiles.Find(id);
            if (profile == null)
            {
                return HttpNotFound();
            }
            return View(profile);
        }

        // POST: Profiles/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
       public ActionResult Edit([Bind(Include = "ID,FIRSTNAME,LASTNAME,BIRTHDAY,CITY,STATE,VETSTATUS,OCCUPATION,FAMILY,BIO,USERID,PROFILECREATED")] Profile profile)
        {

            

            if (ModelState.IsValid)
            {
                db.Entry(profile).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("MyProfile");
            }
            return View(profile);
        }
        [CustomAuthorize]
        public ActionResult search()
        {
            var loggedInUser = User.Identity.GetUserId();
            var profile = db.Profiles.Where(x => x.USERID == loggedInUser).FirstOrDefault(); //profile of user logged in user, less trips to the DB
            string state = profile.STATE; //state of the logged in user
            var hobby = db2.HobbyBridges.Where(x => x.ProfileID == profile.ID).Select(x => x.HobbiesID).FirstOrDefault(); //hobby id of logged in user
            List<int?> ProfileID = db2.HobbyBridges.Where(x => x.HobbiesID == hobby).Select(x => x.ProfileID).ToList<int?>(); // all users who have a similiar hobby to logged in user
            List<Profile> profiles = new List<Profile>();
            List<Profile> AllUsersInSameState = new List<Profile>();
            List<Profile> Allusers = new List<Profile>();
            Allusers = db.Profiles.ToList(); //prevent multiple trips to the db
            AllUsersInSameState = Allusers.Where(x => x.STATE == profile.STATE).ToList<Profile>(); // get all users in Oregon

            foreach (int ProfID in ProfileID) { //for each id in the list search the db and grab that user
                profiles.AddRange(Allusers.Where(x => x.STATE == state && x.ID == ProfID).ToList<Profile>()); // get all people who are in the same state and who share the same hobby
                AllUsersInSameState.RemoveAll(x => x.ID == ProfID); //remove users that met the first requirement
                
            }
 
            profiles = profiles.OrderBy(x => x.LASTNAME).ToList(); //sort by last name
            var removeoriginal = profiles.Single(x => x.ID == profile.ID); //remove the original user from the list
            profiles.Remove(removeoriginal); //remove the user from the list
            AllUsersInSameState = AllUsersInSameState.OrderBy(x => x.LASTNAME).ToList(); ///order by last name
            profiles.AddRange(AllUsersInSameState); // add the rest of the users
            profiles.AddRange(Allusers.Where(x => x.STATE != profile.STATE).ToList()); //all users outside of the state
            FollowerInheritanceModel results = new FollowerInheritanceModel(profiles); //viewmodel

           
            return View(results);

        }



        // GET: Profiles/Delete/5
        [CustomAuthorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Profile profile = db.Profiles.Find(id);
            if (profile == null)
            {
                return HttpNotFound();
            }
            return View(profile);
        }

        // POST: Profiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {

            Profile profile = db.Profiles.Find(id);
            db.Profiles.Remove(profile);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
