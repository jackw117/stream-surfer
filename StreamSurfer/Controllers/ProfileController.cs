using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamSurfer.Models;
using StreamSurfer.Models.ProfileViewModels;
using StreamSurfer.Services;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace StreamSurfer.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly string _externalCookieScheme;
        private readonly IMessageService _messageSender;
        private readonly ILogger _logger;
        private readonly PostgresDataContext _context;

        public ProfileController(
          PostgresDataContext context,
          UserManager<AppUser> userManager,
          SignInManager<AppUser> signInManager,
          IOptions<IdentityCookieOptions> identityCookieOptions,
          IMessageService messageSender,
          ILoggerFactory loggerFactory)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _externalCookieScheme = identityCookieOptions.Value.ExternalCookieAuthenticationScheme;
            _messageSender = messageSender;
            _logger = loggerFactory.CreateLogger<ProfileController>();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return await Overview(null);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Overview(string id)
        {
            bool isPersonal = true;
            var user = await GetCurrentUserAsync();
            if ((id == null || id == "") && user == null)
            {
                return RedirectToAction("Error", "Home", new { id = 404 });
            }
            if (user == null)
            {
                isPersonal = false;
                user = await _context.Users
                    .SingleOrDefaultAsync(x => x.NormalizedUserName == id.ToUpper());
            }
            // double check after getting id user
            if (user == null)
            {
                return NotFound();
            }
            string path = user.ProfilePicture == null || user.ProfilePicture == ""
                ? "default-avatar.png"
                : user.ProfilePicture;
            string bio =  user.Bio == null || user.Bio == ""
                ? "This user hasn't set a bio yet!"
                : user.Bio;
            var myList = await _context.MyList
                .Include(m => m.MyListShows)
                .SingleOrDefaultAsync(x => x.User.Id == user.Id);
            var recentWatch = new List<Show>();
            var recentRate = new List<MyListShows>();
            if (myList == null)
            {
                // just make an empty list, so no errors appear
                myList = new MyList()
                {
                    MyListShows = new List<MyListShows>(),
                };
            }
            else
            {
                var listShows = await _context.MyListShows
                    .Include(m => m.MyList)
                    .Include(m => m.Show)
                    .Where(x => x.MyList.Id == myList.Id)
                    .ToListAsync();
                recentWatch = listShows
                    .OrderByDescending(x => x.LastChange)
                    .Select(x => x.Show)
                    .Take(10)
                    .ToList();
                recentRate = listShows
                    .Where(x => x.Rating > 0)
                    .OrderByDescending(x => x.LastChange)
                    .Take(10)
                    .ToList();
                myList.MyListShows = listShows;
            }
            var model = new OverviewViewModel()
            {
                Username = user.UserName,
                RegisterDate = user.RegisterDate,
                ProfilePicture = path,
                Bio = bio,
                List = myList,
                RecentlyRated = recentRate,
                RecentlyWatched = recentWatch,
                IsPersonalProfile = isPersonal
            };
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult List(int id)
        {
            //var list = _context.MyList.Where(x => x.Id == id);
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> AddToList(int id)
        {
            var toAdd = _context.Shows.SingleOrDefault(x => x.ID == id);
            var user = await GetCurrentUserAsync();
            if(toAdd != null)
            {
                var myList = _context.MyList.FirstOrDefault(x => x.User.Id == user.Id);
                if(myList.MyListShows == null)
                {
                    myList.MyListShows = new List<MyListShows>();
                }
                _context.Add(new MyListShows()
                    {
                        ShowId = id,
                        MyListId = myList.Id,
                        Rating = (int)ShowRating.NOT_RATED,
                        Status = (int)ShowStatus.WANT_TO_WATCH,
                        LastChange = DateTime.Now,
                        MyList = myList
                    });
                _context.SaveChanges();
            }
            return Json("Added to list");
        }

        [HttpPost]
        public  async Task<JsonResult> UpdateList(int id, ShowStatus status, ShowRating rating)
        {
            var user = await GetCurrentUserAsync();
            if(user == null)
            {
                return Json("ERROR: failed to update");
            }
            var toModify = await _context.MyListShows
                .Include(x => x.MyList)
                .SingleOrDefaultAsync(x => x.ShowId == id && x.MyList.UserForeignKey == user.Id);
            if(toModify == null)
            {
                return Json("ERROR: Show not in list");
            }
            _context.Update(toModify);
            toModify.Status = (int)status;
            toModify.Rating = (int)rating;
            await _context.SaveChangesAsync();
            return Json("Updated list");
        }

        private Task<AppUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }
    }
}