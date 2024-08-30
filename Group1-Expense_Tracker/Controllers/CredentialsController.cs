using Firebase.Auth;
using Group1_Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Group1_Expense_Tracker.Controllers
{
	public class CredentialsController : Controller
	{

		private readonly ILogger<CredentialsController> _logger;

		FirebaseAuthProvider _firebaseauth;

		public CredentialsController(ILogger<CredentialsController> logger)
		{
			_logger = logger;
			_firebaseauth = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyAhHT-TnETQg_ow8H_50R5p2c69_ZLVLMU"));
		}

		public IActionResult Index()
		{
			return View();
		}
		
		public async Task<IActionResult> Register(Models.Credentials cred)
		{
			try {

				await _firebaseauth.CreateUserWithEmailAndPasswordAsync(cred.EmailAdd, cred.Password);

				var firebaselink = await _firebaseauth.SignInWithEmailAndPasswordAsync(cred.EmailAdd, cred.Password);
				string accesstoken = firebaselink.FirebaseToken;

				if (accesstoken != null)
				{

					HttpContext.Session.SetString("AccessToken", accesstoken);
					return RedirectToAction("Register");

				}
				else {

					return View(cred);
				}
			}
			catch (FirebaseAuthException ex)
			{
				try
				{
					var firebasex = JsonConvert.DeserializeObject<ErrorModel>(ex.RequestData);
					ModelState.AddModelError(string.Empty, firebasex.message);
				}
				catch
				{
					ModelState.AddModelError(string.Empty, ex.Message);
				}
				return View(cred);
			}

		}

	}
}