using Firebase.Auth;
using Google.Cloud.Firestore;
using Group1_Expense_Tracker.Models;
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
                string userId = HttpContext.Session.GetString("FirebaseUserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Credentials");
                }

                CollectionReference categoryCollectionRef = _firestoreDb.Collection("Category");

                // Query for categories matching the UserId
                Query userCategoryQuery = categoryCollectionRef.WhereEqualTo("UserId", userId);
                QuerySnapshot userCategorySnapshot = await userCategoryQuery.GetSnapshotAsync();

                DocumentReference userDocRef;

                if (userCategorySnapshot.Documents.Any())
                {
                    // Get the first matching document
                    userDocRef = userCategorySnapshot.Documents.First().Reference;
                    var existingCategory = userCategorySnapshot.Documents.First().ToDictionary();

                    // Convert to List<string> for category names
                    var categoryNames = existingCategory["CategoryNames"] as List<object>;
                    var categoryNamesList = categoryNames?.Cast<string>().ToList() ?? new List<string>();

                    // Check if category already exists
                    if (categoryNamesList.Contains(categoryName))
                    {
                        TempData["ErrorMessage"] = "Category already exists!";
                        return RedirectToAction("Details");
                    }

                    // Add the new category to the list
                    categoryNamesList.Add(categoryName);

                    // Update the document with the new category
                    existingCategory["CategoryNames"] = categoryNamesList;
                    await userDocRef.SetAsync(existingCategory, SetOptions.MergeAll);
                }
                else
                {
                    // No category document exists for this user, create a new one
                    userDocRef = categoryCollectionRef.Document();
                    Dictionary<string, object> newCategory = new Dictionary<string, object>
            {
                { "UserId", userId },
                { "CategoryNames", new List<string> { categoryName } }
            };

                    await userDocRef.SetAsync(newCategory);
                }

                TempData["SuccessMessage"] = "Category added successfully!";
                return RedirectToAction("Details");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding category: " + ex.Message);
                TempData["ErrorMessage"] = "Error adding category: " + ex.Message;
                return RedirectToAction("Details");
            }
        }



        public async Task<IActionResult> Details()
        {
            string userId = HttpContext.Session.GetString("FirebaseUserId");

            ViewData["ActivePage"] = "Category";
            _ = HttpContext.Session.GetString("FirebaseUserId"); //retrieve the current logged-in user
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Credentials");
            }

            try
            {
                // Retrieve the category document that matches the userId
                CollectionReference categoryCollectionRef = _firestoreDb.Collection("Category");
                Query userCategoryQuery = categoryCollectionRef.WhereEqualTo("UserId", userId);
                QuerySnapshot userCategorySnapshot = await userCategoryQuery.GetSnapshotAsync();

                var categories = new List<string>();

                if (userCategorySnapshot.Documents.Any())
                {
                    // Fetch the user's category document
                    var userCategoryDoc = userCategorySnapshot.Documents.First().ToDictionary();

                    // Safely cast the category names to List<object> and convert to List<string>
                    var categoryNames = userCategoryDoc["CategoryNames"] as List<object>;
                    categories = categoryNames?.Cast<string>().ToList() ?? new List<string>();
                }

                return View(categories); // Pass the categories to the view
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return View(new List<string>());
            }
        }

    }
}
