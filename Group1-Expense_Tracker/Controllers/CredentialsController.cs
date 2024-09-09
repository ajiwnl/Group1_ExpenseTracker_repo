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

        public CredentialsController(ILogger<CredentialsController> logger, FirebaseAuthProvider firebaseauth, FirestoreDb firestoreDb)
        {
            _logger = logger;
            _firebaseauth = firebaseauth;
            _firestoreDb = firestoreDb;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Register(Credentials cred)
        {
            if (!ModelState.IsValid)
            {
                return View(cred);
            }

            try
            {
                // Check if the username already exists in Firestore
                var usernameQuery = await _firestoreDb.Collection("Users")
                    .WhereEqualTo("Username", cred.Username)
                    .GetSnapshotAsync();

                if (usernameQuery.Documents.Count > 0)
                {
                    TempData["UserExist"] = "Username already exists.";
                    return View(cred);
                }

                // Create the user with email and password
                var authResult = await _firebaseauth.CreateUserWithEmailAndPasswordAsync(cred.EmailAdd, cred.Password);

                // Send email verification
                await _firebaseauth.SendEmailVerificationAsync(authResult.FirebaseToken);

                // Save the username and email in Firestore
                var userId = authResult.User.LocalId;
                DocumentReference docRef = _firestoreDb.Collection("Users").Document(userId);
                Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "Username", cred.Username },
            { "Email", cred.EmailAdd }
        };
                await docRef.SetAsync(userData);

                // Inform the user to check their email for verification
                TempData["RegistrationMsg"] = "Registration successful! Please check your email to verify your account before logging in.";

                return RedirectToAction("Login"); // Redirect to the login page
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"Firebase Exception: {ex.Message}");

                //incorrect password
                if (ex.Message.Contains("INVALID_PASSWORD") || ex.Message.Contains("wrong-password"))
                {
                    TempData["IncorrectPassword"] = "Incorrect password. Please try again!";
                    return View(cred);
                }
                else
                {
                    TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                }

                return View(cred);
            }
        }

        public async Task<IActionResult> Login(Credentials cred)
        {
            try
            {
                // Step 1: Fetch the email based on the provided username
                Query query = _firestoreDb.Collection("Users").WhereEqualTo("Username", cred.Username);
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Documents.Count == 0)
                {
                    TempData["UsernameNotFound"] = "Username does not exist. Please try again!";
                    return View(cred);
                }

                // Step 2: Extract the email from the Firestore result
                DocumentSnapshot documentSnapshot = querySnapshot.Documents.First();
                string email = documentSnapshot.GetValue<string>("Email");

                // Step 3: Use the retrieved email to log in
                var signInResult = await _firebaseauth.SignInWithEmailAndPasswordAsync(email, cred.Password);
                var user = signInResult.User;


                // Step 4: Refresh the user data to get the latest email verification status
                user = await _firebaseauth.GetUserAsync(signInResult.FirebaseToken);

                // Step 5: Check if the email is verified
                if (!user.IsEmailVerified)
                {
                    // Simulate sign out by clearing session data or tokens
                    HttpContext.Session.Remove("FirebaseUserId");
                    TempData["EmailVerifyError"] = "Email not verified. Please verify your email before logging in.";
                    return View(cred);
                }

                // Step 6: Store the user ID in session if email is verified
                HttpContext.Session.SetString("FirebaseUserId", user.LocalId);
                TempData["LoginSuccess"] = $"Welcome, {cred.Username}. You have logged in successfully!";
                return RedirectToAction("Summary", "Analytics");
            }
            catch (FirebaseAuthException ex)
            {
                //When the password is incorrect
                if (ex.Message.Contains("INVALID_PASSWORD"))
                {
                    TempData["IncorrectPassword"] = "Incorrect password. Please try again!";
                    return View(cred);
                }

                // Other Firebase authentication exceptions
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(cred);
            }
        }

        public async Task<IActionResult> ForgotPassword(Credentials cred)
        {
            try
            {
                // Check if the email is provided
                if (string.IsNullOrEmpty(cred.EmailAdd))
                {
                    return View(cred);
                }
                //Send the password reset email
                await _firebaseauth.SendPasswordResetEmailAsync(cred.EmailAdd);

                // Notify the user the email was sent
                TempData["ForgotPasswordMsg"] = "A password reset link has been sent to your email.";
                return RedirectToAction("Login");
            }
            catch (FirebaseAuthException ex)
            {
                ModelState.AddModelError(string.Empty, "Error sending password reset email. Please check your email and try again.");
                return View(cred);
            }
        }

        public IActionResult Logout()
        {
            // Clear the user's authentication cookie or session
            HttpContext.Session.Clear(); // If using session

            // Clear authentication cookie (if using cookies)
            HttpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");

            TempData["LogoutMessage"] = "You have successfully logged out.";
            return RedirectToAction("Login", "Credentials");
        }


    }
}