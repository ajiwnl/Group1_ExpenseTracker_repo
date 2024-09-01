using Firebase.Auth;
using Google.Cloud.Firestore;
using Group1_Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Group1_Expense_Tracker.Controllers
{
	public class CredentialsController : Controller
	{

		private readonly ILogger<CredentialsController> _logger;
		FirestoreDb _firestoreDb;
		FirebaseAuthProvider _firebaseauth;

		public CredentialsController(ILogger<CredentialsController> logger)
		{
			_logger = logger;
			_firebaseauth = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyAhHT-TnETQg_ow8H_50R5p2c69_ZLVLMU"));

			// Initialize Firestore with your service account key
			string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "group1-expensetracker-firebase-adminsdk-yqaqz-f9814ee58b.json");
			Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
			_firestoreDb = FirestoreDb.Create("group1-expensetracker");
		}

		public IActionResult Index()
		{
			return View();
		}

		public async Task<IActionResult> Register(Models.Credentials cred)
		{
			try
			{
				// Create the user with email and password
				var authResult = await _firebaseauth.CreateUserWithEmailAndPasswordAsync(cred.EmailAdd, cred.Password);

				// Sign in the user (necessary to get the user's ID token)
				var signInResult = await _firebaseauth.SignInWithEmailAndPasswordAsync(cred.EmailAdd, cred.Password);
				var userId = signInResult.User.LocalId;

				// Send email verification using Firebase REST API
				var client = new HttpClient();
				var requestContent = new StringContent(
					JsonConvert.SerializeObject(new
					{
						requestType = "VERIFY_EMAIL",
						idToken = signInResult.FirebaseToken
					}),
					Encoding.UTF8, "application/json");

				var response = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=AIzaSyAhHT-TnETQg_ow8H_50R5p2c69_ZLVLMU", requestContent);

				// Ensure the request was successful
				response.EnsureSuccessStatusCode();

				// Save the username and email in Firestore
				DocumentReference docRef = _firestoreDb.Collection("Users").Document(userId);
				Dictionary<string, object> userData = new Dictionary<string, object>
		{
			{ "Username", cred.Username },
			{ "Email", cred.EmailAdd },
			{ "IsEmailVerified", false }
		};
				await docRef.SetAsync(userData);

				// Provide feedback to the user to check their email for verification
				TempData["RegistrationMsg"] = "Registration successful! Please verify your email address to continue.";

				return RedirectToAction("Login");
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


        public async Task<IActionResult> Login(Models.Credentials cred)
        {
            try
            {
                // Fetch user document from Firestore using the provided username
                var userDoc = await _firestoreDb.Collection("Users")
                    .WhereEqualTo("Username", cred.Username)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (userDoc.Documents.Count == 0)
                {
                    TempData["LoginErrorMsg"] = "Username or password is incorrect.";
                    return View(cred);
                }

                var userId = userDoc.Documents[0].Id; // Get user ID from the document
                var userData = userDoc.Documents[0].ToDictionary();

                // Check if the email is verified
                if (!(bool)userData["IsEmailVerified"])
                {
                    TempData["LoginErrorMsg"] = "Email is not verified. Please verify your email to login.";
                    return View("Login");
                }

                // Sign in the user with email and password
                var signInResult = await _firebaseauth.SignInWithEmailAndPasswordAsync(userData["Email"].ToString(), cred.Password);

                // Proceed with successful login (e.g., set auth cookies, redirect, etc.)
                TempData["LoginSuccess"] = $"Welcome, {cred.Username}. You've logged in successfully!";
                return RedirectToAction("ForgotPassword");
            }
            catch (FirebaseAuthException ex)
            {
                TempData["LoginErrorMsg"] = ex.Message;
                return View("Login");
            }
        }

        public async Task<IActionResult> VerifyEmail(string userId)
        {
            // Update the Firestore document to set IsEmailVerified to true
            var docRef = _firestoreDb.Collection("Users").Document(userId);
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
        { "IsEmailVerified", true }
             });

            // Redirect to the login page or provide a success message
            TempData["VerificationSuccess"] = "Email verified successfully! You can now log in.";
            return RedirectToAction("Login");
        }




        public IActionResult ForgotPassword()
		{
			return View();
		}
	}
}