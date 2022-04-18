using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UsersManager.Models;
namespace UsersManager.Controllers
{
    [UserAccess]
    public class PhotosController : Controller
    {
        UsersDBEntities DB = new UsersDBEntities();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DB.Dispose();
            }
            base.Dispose(disposing);
        }

        public void RenewPhotosSerialNumber()
        {
            HttpRuntime.Cache["PhotosSerialNumber"] = Guid.NewGuid().ToString();
        }
        public string GetPhotosSerialNumber()
        {
            if (HttpRuntime.Cache["PhotosSerialNumber"] == null)
            {
                RenewPhotosSerialNumber();
            }
            return (string)HttpRuntime.Cache["PhotosSerialNumber"];
        }
        public void SetLocalPhotosSerialNumber()
        {
            Session["PhotosSerialNumber"] = GetPhotosSerialNumber();
        }
        public bool IsPhotosUpToDate()
        {
            return ((string)Session["PhotosSerialNumber"] == (string)HttpRuntime.Cache["PhotosSerialNumber"]);
        }

        public void InitSortRatings()
        {
            if (Session["RatingFieldToSort"] == null)
                Session["RatingFieldToSort"] = "dates"; // "users", "ratings"
            if (Session["RatingFieldSortDir"] == null)
                Session["RatingFieldSortDir"] = false; // ascendant
        }
        public void InitSortPhotos()
        {
            if (Session["PhotoFieldToSort"] == null)
                Session["PhotoFieldToSort"] = "dates"; // "users", "ratings"
            if (Session["PhotoFieldSortDir"] == null)
                Session["PhotoFieldSortDir"] = false; // ascendant
        }



        public void InitSearchByKeywords()
        {
            if (Session["Keywords"] == null)
                Session["Keywords"] = "";
        }

        public ActionResult SetSearchKeywords(string keywords)
        {
            Session["Keywords"] = keywords.Trim().ToLower();
            RenewPhotosSerialNumber();
            return null;
        }

        public ActionResult SortRatingsBy(string fieldToSort)
        {
            RenewPhotosSerialNumber();
            if ((string)Session["RatingFieldToSort"] == fieldToSort)
            {
                Session["RatingFieldSortDir"] = !(bool)Session["RatingFieldSortDir"];
            }
            else
            {
                Session["RatingFieldToSort"] = fieldToSort;
                Session["RatingFieldSortDir"] = true;
            }
            return null;
        }

        public ActionResult SortPhotosBy(string fieldToSort)
        {
            RenewPhotosSerialNumber();
            if ((string)Session["PhotoFieldToSort"] == fieldToSort)
            {
                Session["PhotoFieldSortDir"] = !(bool)Session["PhotoFieldSortDir"];
            }
            else
            {
                Session["PhotoFieldToSort"] = fieldToSort;
                Session["PhotoFieldSortDir"] = true;
            }
            return null;
        }

        public ActionResult Index()
        {
            InitSortPhotos();
            InitSearchByKeywords();
            return View();
        }

        public PartialViewResult GetPhotos(bool forceRefresh = false)
        {
            if (forceRefresh || !IsPhotosUpToDate() || OnlineUsers.NeedUpdate())
            {
                SetLocalPhotosSerialNumber();
                DB.Update_Photo_Ratings();
                List<Photo> photos = DB.VisiblePhotos(OnlineUsers.CurrentUserId).OrderByDescending(i => i.CreationDate).ToList();
                string keywords = (string)Session["Keywords"];
                if (keywords != "")
                    photos = UsersDBDAL.SearchPhotosByKeywords(photos, keywords);
                switch ((string)Session["PhotoFieldToSort"])
                {
                    case "dates":
                        if ((bool)Session["PhotoFieldSortDir"])
                        {
                            photos = photos.OrderBy(pr => pr.CreationDate).ToList();
                        }
                        else
                        {
                            photos = photos.OrderByDescending(pr => pr.CreationDate).ToList();
                        }
                        break;
                    case "users":
                        if ((bool)Session["PhotoFieldSortDir"])
                        {
                            photos = photos.OrderBy(pr => pr.User.FirstName).ThenBy(pr => pr.User.LastName).ToList();
                        }
                        else
                        {
                            photos = photos.OrderByDescending(pr => pr.User.FirstName).ThenByDescending(pr => pr.User.LastName).ToList();
                        }
                        break;
                    case "ratings":
                        if ((bool)Session["PhotoFieldSortDir"])
                        {
                            photos = photos.OrderBy(pr => pr.Ratings).ToList();
                        }
                        else
                        {
                            photos = photos.OrderByDescending(pr => pr.Ratings).ToList();
                        }
                        break;
                    default: break;
                }

                return PartialView(photos);
            }
            return null;
        }
        public ActionResult Create()
        {
            ViewBag.Visibilities = SelectListItemConverter<PhotoVisibility>.Convert(DB.PhotoVisibilities.ToList());
            return View(new Photo());
        }
        [HttpPost]
        public ActionResult Create(Photo Photo)
        {
            if (ModelState.IsValid)
            {
                DB.Add_Photo(Photo);
                RenewPhotosSerialNumber();
                return RedirectToAction("Index");
            }
            ViewBag.Visibilities = SelectListItemConverter<PhotoVisibility>.Convert(DB.PhotoVisibilities.ToList());
            return View(Photo);
        }
        public ActionResult Edit(int id)
        {
            Photo Photo = DB.Photos.Find(id);
            if (Photo != null)
            {
                ViewBag.Visibilities = SelectListItemConverter<PhotoVisibility>.Convert(DB.PhotoVisibilities.ToList());
                return View(Photo);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public ActionResult Edit(Photo Photo)
        {
            if (ModelState.IsValid)
            {
                DB.Update_Photo(Photo);
                RenewPhotosSerialNumber();
                return RedirectToAction("Index");
            }
            ViewBag.Visibilities = SelectListItemConverter<PhotoVisibility>.Convert(DB.PhotoVisibilities.ToList());
            return View(Photo);
        }
        public ActionResult Details(int id)
        {
            InitSortRatings();
            Photo Photo = DB.Photos.Find(id);
            if (Photo != null)
                return View(Photo);
            return RedirectToAction("Index");
        }
        public ActionResult Delete(int id)
        {
            DB.Remove_Photo(id);
            RenewPhotosSerialNumber();
            return RedirectToAction("Index");
        }

        public ActionResult GetPhotoDetails(int photoId, bool forceRefresh = false)
        {
            if (forceRefresh || !IsPhotosUpToDate() || OnlineUsers.NeedUpdate())
            {
                Photo Photo = DB.Photos.Find(photoId);
                ViewBag.Visility = (DB.AreFriends(OnlineUsers.CurrentUserId, Photo.UserId) ? "Ami" : "Publique");
                if (Photo == null)
                    return null;
                SetLocalPhotosSerialNumber();
                DB.Update_Photo_Ratings();
                return PartialView(Photo);
            }
            return null;
        }

        public ActionResult UpdateCurrentUserRating(int photoId, int rating, string comment)
        {
            PhotoRating photoRating = new PhotoRating();
            photoRating.UserId = OnlineUsers.CurrentUserId;
            photoRating.PhotoId = photoId;
            photoRating.Rating = rating;
            photoRating.Comment = comment;
            photoRating.CreationDate = DateTime.Now;

            DB.AddPhotoRating(photoRating);

            RenewPhotosSerialNumber();
            return null;
        }

        public ActionResult RemoveCurrentUserRating(int photoId)
        {
            PhotoRating photoRating = new PhotoRating();
            photoRating.UserId = OnlineUsers.CurrentUserId;
            photoRating.PhotoId = photoId;

            DB.Remove_PhotoRating(photoRating);

            RenewPhotosSerialNumber();
            return null;
        }
    }
}