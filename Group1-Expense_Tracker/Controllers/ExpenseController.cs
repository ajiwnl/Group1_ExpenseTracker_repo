using Google.Cloud.Firestore;
using Group1_Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Group1_Expense_Tracker.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly FirestoreDb _firestoreDb;

        public ExpenseController(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }
        public async Task<IActionResult> Details()
        {
            ViewData["ActivePage"] = "Expense";

            string userId = HttpContext.Session.GetString("FirebaseUserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Credentials");
            }

            try
            {
                // Fetch the Category collection and filter by the logged-in UserId
                CollectionReference categoryCollectionRef = _firestoreDb.Collection("Category");
                QuerySnapshot categorySnapshot = await categoryCollectionRef
                    .WhereEqualTo("UserId", userId)
                    .GetSnapshotAsync();

                var categories = new List<string>();

                if (categorySnapshot.Documents.Any())
                {
                    // Assuming there is only one document per user, you can access the first document
                    var categoryDoc = categorySnapshot.Documents.First();

                    // Log document data for debugging
                    Console.WriteLine($"Document Data: {categoryDoc.ToDictionary()}");

                    // Retrieve the CategoryNames array
                    var categoryNames = categoryDoc.GetValue<List<object>>("CategoryNames");

                    // Convert each item in the array to a string and add it to the categories list
                    categories = categoryNames.Select(item => item.ToString()).ToList();
                }
                else
                {
                    Console.WriteLine("No category documents found.");
                }

                // Pass the categories to the view
                ViewData["Categories"] = categories;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                ViewData["Categories"] = new List<string>();
                return View();
            }
        }


        public async Task<IActionResult> AddExpense(string categoryName, string title, double amount, DateTime date, string description)
        {
            string userId = HttpContext.Session.GetString("FirebaseUserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Credentials");
            }

            if (string.IsNullOrEmpty(categoryName))
            {
                ViewData["CategoryError"] = "Please select a category.";
                return RedirectToAction("Details");
            }

            try
            {
                // Access the Category collection directly
                CollectionReference categoryCollectionRef = _firestoreDb.Collection("Category");

                // Query the Category collection to find the category with the specified name and userId
                QuerySnapshot categorySnapshot = await categoryCollectionRef
                    .WhereEqualTo("UserId", userId)
                    .WhereArrayContains("CategoryNames", categoryName)
                    .GetSnapshotAsync();

                Console.WriteLine("Category Snapshot Count: " + categorySnapshot.Documents.Count);

                if (categorySnapshot.Documents.Any())
                {
                    Console.WriteLine("Category found: " + categoryName);

                    // Create a new expense using the model
                    Expense newExpense = new Expense
                    {
                        Title = title,
                        Amount = amount,
                        Date = Timestamp.FromDateTime(date.ToUniversalTime()),
                        Description = description,
                        UserId = userId,
                        CategoryName = categoryName  // Add the category name to link it
                    };

                    // Access the Expenses collection directly
                    CollectionReference expensesCollectionRef = _firestoreDb.Collection("Expenses");

                    // Check if the expense already exists
                    QuerySnapshot expenseSnapshot = await expensesCollectionRef
                        .WhereEqualTo("UserId", userId)
                        .WhereEqualTo("Title", title)
                        .WhereEqualTo("CategoryName", categoryName)
                        .GetSnapshotAsync();

                    if (expenseSnapshot.Documents.Any())
                    {
                        TempData["ErrorMessage"] = "Expense already exists!";
                        return RedirectToAction("Details");
                    }

                    // Add the new expense to the Firestore collection
                    await expensesCollectionRef.AddAsync(newExpense);

                    TempData["SuccessMessage"] = "Expense added successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    Console.WriteLine("No category found with name: " + categoryName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                Console.WriteLine("Error: " + ex.Message);
            }

            return RedirectToAction("Details");
        }

    }
}
