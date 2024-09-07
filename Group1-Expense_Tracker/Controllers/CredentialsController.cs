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
                // Create the user with email and password
                var authResult = await _firebaseauth.CreateUserWithEmailAndPasswordAsync(cred.EmailAdd, cred.Password);

                // Sign in the user
                var signInResult = await _firebaseauth.SignInWithEmailAndPasswordAsync(cred.EmailAdd, cred.Password);
                var user = signInResult.User;

                // Send email verification
                await _firebaseauth.SendEmailVerificationAsync(signInResult.FirebaseToken);

                // Save the username and email in Firestore
                var userId = user.LocalId;
                DocumentReference docRef = _firestoreDb.Collection("Users").Document(userId);
                Dictionary<string, object> userData = new Dictionary<string, object>
                {
                    { "Username", cred.Username },
                    { "Email", cred.EmailAdd }
                 };
                await docRef.SetAsync(userData);

                // Provide feedback to the user to check their email for verification
                TempData["RegistrationMsg"] = "Registration successful! Please verify your email for verification.";

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

        public async Task<IActionResult> Login(Credentials cred)
        {
            try
            {
                // Step 1: Fetch the email based on the provided username
                Query query = _firestoreDb.Collection("Users").WhereEqualTo("Username", cred.Username);
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Documents.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Username not found.");
                    return View(cred);
                }

                // Step 2: Extract the email from the Firestore result
                DocumentSnapshot documentSnapshot = querySnapshot.Documents.First();
                string email = documentSnapshot.GetValue<string>("Email");

                // Step 3: Use the retrieved email to log in
                var signInResult = await _firebaseauth.SignInWithEmailAndPasswordAsync(email, cred.Password);
                var user = signInResult.User;

                if (user == null)
                {
                    TempData["LoginErrorMsg"] = "Username or password is incorrect.";
                    return View("Login");
                }

                // Step 4: Check if the email is verified
                if (!user.IsEmailVerified)
                {
                    TempData["LoginSuccess"] = $"Welcome, {cred.Username}. You have logged in successfully!";
                    return RedirectToAction("Summary", "Analytics");
                }
                return View(cred);

            }
            catch (FirebaseAuthException ex)
            {
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

    }
}
