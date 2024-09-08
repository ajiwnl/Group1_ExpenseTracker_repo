using Firebase.Auth;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Group1_Expense_Tracker.Controllers
{
    public class CategoryController : Controller
    {
        private readonly FirestoreDb _firestoreDb;

        public CategoryController(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(string categoryName)
        {
            try
            {
                // Step 1: Get the currently logged-in user's ID from session
                string userId = HttpContext.Session.GetString("FirebaseUserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Credentials");
                }

                // Step 2: Navigate to the user's document in Firestore
                DocumentReference userDocRef = _firestoreDb.Collection("Users").Document(userId);

                // Step 3: Create a reference to the "Category" sub-collection under the user
                CollectionReference categoryCollectionRef = userDocRef.Collection("Category");

                // Step 4: Check if the category already exists
                Query categoryQuery = categoryCollectionRef.WhereEqualTo("CategoryName", categoryName);
                QuerySnapshot categorySnapshot = await categoryQuery.GetSnapshotAsync();

                if (categorySnapshot.Documents.Any())
                {
                    // Category already exists
                    TempData["ErrorMessage"] = "Category already exists!";
                    return RedirectToAction("Details");
                }

                // Step 5: Add the new category if it does not exist
                await categoryCollectionRef.AddAsync(new
                {
                    CategoryName = categoryName,
                });

                TempData["SuccessMessage"] = "Category added successfully!";
                return RedirectToAction("Details");
            }
            catch (FirebaseAuthException ex)
            {
                TempData["ErrorMessage"] = "Error adding category: " + ex.Message;
                return RedirectToAction("Details");
            }
        }

        public async Task<IActionResult> Details()
        {
            ViewData["ActivePage"] = "Category";

            string userId = HttpContext.Session.GetString("FirebaseUserId"); //retrieve the current logged-in user
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Credentials");
            }

            try
            {
                // Retrieve all the category fields from the logged-in user
                DocumentReference userDocRef = _firestoreDb.Collection("Users").Document(userId);
                CollectionReference categoryCollectionRef = userDocRef.Collection("Category");
                QuerySnapshot categorySnapshot = await categoryCollectionRef.GetSnapshotAsync();

                var categories = categorySnapshot.Documents
                    .Select(doc => doc.GetValue<string>("CategoryName"))
                    .ToList();

                return View(categories);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return View(new List<string>());
            }
        }
    }
}
