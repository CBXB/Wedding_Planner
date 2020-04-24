using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wedding_Planner.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Wedding_Planner.Controllers
{
    public class HomeController : Controller
    {
        private WeddingPlannerContext _context;
        public HomeController(WeddingPlannerContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("Register")]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email is already in use!");
                }
                PasswordHasher<User> Hasher = new PasswordHasher<User>();
                user.Password = Hasher.HashPassword(user, user.Password);
                // Save your user object to database
                User NewUser = new User
                {
                    First_Name = @user.First_Name,
                    Last_Name = @user.Last_Name,
                    Email = @user.Email,
                    Password = @user.Password,
                };
                var userEntity = _context.Add(NewUser).Entity;
                _context.SaveChanges();
                return RedirectToAction("Welcome");
            }

            return View("Index");
        }

        [HttpPost("Login")]
        public IActionResult Login(User userSubmission)
        {
            // if initial ModelState is valid, query for user with the provided email.
            var userInDb = _context.Users.FirstOrDefault(u => u.Email == userSubmission.Email);
            // if no user exisits with provided email....
            if (userInDb == null)
            {
                // Add an error to ModelState and return to View!
                ModelState.AddModelError("Email", "This email does not exisit within our records :(");
                return View("Index");
            }
            // Initialize hasher object.
            var hasher = new PasswordHasher<User>();
            // Verify provided password againest hash store in DB.
            var result = hasher.VerifyHashedPassword(userSubmission, userInDb.Password, userSubmission.Password);
            if (result == 0)
            {
                Console.WriteLine("Invaild Password");
                ModelState.AddModelError("Password", "Invaild Password");
                return View("Index");
            }
            HttpContext.Session.SetInt32("UserId", userInDb.UserId);
            return RedirectToAction("Welcome");
        }
        // the welcome page is set to render wedings but you caj change it according to the new model
        [HttpGet("Welcome")]
        public IActionResult Welcome()
        {
            var weddings = _context.Weddings
            .Include(w => w.RSVPs)
            .OrderByDescending(w => w.Date);

            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");

            var responded = weddings.Where(w => w.RSVPs.Any(r => r.UserId == 1));

            return View("Welcome", weddings);
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
        [HttpGet("NewWedding")]
        public IActionResult NewWedding()
        {
            return View("NewWedding");
        }

        [HttpPost("CreateWedding")]
        public IActionResult CreateWedding(Wedding new_wedding)
        {
            if(ModelState.IsValid)
            {
                if(new_wedding.Date < DateTime.Today)
                {
                    ModelState.AddModelError("Date", "Date must be in the future!");
                    return View("NewWedding");
                }
                else
                {
                    Wedding this_wedding = new Wedding
                    {
                        Address = new_wedding.Address,
                        Date = new_wedding.Date,
                        WedderOne = new_wedding.WedderOne,
                        WedderTwo = new_wedding.WedderTwo,
                        UserId = (int) HttpContext.Session.GetInt32("UserId")
                    };
                    _context.Add(this_wedding);
                    _context.SaveChanges();
                    return RedirectToAction("Welcome");
                }
            }
            else
            {
                if(new_wedding.Date < DateTime.Today)
                {
                    ModelState.AddModelError("Date","Date must be in the future!");
                }
                return View("NewWedding");
            }
        }
        

        [HttpGet]
        [Route("ViewWedding/{weddingId}")]
        public IActionResult ViewWedding(int weddingId)
        {
            Wedding wedding = _context.Weddings
            .Include(r => r.RSVPs)
            .ThenInclude(u => u.User)
            .Where(w => w.WeddingId == weddingId)
            .SingleOrDefault();

            ViewBag.Wedding = wedding;
            ViewBag.Address = wedding.Address;
            return View("ViewWedding");
        }

        [Route("RSVP")]
        public IActionResult RSVP(int weddingId)
        {
            RSVP new_rsvp = new RSVP
            {
                UserId = (int) HttpContext.Session.GetInt32("UserId"),
                WeddingId = weddingId
            };
            _context.Add(new_rsvp);
            _context.SaveChanges();

            return RedirectToAction("Welcome");

        }
        [Route("UnRSVP")]
        public IActionResult UnRSVP(int weddingId)
        {
            RSVP this_attender = _context.RSVP
            .SingleOrDefault(u => u.UserId == HttpContext.Session
            .GetInt32("UserId") && u.WeddingId == weddingId);

            _context.RSVP.Remove(this_attender);
            _context.SaveChanges();
            return RedirectToAction("Welcome");
        }
        [Route("Delete")]
        public IActionResult Delete(int weddingId) 
        {
            Wedding this_wedding = _context.Weddings
            .SingleOrDefault(w => w.WeddingId == weddingId);

            List<RSVP> rsvps = _context.RSVP
            .Where(a => a.WeddingId == weddingId)
            .ToList();

            foreach(var attender in rsvps)
            {
                _context.RSVP.Remove(attender);
            } 
            _context.Weddings.Remove(this_wedding);
            _context.SaveChanges();

            return RedirectToAction("Welcome");
        }
        


        // ==============================================================================================

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
